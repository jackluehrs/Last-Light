using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>One dungeon: its entrance on the map, and (once visited) its rooms far away from the map.</summary>
public class Dungeon
{
    public Biome Biome;
    public int Index;
    public GameObject Entrance;
    public Vector3 EntrancePos;
    public bool Generated, Cleared;

    public GameObject Root;
    public Rect Bounds;
    public Vector3 Start, StairsPos, BossRoomCenter;
    public GameObject Portal;
    public Enemy Boss;

    public string Name => Biome switch
    {
        Biome.DarkForest => "the Spider Den",
        Biome.Bloodmoor => "the Blood Keep",
        Biome.Crypt => "the Lich's Tomb",
        _ => "the Molten Depths",
    };

    public OreType Ore => GameManager.OreOf(Biome);
}

/// <summary>
/// Dungeons and boss fights.
/// Each wild land has a dungeon entrance. Walk up to it and press E to go in.
/// Inside: a chain of rooms full of that land's monsters, and its boss at the end.
/// Beat the boss for a big reward (gold, ore and a skill point), then take the portal home.
/// Press E at the stairs to leave early. While you're inside, your base still has to defend itself!
/// </summary>
public class DungeonManager : MonoBehaviour
{
    public readonly List<Dungeon> All = new();

    /// <summary>The dungeon the hero is in right now (null = out on the map).</summary>
    public Dungeon Current { get; private set; }

    /// <summary>The entrance the hero is standing next to (on the map).</summary>
    public Dungeon NearbyEntrance { get; private set; }

    /// <summary>True if the hero is next to the stairs or the portal (inside a dungeon).</summary>
    public bool NearExit { get; private set; }

    [Tooltip("How close the hero must be to use an entrance, stairs or portal.")]
    public float interactRange = 1.8f;

    [Header("Rewards for beating a dungeon boss")]
    public int bossGold = 150;
    public int bossOre = 12;

    const float FarAway = 2000f; // dungeons are built this far to the right of the map

    readonly Dictionary<(Biome, int), Tile> tileCache = new();

    // ---------- Setup ----------

    public void ResetAll(MapGenerator map)
    {
        foreach (var d in All)
        {
            if (d.Entrance != null) Destroy(d.Entrance);
            if (d.Root != null) Destroy(d.Root);
        }
        All.Clear();
        Current = null;

        int i = 0;
        foreach (var biome in MapGenerator.WildBiomes)
        {
            var cell = map.DungeonSite(biome);
            Vector3 pos = (map.CellToWorld(cell) + map.CellToWorld(cell + Vector2Int.one)) / 2f;

            var go = new GameObject("Dungeon Entrance: " + biome);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.DungeonEntrance(biome);
            sr.sortingOrder = 5;
            SpriteFactory.AddGlow(sr);
            GlowPulse.On(sr, GlowPulse.Mode.Pulse);
            NightLighting.AddPointLight(go.transform, new Vector3(0f, -0.3f, 0f), 4f, TorchColor(biome), 0.8f)
                .gameObject.AddComponent<LightFlicker>();

            All.Add(new Dungeon { Biome = biome, Index = i++, Entrance = go, EntrancePos = pos });
        }
    }

    static Color TorchColor(Biome b) => b switch
    {
        Biome.Bloodmoor => new Color(1f, 0.45f, 0.45f),
        Biome.Crypt => new Color(0.5f, 1f, 0.7f),
        Biome.Ashlands => new Color(1f, 0.6f, 0.3f),
        _ => new Color(1f, 0.75f, 0.45f),
    };

    // ---------- Each frame ----------

    void Update()
    {
        var gm = GameManager.Instance;
        NearbyEntrance = null;
        NearExit = false;
        if (gm == null || gm.Player == null || !gm.Player.IsAlive) return;
        Vector3 p = gm.Player.transform.position;

        if (Current == null)
        {
            foreach (var d in All)
                if (Vector2.Distance(p, d.EntrancePos + Vector3.down * 0.6f) < interactRange) NearbyEntrance = d;
        }
        else
        {
            NearExit = Vector2.Distance(p, Current.StairsPos) < interactRange ||
                       (Current.Portal != null && Vector2.Distance(p, Current.Portal.transform.position) < interactRange);
        }
    }

    /// <summary>Called when the hero presses E. Returns true if it did something (so E doesn't also upgrade).</summary>
    public bool TryInteract()
    {
        var gm = GameManager.Instance;
        if (Current == null && NearbyEntrance != null)
        {
            if (NearbyEntrance.Cleared)
            {
                gm.ShowPopup(NearbyEntrance.EntrancePos + Vector3.up, "Already cleared", Color.white);
                return true;
            }
            Enter(NearbyEntrance);
            return true;
        }
        if (Current != null && NearExit)
        {
            Exit(toHub: false);
            return true;
        }
        return false;
    }

    public void Enter(Dungeon d)
    {
        var gm = GameManager.Instance;
        if (!d.Generated) Generate(d, gm);
        Current = d;
        gm.Player.TeleportTo(d.Start);
        gm.SnapCamera();
        gm.Announce(d.Cleared
            ? $"You enter {d.Name}. It's quiet now."
            : $"You enter {d.Name}... {EnemyDefs.Get(EnemyDefs.BossOf(d.Biome)).Name} waits at the end. (E at the stairs to leave)");
    }

    /// <summary>Leave the dungeon: back out the entrance, or straight home (recall).</summary>
    public void Exit(bool toHub)
    {
        var gm = GameManager.Instance;
        var d = Current;
        Current = null;
        if (d == null || gm.Player == null) return;
        gm.Player.TeleportTo(toHub ? gm.PlayerSpawnPoint : d.EntrancePos + Vector3.down * 1.6f);
        gm.SnapCamera();
    }

    /// <summary>The hero died: they'll respawn at the Hub, outside.</summary>
    public void OnPlayerDied() => Current = null;

    public void OnBossKilled(Enemy boss)
    {
        var d = All.Find(x => x == boss.Dungeon);
        if (d == null) return;
        var gm = GameManager.Instance;
        d.Cleared = true;

        // Treasure! (more for harder lands)
        int tier = Mathf.Max(1, AreaLocks.Tier(d.Biome));
        int gold = bossGold * tier;
        gm.AddOre(OreType.Gold, gold, boss.transform.position + Vector3.up * 0.8f);
        gm.AddOre(d.Ore, bossOre, boss.transform.position + Vector3.up * 0.3f);
        if (gm.Player != null) gm.Player.Skills.AddPoint();

        // ...and the artifact that opens the next land.
        var next = AreaLocks.NextAfter(d.Biome);
        bool newArtifact = next != null && !gm.Locks.IsUnlocked(next.Value) && !gm.Locks.HasArtifact(next.Value);
        gm.Locks.OnBossKilled(d.Biome, boss.transform.position);
        string artifactText = newArtifact
            ? $" You found the {PixelArt.ArtifactName(next.Value)}! {AreaLocks.HintFor(next.Value)}"
            : next == null ? " Every land is free of its master!" : "";
        gm.Announce($"{boss.Def.Name} is defeated! +{gold} gold, +{bossOre} {PixelArt.OreName(d.Ore)}, +1 skill point.{artifactText} Take the portal home.");

        // A portal home appears in the boss room
        var sr = new GameObject("Portal").AddComponent<SpriteRenderer>();
        sr.transform.SetParent(d.Root.transform, true);
        sr.transform.position = d.BossRoomCenter;
        sr.sprite = SpriteFactory.Portal();
        sr.sortingOrder = 3;
        SpriteFactory.MakeUnlit(sr);
        Idle.On(sr.gameObject, Idle.Kind.Swirl);
        Juice.Sparkle(d.BossRoomCenter, new Color(0.75f, 0.55f, 1f), 30, 1f);
        NightLighting.AddPointLight(sr.transform, Vector3.zero, 5f, new Color(0.7f, 0.5f, 1f), 1.2f)
            .gameObject.AddComponent<LightFlicker>();
        d.Portal = sr.gameObject;

        // The entrance on the map goes dark-ish to show it's done
        var esr = d.Entrance.GetComponent<SpriteRenderer>();
        if (esr != null) esr.color = new Color(0.7f, 0.7f, 0.75f);
    }

    // ---------- Building a dungeon ----------

    /// <summary>
    /// A chain of four rooms joined by corridors, left to right:
    /// the entrance hall (with the stairs out), two monster rooms and the boss room.
    /// </summary>
    void Generate(Dungeon d, GameManager gm)
    {
        d.Generated = true;
        Vector3 origin = new Vector3(FarAway + d.Index * 300f, 0f, 0f);
        d.Root = new GameObject("Dungeon: " + d.Name);
        d.Root.transform.position = origin;

        d.Root.AddComponent<Grid>();
        var floor = MakeTilemap(d.Root.transform, "Floor", 0, false);
        var walls = MakeTilemap(d.Root.transform, "Walls", 1, false);
        var glow = MakeTilemap(d.Root.transform, "Glow", 3, true);

        Vector2Int[] sizes = { new(9, 9), new(13, 11), new(15, 13), new(19, 15) };
        const int corridor = 5;
        var rooms = new List<RectInt>();
        int cursor = 0;
        var floorCells = new HashSet<Vector2Int>();
        for (int i = 0; i < sizes.Length; i++)
        {
            var r = new RectInt(cursor, -sizes[i].y / 2, sizes[i].x, sizes[i].y);
            rooms.Add(r);
            foreach (var c in r.allPositionsWithin) floorCells.Add(c);
            if (i < sizes.Length - 1)
                for (int x = r.xMax; x < r.xMax + corridor; x++)
                    for (int y = -1; y <= 1; y++) floorCells.Add(new Vector2Int(x, y));
            cursor += sizes[i].x + corridor;
        }

        // Floor, then a ring of wall around every floor cell
        var rng = new System.Random(d.Index * 97 + 5);
        var wallCells = new HashSet<Vector2Int>();
        foreach (var c in floorCells)
        {
            int v = rng.NextDouble() < 0.1 ? 2 : rng.Next(2);
            floor.SetTile((Vector3Int)c, FloorTile(d.Biome, v));
            if (v == 2 && d.Biome == Biome.Ashlands) glow.SetTile((Vector3Int)c, FloorGlowTile(d.Biome));
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    var n = c + new Vector2Int(dx, dy);
                    if (!floorCells.Contains(n)) wallCells.Add(n);
                }
        }
        var wallTile = WallTile(d.Biome);
        foreach (var c in wallCells) walls.SetTile((Vector3Int)c, wallTile);
        walls.gameObject.AddComponent<TilemapCollider2D>();

        Vector3 W(Vector2 cell) => origin + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

        // Torches along the top wall of each room
        foreach (var r in rooms)
            for (int x = r.xMin + 1; x < r.xMax; x += 4)
            {
                var t = new GameObject("Wall Torch").AddComponent<SpriteRenderer>();
                t.transform.SetParent(d.Root.transform, false);
                t.transform.position = W(new Vector2(x, r.yMax)) + Vector3.down * 0.2f;
                t.sprite = SpriteFactory.WallTorch();
                t.sortingOrder = 4;
                SpriteFactory.MakeUnlit(t);
                Idle.On(t.gameObject, Idle.Kind.Flicker);
                NightLighting.AddPointLight(t.transform, Vector3.down * 0.5f, 4f, TorchColor(d.Biome), 0.8f)
                    .gameObject.AddComponent<LightFlicker>();
            }

        // Stairs out, in the first room
        var stairsCell = new Vector2(rooms[0].xMin + 1, 0);
        d.StairsPos = W(stairsCell);
        var stairs = new GameObject("Stairs Up").AddComponent<SpriteRenderer>();
        stairs.transform.SetParent(d.Root.transform, false);
        stairs.transform.position = d.StairsPos;
        stairs.sprite = SpriteFactory.StairsUp();
        stairs.sortingOrder = 2;
        SpriteFactory.AddGlow(stairs);
        GlowPulse.On(stairs, GlowPulse.Mode.Pulse);
        d.Start = W(stairsCell + new Vector2(2, 0));

        var bossRoom = rooms[rooms.Count - 1];
        d.BossRoomCenter = W(bossRoom.center - Vector2.one * 0.5f);
        d.Bounds = new Rect(origin.x - 2f, origin.y + rooms[3].yMin - 2f, cursor + 2f, rooms[3].height + 4f);

        // Monsters
        float hp = Mathf.Max(1f, gm.Spawner.HpMultiplier);
        void Fill(RectInt r, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var c = new Vector2(Random.Range(r.xMin + 2, r.xMax - 2), Random.Range(r.yMin + 2, r.yMax - 2));
                Enemy.Spawn(EnemyDefs.Pick(d.Biome, 0f, ignoreTime: true, biomeOnly: true), W(c), hp, EnemyMode.Dungeon, d);
            }
        }
        Fill(rooms[1], 4);
        Fill(rooms[2], 6);
        Fill(new RectInt(bossRoom.xMin, bossRoom.yMin, bossRoom.width / 2, bossRoom.height), 2);
        d.Boss = Enemy.Spawn(EnemyDefs.BossOf(d.Biome), d.BossRoomCenter + Vector3.right * 3f, hp, EnemyMode.Dungeon, d);
    }

    static Tilemap MakeTilemap(Transform parent, string name, int order, bool unlit)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tm = go.AddComponent<Tilemap>();
        var r = go.AddComponent<TilemapRenderer>();
        r.sortingOrder = order;
        if (unlit && SpriteFactory.Unlit != null) r.sharedMaterial = SpriteFactory.Unlit;
        return tm;
    }

    Tile FloorTile(Biome b, int v) => CachedTile((b, v), () => PixelArt.DungeonFloor(b, v), false);
    Tile FloorGlowTile(Biome b) => CachedTile((b, 100), () => PixelArt.DungeonFloor(b, 2), true);
    Tile WallTile(Biome b) => CachedTile((b, 200), () => PixelArt.DungeonWall(b), false);

    Tile CachedTile((Biome, int) key, System.Func<PixelCanvas> draw, bool glowOnly)
    {
        if (tileCache.TryGetValue(key, out var t) && t != null) return t;
        var tex = SpriteFactory.ToTexture(draw(), glowOnly);
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16);
        t = ScriptableObject.CreateInstance<Tile>();
        t.sprite = sprite;
        t.colliderType = glowOnly ? Tile.ColliderType.None : Tile.ColliderType.Grid;
        tileCache[key] = t;
        return t;
    }
}
