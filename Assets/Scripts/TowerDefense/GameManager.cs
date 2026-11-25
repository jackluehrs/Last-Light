using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The brain of the game: resources, the Hub, the build grid, ore nodes, win/lose.
/// The HUD lives in GameManager.Hud.cs; dungeons in DungeonManager.
///
/// You don't have to add this to the scene: if it's missing, it creates itself when you press Play.
/// Add it with Tools > Create Tower Defense Game if you want to tweak values in the Inspector.
/// </summary>
[RequireComponent(typeof(EnemySpawner), typeof(BuildController), typeof(NightLighting))]
[RequireComponent(typeof(SkillTreeUI), typeof(DungeonManager), typeof(AreaLocks))]
public partial class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Camera")]
    [Tooltip("How far the camera is zoomed out (half the screen height, in tiles).")]
    public float cameraZoom = 8f;

    [Header("Economy")]
    public int startingGold = 100;
    [Tooltip("Free gold the hub makes every second (times the Hub's level).")]
    public float hubIncomePerSecond = 0.25f;

    [Header("Ore")]
    [Tooltip("Gold ore nodes in the Meadow around the Hub.")]
    public int meadowOreCount = 26;
    [Tooltip("How many of those are guaranteed to spawn just outside the hub.")]
    public int nearbyOreCount = 5;
    [Tooltip("Ore won't spawn closer than this many tiles to the hub.")]
    public float oreMinDistanceFromHub = 3f;
    [Tooltip("Ore nodes in each wild land (most are that land's special ore, some are gold).")]
    public int oresPerWildLand = 10;
    [Tooltip("How many hero mining hits it takes to get 1 special ore (gold is 1 per hit).")]
    public int hitsPerSpecialOre = 3;
    [Tooltip("Chance that a special ore node has monsters guarding it.")]
    [Range(0f, 1f)] public float guardChance = 0.6f;

    public MapGenerator Map { get; private set; }
    public Building Hub { get; private set; }
    public float ElapsedTime { get; private set; }
    public int Kills { get; private set; }
    public bool IsGameOver { get; private set; }
    public PlayerController Player { get; private set; }
    public BuildController Builder => builder;
    public NightLighting Lighting { get; private set; }
    public SkillTreeUI SkillMenu { get; private set; }
    public DungeonManager Dungeons { get; private set; }
    public AreaLocks Locks { get; private set; }
    public EnemySpawner Spawner => spawner;

    /// <summary>True while a menu (the skill tree) has the game paused.</summary>
    public bool IsMenuOpen => SkillMenu != null && SkillMenu.IsOpen;

    /// <summary>The camera stays inside this: the map, or the dungeon you're in.</summary>
    public Rect CurrentBounds => Dungeons.Current != null ? Dungeons.Current.Bounds : Map.WorldBounds;

    /// <summary>Which land the hero is standing in.</summary>
    public Biome PlayerBiome => Dungeons.Current != null ? Dungeons.Current.Biome
                              : Player != null ? Map.BiomeAt(Player.transform.position) : Biome.Meadow;

    // ---- Resources ----
    readonly int[] stock = new int[5];
    readonly float[] oreCarry = new float[5];

    public int Gold { get => stock[0]; private set => stock[0] = value; }
    public int Amount(OreType t) => stock[(int)t];

    /// <summary>The special ore each wild land has.</summary>
    public static OreType OreOf(Biome b) => b switch
    {
        Biome.DarkForest => OreType.Iron,
        Biome.Crypt => OreType.SoulCrystal,
        Biome.Bloodmoor => OreType.Bloodstone,
        Biome.Ashlands => OreType.Emberite,
        _ => OreType.Gold,
    };

    public bool CanAfford(int gold, OreType ore, int oreAmount) => Gold >= gold && (oreAmount <= 0 || Amount(ore) >= oreAmount);

    /// <summary>"Need 20 more gold" / "Need 3 more Iron", or null if you can afford it.</summary>
    public string MissingText(int gold, OreType ore, int oreAmount)
    {
        if (Gold < gold) return $"Need {gold - Gold} more gold";
        if (oreAmount > 0 && Amount(ore) < oreAmount) return $"Need {oreAmount - Amount(ore)} more {PixelArt.OreName(ore)}";
        return null;
    }

    void Spend(int gold, OreType ore, int oreAmount)
    {
        Gold -= gold;
        if (oreAmount > 0) stock[(int)ore] -= oreAmount;
    }

    /// <summary>Add gold or ore, with a little "+3" popup at a position.</summary>
    public void AddOre(OreType type, int amount, Vector3 popupAt)
    {
        if (amount <= 0) return;
        stock[(int)type] += amount;
        ShowPopup(popupAt, type == OreType.Gold ? "+" + amount : $"+{amount} {PixelArt.OreName(type)}", OreColor(type));
    }

    public void AddGold(int amount) => Gold += amount;

    public static Color OreColor(OreType t) => Ui.Hex(PixelArt.OreColorHex(t));

    // ---- Effects of skills and the Hub on the base ----
    public int MinerBonusGold => Player != null ? Player.Skills.Rank("prospector") : 0;
    public float TowerDamageMultiplier =>
        (Player != null && Player.Skills.Has("warden") ? 1.25f : 1f) * (Hub != null && Hub.Level >= HubLevels.SunBeaconLevel ? 1.25f : 1f);
    public bool HasUnlocked(BuildType t) => Hub != null && Hub.Level >= BuildingDef.Get(t).HubLevel;

    /// <summary>Upgrade price (gold part) after the Engineer skill's discount.</summary>
    public int UpgradeCostFor(Building b)
    {
        float discount = Player != null ? 0.2f * Player.Skills.Rank("engineer") : 0f;
        return Mathf.Max(1, Mathf.RoundToInt(b.UpgradeCost * (1f - discount)));
    }

    /// <summary>Where the player appears: just below the hub.</summary>
    public Vector3 PlayerSpawnPoint => Hub.transform.position + Vector3.down * 1.6f;

    public readonly List<Building> Buildings = new();
    public readonly List<Enemy> Enemies = new();

    class OreSpot { public GameObject Go; public OreType Type; }

    readonly Dictionary<Vector2Int, Building> occupied = new();
    readonly Dictionary<Vector2Int, OreSpot> ore = new();
    readonly List<Popup> popups = new();

    EnemySpawner spawner;
    BuildController builder;
    float incomeCarry;
    float repairTimer;
    float lastHubWarning = -999f;
    Biome lastBiome;
    string announcement;
    float announcementTimer;

    struct Popup { public Vector3 pos; public string text; public Color color; public float age; }

    // ---------- Auto setup ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (FindAnyObjectByType<GameManager>() == null)
            new GameObject("Game Manager").AddComponent<GameManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        spawner = GetComponent<EnemySpawner>();
        builder = GetComponent<BuildController>();
        Lighting = GetComponent<NightLighting>();
        SkillMenu = GetComponent<SkillTreeUI>();
        Dungeons = GetComponent<DungeonManager>();
        Locks = GetComponent<AreaLocks>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        Map = FindAnyObjectByType<MapGenerator>();
        if (Map == null) Map = new GameObject("Map").AddComponent<MapGenerator>();
        // The map's size comes from the Map object, so what you see in the editor is what you play.
        Lighting.Setup();
        NewGame();
    }

    void NewGame()
    {
        // Clean up anything left over from a previous round.
        foreach (var e in Enemies.ToArray()) if (e != null) Destroy(e.gameObject);
        foreach (var b in Buildings.ToArray()) if (b != null) Destroy(b.gameObject);
        foreach (var o in ore.Values) if (o.Go != null) Destroy(o.Go);
        if (Player != null) Destroy(Player.gameObject);
        foreach (var shot in FindObjectsByType<EnemyBolt>()) Destroy(shot.gameObject);
        Enemies.Clear(); Buildings.Clear(); occupied.Clear(); ore.Clear(); popups.Clear();

        SkillMenu.SetOpen(false);
        Time.timeScale = 1f;
        IsGameOver = false;
        System.Array.Clear(stock, 0, stock.Length);
        System.Array.Clear(oreCarry, 0, oreCarry.Length);
        Gold = startingGold;
        ElapsedTime = 0f;
        Kills = 0;
        incomeCarry = 0f;
        lastBiome = Biome.Meadow;

        CreateHub();
        Dungeons.ResetAll(Map);
        Locks.ResetAll(Map);
        SpawnOre();
        Player = PlayerController.Spawn(PlayerSpawnPoint);
        SetUpCamera();
        spawner.ResetSpawner();
        FogOfWar.ResetExplored(Map);
        BuildMinimap();
        Announce("Hold Space next to gold ore to mine it! Press K to pick a class. The Dark Forest lies west - the other lands are sealed until you find their keys...");
    }

    void CreateHub()
    {
        // The hub is 2x2 tiles in the middle of the map.
        var c = Map.CenterCell;
        var cells = new List<Vector2Int>
        {
            new(c.x - 1, c.y - 1), new(c.x, c.y - 1), new(c.x - 1, c.y), new(c.x, c.y)
        };
        Hub = CreateBuilding(BuildType.Hub, cells);
        Hub.Health.Damaged += OnHubDamaged;

        // The hub has a weak built-in gun so you aren't defenseless at the start.
        var gun = Hub.gameObject.AddComponent<Tower>();
        gun.range = 3f;
        gun.damage = 5f;
        gun.fireInterval = 1f;
        gun.projectileColor = new Color(1f, 0.85f, 0.5f);
        gun.crystalHeight = 0.3f;
    }

    void SetUpCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.zoom = cameraZoom;
        follow.target = Player.transform;
        follow.SnapToTarget();
    }

    /// <summary>Jump the camera straight to the hero (after teleporting).</summary>
    public void SnapCamera()
    {
        var follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (follow != null) follow.SnapToTarget();
    }

    /// <summary>Warn the player (at most every 8 seconds) when the hub is hit while it's off screen.</summary>
    void OnHubDamaged(float amount)
    {
        if (Time.time - lastHubWarning < 8f) return;
        var cam = Camera.main;
        if (cam != null && Dungeons.Current == null)
        {
            Vector3 vp = cam.WorldToViewportPoint(Hub.transform.position);
            if (vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f) return; // you can already see it
        }
        lastHubWarning = Time.time;
        Announce(Dungeons.Current != null ? "Your Hub is under attack! (Recall with H, or take the stairs)" : "Your Hub is under attack!");
    }

    // ---------- Ore ----------

    void SpawnOre()
    {
        Vector3 hubPos = Hub.transform.position;

        // A few guaranteed gold nodes close to the hub, so the hero can start mining right away.
        int attempts = 0;
        while (ore.Count < nearbyOreCount && attempts++ < 500)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(oreMinDistanceFromHub, oreMinDistanceFromHub + 3f);
            TryPlaceOre(Map.WorldToCell(hubPos + (Vector3)offset), Biome.Meadow, OreType.Gold, false);
        }

        // Each land gets its own share. The wild lands mostly have their special ore, often guarded.
        foreach (var biome in new[] { Biome.Meadow, Biome.DarkForest, Biome.Bloodmoor, Biome.Crypt, Biome.Ashlands })
        {
            int want = biome == Biome.Meadow ? meadowOreCount - nearbyOreCount : oresPerWildLand;
            int placed = 0;
            attempts = 0;
            while (placed < want && attempts++ < 4000)
            {
                var c = new Vector2Int(Random.Range(0, Map.width), Random.Range(0, Map.height));
                if (Map.BiomeAt(c) != biome) continue;
                bool special = biome != Biome.Meadow && placed < want * 0.7f;
                var type = special ? OreOf(biome) : OreType.Gold;
                if (TryPlaceOre(c, biome, type, special && Random.value < guardChance)) placed++;
            }
        }
    }

    bool TryPlaceOre(Vector2Int c, Biome biome, OreType type, bool guarded)
    {
        if (Map.IsTree(c) || Map.IsRoad(c) || occupied.ContainsKey(c) || ore.ContainsKey(c)) return false;
        Vector3 pos = Map.CellToWorld(c);
        if (Vector3.Distance(pos, Hub.transform.position) < oreMinDistanceFromHub) return false;
        foreach (var d in Dungeons.All) if (Vector3.Distance(pos, d.EntrancePos) < 3f) return false;

        var go = new GameObject(PixelArt.OreName(type) + " Ore");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.OreNode(type);
        sr.sortingOrder = 2;
        SpriteFactory.AddGlow(sr); // gold glints and crystals you can spot in the dark
        GlowPulse.On(sr, GlowPulse.Mode.Pulse);
        Idle.On(go, Idle.Kind.Glint); // a sparkle now and then
        ore[c] = new OreSpot { Go = go, Type = type };

        if (guarded)
        {
            int count = Random.Range(1, 3);
            for (int i = 0; i < count; i++)
            {
                Vector3 p = pos + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(0.9f, 1.6f));
                Enemy.Spawn(EnemyDefs.Pick(biome, 0f, ignoreTime: true, biomeOnly: true), p, 1f, EnemyMode.Guard);
            }
        }
        return true;
    }

    // ---------- Game loop ----------

    void Update()
    {
        if (IsGameOver)
        {
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) NewGame();
            return;
        }

        ElapsedTime += Time.deltaTime;

        incomeCarry += hubIncomePerSecond * Hub.Level * Time.deltaTime; // hub upgrades raise income
        if (incomeCarry >= 1f)
        {
            int whole = Mathf.FloorToInt(incomeCarry);
            incomeCarry -= whole;
            Gold += whole;
        }

        // Hub level 3+: slowly repairs buildings near it.
        if (Hub.Level >= HubLevels.RepairLevel && (repairTimer -= Time.deltaTime) <= 0f)
        {
            repairTimer = 1f;
            foreach (var b in Buildings)
                if (b != null && Vector3.Distance(b.transform.position, Hub.transform.position) < 9f)
                    b.Health.Heal(2f + b.Health.Max * 0.01f);
        }

        // Tell the player when they walk into a new land
        if (Player != null && Player.IsAlive && Dungeons.Current == null)
        {
            var biome = PlayerBiome;
            if (biome != lastBiome)
            {
                lastBiome = biome;
                Announce(biome == Biome.Meadow ? "Back in the Meadow." : $"You enter {PixelArt.BiomeName(biome)}. Look for {PixelArt.OreName(OreOf(biome))}!");
            }
        }

        for (int i = popups.Count - 1; i >= 0; i--)
        {
            var p = popups[i];
            p.age += Time.deltaTime;
            p.pos += Vector3.up * (0.6f * Time.deltaTime);
            if (p.age > 1f) popups.RemoveAt(i); else popups[i] = p;
        }
        if (announcementTimer > 0f) announcementTimer -= Time.deltaTime;
    }

    // ---------- Building ----------

    public bool CanPlace(BuildType type, Vector2Int cell, out string reason)
    {
        reason = null;
        var def = BuildingDef.Get(type);
        if (Dungeons.Current != null) { reason = "Can't build in a dungeon"; return false; }
        if (!HasUnlocked(type)) { reason = $"Upgrade the Hub to level {def.HubLevel} first"; return false; }
        if (!Map.InBounds(cell)) { reason = "Outside the map"; return false; }
        if (Map.IsTree(cell)) { reason = "Something's in the way"; return false; }
        if (occupied.ContainsKey(cell)) { reason = "Something is already here"; return false; }
        bool onOre = ore.ContainsKey(cell);
        if (type == BuildType.Miner && !onOre) { reason = "Miners must go on ore"; return false; }
        if (type != BuildType.Miner && onOre) { reason = "Save ore for Miners"; return false; }
        reason = MissingText(def.Cost, def.Ore, def.OreCost);
        return reason == null;
    }

    public bool Build(BuildType type, Vector2Int cell)
    {
        if (!CanPlace(type, cell, out _)) return false;
        var def = BuildingDef.Get(type);
        Spend(def.Cost, def.Ore, def.OreCost);

        var b = CreateBuilding(type, new List<Vector2Int> { cell });
        if (def.IsTower)
            b.gameObject.AddComponent<Tower>().Configure(type);
        else if (type == BuildType.Miner)
            b.gameObject.AddComponent<Miner>().ore = ore[cell].Type;
        return true;
    }

    Building CreateBuilding(BuildType type, List<Vector2Int> cells)
    {
        Vector3 center = Vector3.zero;
        foreach (var c in cells) center += Map.CellToWorld(c);
        center /= cells.Count;

        var go = new GameObject(BuildingDef.Get(type).Name);
        go.transform.position = center;
        // The sprite sits on a child "Body" so the animator can squash it (see SpriteAnimator).
        var sr = SpriteAnimator.CreateBody(go);
        sr.sprite = SpriteFactory.ForBuilding(type);
        sr.sortingOrder = 5;
        SpriteFactory.AddGlow(sr); // lit windows, crystals, lanterns, fire
        GlowPulse.On(sr, GlowPulse.Mode.Pulse);

        var b = go.AddComponent<Building>();
        b.Setup(type, cells, cells.Count > 1 ? 1f : 0.5f);
        var style = type == BuildType.Miner ? AnimStyle.Worker
                  : BuildingDef.Get(type).IsTower ? AnimStyle.Tower : AnimStyle.Building;
        b.Anim = SpriteAnimator.Attach(go, sr, style);
        b.Health.Damaged += amount => b.Anim.OnDamaged(amount, b.Health.Max);
        b.Anim.Appear(drop: true, duration: 0.3f); // falls into place with a thud
        Lighting.AddBuildingLight(b);
        Buildings.Add(b);
        foreach (var c in cells) occupied[c] = b;
        return b;
    }

    public Building GetBuildingAt(Vector2Int cell) => occupied.TryGetValue(cell, out var b) ? b : null;

    /// <summary>Sell a building for half the gold spent (ore isn't refunded).</summary>
    public void TrySell(Vector2Int cell)
    {
        var b = GetBuildingAt(cell);
        if (b == null || b.Type == BuildType.Hub) return;
        Gold += b.SellValue;
        ShowPopup(b.transform.position, "+" + b.SellValue, OreColor(OreType.Gold));
        b.Anim.Die(crumble: true, duration: 0.4f);
        Juice.Sparkle(b.transform.position, OreColor(OreType.Gold), 12, 0.5f);
        RemoveBuilding(b);
    }

    public void OnBuildingDestroyed(Building b)
    {
        if (b == Hub)
        {
            IsGameOver = true;
            Time.timeScale = 0f;
            return;
        }
        ShowPopup(b.transform.position, BuildingDef.Get(b.Type).Name + " destroyed!", new Color(1f, 0.4f, 0.4f));
        b.Anim.Die(crumble: true);
        RemoveBuilding(b);
    }

    void RemoveBuilding(Building b)
    {
        foreach (var c in b.Cells) occupied.Remove(c);
        Buildings.Remove(b);
        Destroy(b.gameObject);
    }

    // ---------- Combat helpers ----------

    public Building FindClosestBuilding(Vector3 from)
    {
        Building best = null;
        float bestDist = float.MaxValue;
        foreach (var b in Buildings)
        {
            if (b == null || b.Health.IsDead) continue;
            float d = Vector3.Distance(from, b.transform.position) - b.Radius;
            if (d < bestDist) { bestDist = d; best = b; }
        }
        return best;
    }

    public Enemy FindClosestEnemy(Vector3 from, float maxRange)
    {
        Enemy best = null;
        float bestDist = maxRange;
        foreach (var e in Enemies)
        {
            if (e == null || e.Health.IsDead) continue;
            float d = Vector3.Distance(from, e.transform.position);
            if (d <= bestDist) { bestDist = d; best = e; }
        }
        return best;
    }

    /// <summary>
    /// Normal enemies only give XP (no gold): full for the hero's kills, half for tower kills.
    /// Bosses also drop treasure.
    /// </summary>
    public void OnEnemyKilled(Enemy e, int reward)
    {
        Kills++;
        if (Player == null) return;
        float xp = reward * 2f * (e.LastHitByHero || e.IsBoss ? 1f : 0.5f);
        Player.AddXp(xp);
        ShowPopup(e.transform.position, $"+{Mathf.RoundToInt(xp)} XP", new Color(0.45f, 0.85f, 1f));

        if (!e.IsBoss) return;
        if (e.Mode == EnemyMode.Dungeon) Dungeons.OnBossKilled(e);
        else
        {
            var oreType = OreOf(e.Def.Home ?? Biome.DarkForest);
            AddOre(oreType, 6, e.transform.position + Vector3.up * 0.5f);
            AddOre(OreType.Gold, 60, e.transform.position + Vector3.up);
            Announce($"{e.Def.Name} has fallen! It dropped 60 gold and 6 {PixelArt.OreName(oreType)}.");
        }
    }

    /// <summary>The closest living boss to the hero (for the big health bar), or null.</summary>
    public Enemy NearbyBoss()
    {
        if (Player == null) return null;
        Enemy best = null;
        float bestDist = 16f;
        foreach (var e in Enemies)
        {
            if (e == null || !e.IsBoss || e.Health.IsDead || e.Dungeon != Dungeons.Current) continue;
            float d = Vector2.Distance(e.transform.position, Player.transform.position);
            if (d < bestDist) { bestDist = d; best = e; }
        }
        return best;
    }

    // ---------- Hero mining ----------

    /// <summary>
    /// The closest ore node within reach that doesn't have a Miner on it.
    /// Returns false if there isn't one.
    /// </summary>
    public bool FindMinableOre(Vector3 from, float reach, out Vector2Int cell, out Vector3 orePosition)
    {
        cell = default;
        orePosition = default;
        if (Dungeons.Current != null) return false;
        float best = reach;
        bool found = false;
        foreach (var kv in ore)
        {
            if (occupied.ContainsKey(kv.Key)) continue; // a Miner is working it
            Vector3 p = Map.CellToWorld(kv.Key);
            float d = Vector2.Distance(from, p);
            if (d <= best) { best = d; cell = kv.Key; orePosition = p; found = true; }
        }
        return found;
    }

    /// <summary>
    /// The hero hits an ore node. Gold comes out right away; special ores take a few hits each.
    /// </summary>
    public void MineOre(Vector2Int cell, int goldPerHit)
    {
        if (!ore.TryGetValue(cell, out var spot) || spot.Go == null) return;
        Vector3 at = spot.Go.transform.position + Vector3.up * 0.4f;
        if (spot.Type == OreType.Gold) AddOre(OreType.Gold, goldPerHit, at);
        else
        {
            int i = (int)spot.Type;
            oreCarry[i] += 1f / Mathf.Max(1, hitsPerSpecialOre);
            if (oreCarry[i] >= 0.999f) { oreCarry[i] = 0f; AddOre(spot.Type, 1, at); }
            else ShowPopup(at, "*", OreColor(spot.Type));
        }
        // Chips fly off the rock.
        var oreSr = spot.Go.GetComponent<SpriteRenderer>();
        if (oreSr != null) Juice.BurstFromSprite(spot.Go.transform.position, oreSr.sprite, 4, 2f);
        var shake = spot.Go.GetComponent<RockShake>();
        if (shake == null) shake = spot.Go.AddComponent<RockShake>();
        shake.Shake();
    }

    /// <summary>The ore type at a cell (for the build preview), or null.</summary>
    public OreType? OreAt(Vector2Int cell) => ore.TryGetValue(cell, out var s) ? s.Type : (OreType?)null;

    // ---------- Upgrades ----------

    [Header("Upgrades")]
    [Tooltip("How close (in tiles, from the building's edge) the hero must be to upgrade it with E.")]
    public float upgradeReach = 1.2f;

    /// <summary>The building closest to the hero, if they're close enough to upgrade it.</summary>
    public Building BuildingNearPlayer()
    {
        if (Player == null || !Player.IsAlive || Dungeons.Current != null) return null;
        Building best = null;
        float bestDist = upgradeReach;
        foreach (var b in Buildings)
        {
            if (b == null || b.Health.IsDead) continue;
            float d = Vector3.Distance(Player.transform.position, b.transform.position) - b.Radius;
            if (d <= bestDist) { bestDist = d; best = b; }
        }
        return best;
    }

    /// <summary>E: use a dungeon door if you're at one, otherwise upgrade the building next to you.</summary>
    public void Interact()
    {
        if (Dungeons.TryInteract()) return;
        if (Locks.TryInteract()) return;
        TryUpgradeNearPlayer();
    }

    public void TryUpgradeNearPlayer()
    {
        var b = BuildingNearPlayer();
        if (b == null) return;
        Vector3 popPos = b.transform.position + Vector3.up * (b.Radius + 0.3f);

        if (b.IsMaxLevel) { ShowPopup(popPos, "Max level", Color.white); return; }
        int cost = UpgradeCostFor(b);
        var oreType = OreType.Gold;
        int oreCost = 0;
        if (b.Type == BuildType.Hub)
        {
            var step = HubLevels.To(b.Level + 1);
            oreType = step.Ore; oreCost = step.OreAmount;
        }
        string missing = MissingText(cost, oreType, oreCost);
        if (missing != null) { ShowPopup(popPos, missing, new Color(1f, 0.5f, 0.5f)); return; }

        Spend(cost, oreType, oreCost);
        b.Upgrade(cost);
        ShowPopup(popPos, $"{BuildingDef.Get(b.Type).Name} level {b.Level}!", new Color(0.5f, 1f, 0.5f));

        if (b.Type == BuildType.Hub)
        {
            Announce($"Hub level {b.Level}! Unlocked: {string.Join(", ", HubLevels.To(b.Level).Unlocks)}");
            Fx.Ring(b.transform.position, 6f, new Color(1f, 0.85f, 0.4f), 0.8f);
            if (b.Level >= HubLevels.SunBeaconLevel) Lighting.SunBeacon(b);
        }
    }
}
