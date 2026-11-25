using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds the world map: a peaceful Meadow in the middle (where the Hub goes), surrounded by four wild lands:
///   West:  the Dark Forest  - thick trees, glowing mushrooms
///   East:  the Bloodmoor    - crimson grass, dead trees, blood roses (vampire country)
///   North: the Crypt Lands  - graves, broken pillars, ghost candles
///   South: the Ashlands     - ash, basalt and pools of lava
/// Each wild land has a dungeon entrance, with a road leading to it from the Hub.
/// Sprites are drawn in code (see the Art folder). Runs in both Edit mode and Play mode,
/// so the editor shows the same map you'll play on.
/// </summary>
[ExecuteAlways]
public class MapGenerator : MonoBehaviour
{
    [Header("Map Size (in tiles)")]
    [Min(5)] public int width = 176;
    [Min(5)] public int height = 120;

    [Header("Border")]
    [Tooltip("How many tiles deep the trees/rocks always are along each edge.")]
    [Min(1)] public int borderThickness = 2;
    [Tooltip("Extra ragged tiles that can poke inward past the solid border.")]
    [Min(0)] public int raggedDepth = 2;
    [Range(0f, 1f)] public float raggedChance = 0.35f;
    [Tooltip("Change this to get a different random layout.")]
    public int seed = 12345;

    [Header("Biomes")]
    [Tooltip("Size of the Meadow in the middle (as a fraction of the map).")]
    [Range(0.1f, 0.7f)] public float meadowSize = 0.45f;
    [Tooltip("How much of each land is covered in obstacles.")]
    [Range(0f, 0.7f)] public float darkForestDensity = 0.45f;
    [Range(0f, 0.5f)] public float bloodmoorDensity = 0.12f;
    [Range(0f, 0.5f)] public float cryptDensity = 0.12f;
    [Range(0f, 0.5f)] public float ashlandsDensity = 0.14f;
    [Range(0f, 0.2f)] public float meadowTreeChance = 0.01f;

    [Header("Extras")]
    [Tooltip("Adds colliders to trees and rocks so the hero can't walk through them.")]
    public bool treesBlockMovement = true;
    [Tooltip("Centers the Main Camera on the map and zooms to fit (editor view).")]
    public bool fitMainCamera = true;

    const int PixelsPerTile = 16;
    Transform gridRoot;
    bool[,] blocked;
    bool[,] road;
    Biome[,] biomes;
    readonly Dictionary<Biome, Vector2Int> dungeonSites = new();

    public static readonly Biome[] WildBiomes = { Biome.DarkForest, Biome.Bloodmoor, Biome.Crypt, Biome.Ashlands };

    // ---------- Grid helpers (used by the game) ----------

    public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < height;

    /// <summary>True if the cell is blocked by a tree, rock, grave... (or is outside the map).</summary>
    public bool IsTree(Vector2Int c) => !InBounds(c) || (blocked != null && blocked[c.x, c.y]);

    public bool IsRoad(Vector2Int c) => InBounds(c) && road != null && road[c.x, c.y];

    public Biome BiomeAt(Vector2Int c)
    {
        if (biomes == null) return Biome.Meadow;
        c.x = Mathf.Clamp(c.x, 0, width - 1); c.y = Mathf.Clamp(c.y, 0, height - 1);
        return biomes[c.x, c.y];
    }

    public Biome BiomeAt(Vector3 world) => BiomeAt(WorldToCell(world));

    /// <summary>The cell where a wild land's dungeon entrance stands (bottom-left of its 2x2).</summary>
    public Vector2Int DungeonSite(Biome b) => dungeonSites.TryGetValue(b, out var c) ? c : new Vector2Int(width / 2, height / 2);

    public Vector2Int CenterCell => new Vector2Int(width / 2, height / 2);

    /// <summary>World-space center of a grid cell.</summary>
    public Vector3 CellToWorld(Vector2Int c) =>
        transform.position + new Vector3(-width / 2f + c.x + 0.5f, -height / 2f + c.y + 0.5f, 0f);

    public Vector2Int WorldToCell(Vector3 world)
    {
        Vector3 local = world - transform.position;
        return new Vector2Int(Mathf.FloorToInt(local.x + width / 2f), Mathf.FloorToInt(local.y + height / 2f));
    }

    /// <summary>The map's rectangle in world space.</summary>
    public Rect WorldBounds => new Rect(transform.position.x - width / 2f, transform.position.y - height / 2f, width, height);

    void OnEnable() => Generate();

#if UNITY_EDITOR
    void OnValidate()
    {
        // Can't create objects inside OnValidate directly, so defer one frame.
        UnityEditor.EditorApplication.delayCall += () => { if (this != null && isActiveAndEnabled) Generate(); };
    }
#endif

    void OnDisable() => Clear();

    // ---------- Generation ----------

    public void Generate()
    {
        Clear();
        var rng = new System.Random(seed);

        ComputeBiomes();
        PlaceDungeonSites();
        CarveRoads();
        PlaceObstacles(rng);
        BuildTilemaps(rng);

        if (fitMainCamera && !Application.isPlaying && Camera.main != null && Camera.main.orthographic)
        {
            Camera cam = Camera.main;
            Vector3 c = transform.position;
            cam.transform.position = new Vector3(c.x, c.y, cam.transform.position.z);
            float aspect = cam.aspect > 0 ? cam.aspect : 16f / 9f;
            cam.orthographicSize = Mathf.Max(height / 2f, width / 2f / aspect);
        }
    }

    /// <summary>Meadow in the middle; the four wild lands split by direction, with wavy borders.</summary>
    void ComputeBiomes()
    {
        biomes = new Biome[width, height];
        float ox = seed * 0.0131f, oy = seed * 0.0173f;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float nx = (x - width / 2f) / (width / 2f), ny = (y - height / 2f) / (height / 2f);
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float wobble = Mathf.PerlinNoise(ox + x * 0.06f, oy + y * 0.06f) - 0.5f;
                if (r < meadowSize + wobble * 0.15f) { biomes[x, y] = Biome.Meadow; continue; }

                float angle = Mathf.Atan2(ny, nx) + wobble * 0.9f;
                if (angle > Mathf.PI) angle -= 2f * Mathf.PI;
                if (angle < -Mathf.PI) angle += 2f * Mathf.PI;
                float a = Mathf.Abs(angle);
                biomes[x, y] = a < Mathf.PI / 4f ? Biome.Bloodmoor
                             : a > 3f * Mathf.PI / 4f ? Biome.DarkForest
                             : angle > 0f ? Biome.Crypt : Biome.Ashlands;
            }
    }

    void PlaceDungeonSites()
    {
        dungeonSites.Clear();
        foreach (var b in WildBiomes)
        {
            Vector2 dir = b switch
            {
                Biome.DarkForest => new Vector2(-1f, 0.15f),
                Biome.Bloodmoor => new Vector2(1f, -0.15f),
                Biome.Crypt => new Vector2(0.2f, 1f),
                _ => new Vector2(-0.2f, -1f),
            };
            var site = new Vector2Int(
                Mathf.RoundToInt(width / 2f + dir.x * width / 2f * 0.76f),
                Mathf.RoundToInt(height / 2f + dir.y * height / 2f * 0.74f));
            site.x = Mathf.Clamp(site.x, borderThickness + 4, width - borderThickness - 6);
            site.y = Mathf.Clamp(site.y, borderThickness + 4, height - borderThickness - 6);
            dungeonSites[b] = site;
        }
    }

    /// <summary>Winding dirt roads from the Hub out to each dungeon.</summary>
    void CarveRoads()
    {
        road = new bool[width, height];
        Vector2 hub = new Vector2(width / 2f, height / 2f);
        int i = 0;
        foreach (var kv in dungeonSites)
        {
            Vector2 end = kv.Value + new Vector2(1f, -1f); // in front of the entrance
            float len = Vector2.Distance(hub, end);
            Vector2 dir = (end - hub) / len, perp = new Vector2(-dir.y, dir.x);
            for (float t = 2.5f; t <= len; t += 0.3f)
            {
                float bend = Mathf.Sin(t * 0.15f + i * 1.7f) * 3f * Mathf.Sin(Mathf.PI * t / len);
                Vector2 p = hub + dir * t + perp * bend;
                for (int dx = 0; dx <= 1; dx++)
                    for (int dy = 0; dy <= 1; dy++)
                    {
                        var c = new Vector2Int(Mathf.FloorToInt(p.x) + dx, Mathf.FloorToInt(p.y) + dy - 1);
                        if (InBounds(c)) road[c.x, c.y] = true;
                    }
            }
            i++;
        }
    }

    void PlaceObstacles(System.Random rng)
    {
        blocked = new bool[width, height];
        Vector2 hub = new Vector2(width / 2f, height / 2f);
        float ox = seed * 0.031f + 100f, oy = seed * 0.027f + 100f;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                int distToEdge = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                bool isBlocked = distToEdge < borderThickness;
                if (!isBlocked && distToEdge < borderThickness + raggedDepth)
                {
                    // Chance falls off the further inward we go, giving a natural edge.
                    int step = distToEdge - borderThickness + 1;
                    isBlocked = rng.NextDouble() < raggedChance / step;
                }

                if (!isBlocked)
                {
                    var b = biomes[x, y];
                    float clump = Mathf.PerlinNoise(ox + x * 0.18f, oy + y * 0.18f); // obstacles come in clumps
                    float density = b switch
                    {
                        Biome.DarkForest => darkForestDensity,
                        Biome.Bloodmoor => bloodmoorDensity,
                        Biome.Crypt => cryptDensity,
                        Biome.Ashlands => ashlandsDensity,
                        _ => meadowTreeChance,
                    };
                    if (b == Biome.DarkForest) isBlocked = clump < density * 1.25f || rng.NextDouble() < density * 0.25f;
                    else isBlocked = rng.NextDouble() < density * (0.4f + clump * 1.2f);
                }

                // Keep the roads, the Hub area and the dungeon doors clear.
                if (isBlocked)
                {
                    if (Vector2.Distance(hub, new Vector2(x, y)) < 7f) isBlocked = false;
                    else if (NearRoad(x, y)) isBlocked = false;
                    else foreach (var s in dungeonSites.Values)
                        if (Vector2.Distance(new Vector2(x, y), s + Vector2.one * 0.5f) < 4f) { isBlocked = false; break; }
                    if (distToEdge < borderThickness) isBlocked = true;
                }
                blocked[x, y] = isBlocked;
            }
    }

    bool NearRoad(int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (IsRoad(new Vector2Int(x + dx, y + dy))) return true;
        return false;
    }

    // ---------- Tiles ----------

    void BuildTilemaps(System.Random rng)
    {
        // Grid + layers: ground, obstacles on top, and a glow layer (unlit) for things that shine in the dark.
        var gridGO = new GameObject("Map Grid (generated)") { hideFlags = HideFlags.DontSave };
        gridGO.transform.SetParent(transform, false);
        gridGO.AddComponent<Grid>();
        // Offset so the map is centered on this object's position.
        gridGO.transform.localPosition = new Vector3(-width / 2f, -height / 2f, 0f);
        gridRoot = gridGO.transform;

        Tilemap ground = MakeTilemap("Ground", 0, false);
        Tilemap obstacles = MakeTilemap("Obstacles", 1, false);
        Tilemap glow = MakeTilemap("Glow", 3, true);

        // Make each tile once, then reuse it.
        var groundTiles = new Dictionary<(Biome, int, int), (Tile, Tile)>();
        var obstacleTiles = new Dictionary<(Biome, int), (Tile, Tile)>();
        var roadTiles = new Dictionary<(Biome, int), Tile>();

        (Tile, Tile) GroundTile(Biome b, int tone, int v)
        {
            if (groundTiles.TryGetValue((b, tone, v), out var t)) return t;
            int decor = v < 3 ? 0 : v - 2; // 3 plain variations, then 3 kinds of decoration
            var art = PixelArt.Ground(b, tone, decor, seed + v * 101);
            t = (MakeTile(art, $"{b} {tone}-{v}"), MakeGlowTile(art, $"{b} {tone}-{v} glow"));
            groundTiles[(b, tone, v)] = t;
            return t;
        }

        (Tile, Tile) ObstacleTile(Biome b, int v)
        {
            if (obstacleTiles.TryGetValue((b, v), out var t)) return t;
            var art = PixelArt.Obstacle(b, v);
            t = (MakeTile(art, $"{b} obstacle {v}"), MakeGlowTile(art, $"{b} obstacle {v} glow"));
            obstacleTiles[(b, v)] = t;
            return t;
        }

        Tile RoadTile(Biome b, int v)
        {
            if (roadTiles.TryGetValue((b, v), out var t)) return t;
            t = MakeTile(PixelArt.PathTile(b, seed + v * 13), $"{b} road {v}");
            roadTiles[(b, v)] = t;
            return t;
        }

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                var b = biomes[x, y];

                if (road[x, y])
                    ground.SetTile(cell, RoadTile(b, rng.Next(2)));
                else
                {
                    float patch = Mathf.PerlinNoise(seed * 0.013f + x * 0.09f, seed * 0.017f + y * 0.09f);
                    double roll = rng.NextDouble();
                    int variant = roll < 0.75 ? rng.Next(3) : roll < 0.87 ? 3 : roll < 0.94 ? 4 : 5;
                    var (tile, glowTile) = GroundTile(b, patch > 0.55f ? 1 : 0, variant);
                    ground.SetTile(cell, tile);
                    if (glowTile != null) glow.SetTile(cell, glowTile);
                }

                if (blocked[x, y])
                {
                    var (tile, glowTile) = ObstacleTile(b, PickObstacle(b, rng.NextDouble()));
                    obstacles.SetTile(cell, tile);
                    if (glowTile != null) glow.SetTile(cell, glowTile);
                }
            }

        if (treesBlockMovement) obstacles.gameObject.AddComponent<TilemapCollider2D>();
    }

    static int PickObstacle(Biome b, double r) => b switch
    {
        Biome.DarkForest => r < 0.45 ? 0 : r < 0.92 ? 1 : 2,  // pine, gnarled tree, watcher tree
        Biome.Bloodmoor => r < 0.6 ? 0 : 1,                    // dead tree, crimson tree
        Biome.Crypt => r < 0.45 ? 0 : r < 0.7 ? 1 : 2,         // gravestone, pillar, dead tree
        Biome.Ashlands => r < 0.5 ? 0 : r < 0.8 ? 1 : 2,       // basalt, lava pool, charred tree
        _ => r < 0.65 ? 0 : 1,                                 // oak, pine
    };

    Tilemap MakeTilemap(string name, int sortingOrder, bool unlit)
    {
        var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(gridRoot, false);
        var tm = go.AddComponent<Tilemap>();
        var r = go.AddComponent<TilemapRenderer>();
        r.sortingOrder = sortingOrder;
        if (unlit && SpriteFactory.Unlit != null) r.sharedMaterial = SpriteFactory.Unlit;
        return tm;
    }

    void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (child.name != "Map Grid (generated)") continue;
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
        gridRoot = null;
    }

    static Tile MakeTile(PixelCanvas art, string name, bool glowOnly = false)
    {
        var tex = SpriteFactory.ToTexture(art, glowOnly);
        tex.name = name;
        tex.hideFlags = HideFlags.DontSave;
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                   new Vector2(0.5f, 0.5f), PixelsPerTile);
        sprite.name = name;
        sprite.hideFlags = HideFlags.DontSave;
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.name = name;
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.Grid;
        tile.hideFlags = HideFlags.DontSave;
        return tile;
    }

    /// <summary>A tile with only the glowing pixels (or null if nothing glows).</summary>
    static Tile MakeGlowTile(PixelCanvas art, string name)
    {
        if (!art.HasGlow) return null;
        var t = MakeTile(art, name, glowOnly: true);
        t.colliderType = Tile.ColliderType.None;
        return t;
    }
}
