using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Progression through the wild lands. They're opened in this order, each harder than the last:
///   1. Dark Forest  - open from the start
///   2. Crypt Lands  - needs the Crypt Key   (dropped by the Spider Queen, boss of the Dark Forest)
///   3. Bloodmoor    - needs the Blood Sigil (dropped by the Lich King, boss of the Crypt)
///   4. Ashlands     - needs the Ember Heart (dropped by the Vampire Lord, boss of the Bloodmoor)
/// Locked lands are walled off by a glowing magic seal. Where the road meets the seal there's a
/// Seal Gate: bring the right artifact and press E there to break the seal.
/// Raiders only come out of lands you've opened, so opening a land also brings its monsters to your base.
/// </summary>
public class AreaLocks : MonoBehaviour
{
    /// <summary>The wild lands, easiest first.</summary>
    public static readonly Biome[] Order = { Biome.DarkForest, Biome.Crypt, Biome.Bloodmoor, Biome.Ashlands };

    [Header("Difficulty of each land (Dark Forest, Crypt, Bloodmoor, Ashlands)")]
    [Tooltip("Monster health multiplier in each land.")]
    public float[] healthScale = { 1f, 1.7f, 2.6f, 3.8f };
    [Tooltip("Monster damage multiplier in each land.")]
    public float[] damageScale = { 1f, 1.35f, 1.75f, 2.2f };

    [Tooltip("How close the hero must be to use a Seal Gate.")]
    public float interactRange = 2.2f;

    class Seal { public Biome Biome; public GameObject Wall, Gate; public Vector3 GatePos; }

    readonly HashSet<Biome> unlocked = new();
    readonly HashSet<Biome> artifacts = new(); // artifacts held but not used yet (keyed by the land they open)
    readonly List<Seal> seals = new();
    readonly Dictionary<(Biome, int), Tile> tiles = new();

    /// <summary>The seal gate the hero is standing at (null if none).</summary>
    public Biome? NearbyGate { get; private set; }
    public Vector3 NearbyGatePos { get; private set; }

    public static AreaLocks Instance => GameManager.Instance != null ? GameManager.Instance.Locks : null;

    // ---------- Questions the rest of the game asks ----------

    public bool IsUnlocked(Biome b) => b == Biome.Meadow || unlocked.Contains(b);
    public bool HasArtifact(Biome opens) => artifacts.Contains(opens);
    public IEnumerable<Biome> HeldArtifacts => artifacts;

    /// <summary>0 for the Meadow, 1 for the Dark Forest ... 4 for the Ashlands.</summary>
    public static int Tier(Biome b) => System.Array.IndexOf(Order, b) + 1;

    public float HealthScale(Biome b) => Scale(healthScale, b);
    public float DamageScale(Biome b) => Scale(damageScale, b);
    static float Scale(float[] table, Biome b)
    {
        int i = Tier(b) - 1;
        return i < 0 || table == null || table.Length == 0 ? 1f : table[Mathf.Min(i, table.Length - 1)];
    }

    /// <summary>The next land after this one (null after the Ashlands).</summary>
    public static Biome? NextAfter(Biome b)
    {
        int i = System.Array.IndexOf(Order, b);
        return i >= 0 && i + 1 < Order.Length ? Order[i + 1] : null;
    }

    /// <summary>The land whose boss drops the artifact for this one.</summary>
    public static Biome? PreviousOf(Biome b)
    {
        int i = System.Array.IndexOf(Order, b);
        return i > 0 ? Order[i - 1] : null;
    }

    // ---------- Setup ----------

    public void ResetAll(MapGenerator map)
    {
        foreach (var s in seals)
        {
            if (s.Wall != null) Destroy(s.Wall);
            if (s.Gate != null) Destroy(s.Gate);
        }
        seals.Clear();
        unlocked.Clear();
        artifacts.Clear();
        unlocked.Add(Order[0]);

        for (int i = 1; i < Order.Length; i++) seals.Add(BuildSeal(map, Order[i]));
    }

    /// <summary>A glowing wall all along the land's border (with a collider), plus a gate on the road.</summary>
    Seal BuildSeal(MapGenerator map, Biome biome)
    {
        var seal = new Seal { Biome = biome };

        var root = new GameObject("Seal: " + biome);
        root.AddComponent<Grid>();
        root.transform.position = map.transform.position + new Vector3(-map.width / 2f, -map.height / 2f, 0f);
        var wallGo = new GameObject("Seal Wall");
        wallGo.transform.SetParent(root.transform, false);
        var tm = wallGo.AddComponent<Tilemap>();
        var tr = wallGo.AddComponent<TilemapRenderer>();
        tr.sortingOrder = 4;
        if (SpriteFactory.Unlit != null) tr.sharedMaterial = SpriteFactory.Unlit; // seals glow in the dark

        // Border cells: cells of this land that touch a different land.
        Vector3 hubWorld = map.CellToWorld(map.CenterCell);
        float bestRoadDist = float.MaxValue;
        Vector2Int? gateCell = null;
        for (int x = 0; x < map.width; x++)
            for (int y = 0; y < map.height; y++)
            {
                var c = new Vector2Int(x, y);
                if (map.BiomeAt(c) != biome || !TouchesOtherLand(map, c, biome)) continue;
                tm.SetTile(new Vector3Int(x, y, 0), WallTile(biome, (x * 7 + y * 3) % 4));

                // The gate goes where the road crosses the border closest to the Hub.
                if (map.IsRoad(c))
                {
                    float d = Vector3.Distance(map.CellToWorld(c), hubWorld);
                    if (d < bestRoadDist) { bestRoadDist = d; gateCell = c; }
                }
            }
        wallGo.AddComponent<TilemapCollider2D>();
        seal.Wall = root;

        if (gateCell == null) gateCell = map.DungeonSite(biome); // shouldn't happen: every land has a road
        seal.GatePos = map.CellToWorld(gateCell.Value);
        var gate = new GameObject("Seal Gate: " + biome).AddComponent<SpriteRenderer>();
        gate.transform.position = seal.GatePos;
        gate.sprite = SpriteFactory.SealGate(biome);
        gate.sortingOrder = 6;
        SpriteFactory.AddGlow(gate);
        GlowPulse.On(gate, GlowPulse.Mode.Pulse);
        Idle.On(gate.gameObject, Idle.Kind.Hum);
        NightLighting.AddPointLight(gate.transform, Vector3.up * 0.5f, 3.5f, Ui.Hex(PixelArt.SealColorHex(biome)), 0.9f)
            .gameObject.AddComponent<LightFlicker>();
        seal.Gate = gate.gameObject;
        return seal;
    }

    static bool TouchesOtherLand(MapGenerator map, Vector2Int c, Biome biome)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                var n = c + new Vector2Int(dx, dy);
                if (map.InBounds(n) && map.BiomeAt(n) != biome) return true;
            }
        return false;
    }

    Tile WallTile(Biome b, int variant)
    {
        if (tiles.TryGetValue((b, variant), out var t) && t != null) return t;
        var tex = SpriteFactory.ToTexture(PixelArt.SealWall(b, variant));
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16);
        t = ScriptableObject.CreateInstance<Tile>();
        t.sprite = sprite;
        t.colliderType = Tile.ColliderType.Grid;
        tiles[(b, variant)] = t;
        return t;
    }

    // ---------- Each frame ----------

    void Update()
    {
        NearbyGate = null;
        var gm = GameManager.Instance;
        if (gm == null || gm.Player == null || !gm.Player.IsAlive || gm.Dungeons.Current != null) return;
        Vector3 p = gm.Player.transform.position;
        foreach (var s in seals)
        {
            if (s.Gate == null) continue;
            if (Vector2.Distance(p, s.GatePos) < interactRange)
            {
                NearbyGate = s.Biome;
                NearbyGatePos = s.GatePos;
            }
        }
    }

    /// <summary>E at a seal gate. Returns true if it did something.</summary>
    public bool TryInteract()
    {
        if (NearbyGate == null) return false;
        var gm = GameManager.Instance;
        var b = NearbyGate.Value;
        if (!HasArtifact(b))
        {
            gm.ShowPopup(NearbyGatePos + Vector3.up * 1.5f, $"Sealed. Needs the {PixelArt.ArtifactName(b)}", Ui.Hex(PixelArt.SealColorHex(b)));
            return true;
        }
        Unlock(b);
        return true;
    }

    void Unlock(Biome b)
    {
        var gm = GameManager.Instance;
        artifacts.Remove(b);
        unlocked.Add(b);
        var seal = seals.Find(s => s.Biome == b);
        if (seal != null)
        {
            if (seal.Wall != null) Destroy(seal.Wall);
            if (seal.Gate != null)
            {
                // The gate shatters.
                var gateSr = seal.Gate.GetComponent<SpriteRenderer>();
                if (gateSr != null) Corpse.From(gateSr, crumble: true, duration: 1.2f);
                Juice.Sparkle(seal.GatePos, Ui.Hex(PixelArt.SealColorHex(b)), 30, 1f);
                Juice.Shake(0.4f);
                Destroy(seal.Gate);
            }
            seals.Remove(seal);
        }
        string name = PixelArt.BiomeName(b);
        gm.Announce($"The {PixelArt.ArtifactName(b)} breaks the seal! {char.ToUpper(name[0]) + name.Substring(1)} is open - " +
                    "but its monsters (much tougher ones) will now raid your base too.");
    }

    /// <summary>A dungeon boss died: hand out the artifact for the next land.</summary>
    public void OnBossKilled(Biome land, Vector3 at)
    {
        var next = NextAfter(land);
        if (next == null || IsUnlocked(next.Value) || artifacts.Contains(next.Value)) return;
        artifacts.Add(next.Value);
        var gm = GameManager.Instance;
        gm.ShowPopup(at + Vector3.up * 1.4f, "+ " + PixelArt.ArtifactName(next.Value), Ui.Hex(PixelArt.SealColorHex(next.Value)));
    }

    /// <summary>Text for the HUD/boss announcement: what to do next.</summary>
    public static string HintFor(Biome opens) =>
        $"Take the {PixelArt.ArtifactName(opens)} to the Seal Gate of {PixelArt.BiomeName(opens)} and press E.";
}
