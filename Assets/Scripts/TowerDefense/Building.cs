using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum BuildType { None, Hub, Tower, Miner, Cannon, Frost, Storm, Inferno }

/// <summary>Cost and stats for each thing the player can build. Tweak balance here.</summary>
public struct BuildingDef
{
    public string Name;
    public int Cost;
    /// <summary>Extra ore needed to build it (0 = gold only).</summary>
    public int OreCost;
    public OreType Ore;
    /// <summary>The Hub level needed before you can build it.</summary>
    public int HubLevel;
    public float MaxHp;
    public string Description;
    /// <summary>What each upgrade gives, shown in the upgrade prompt.</summary>
    public string UpgradeText;

    // Tower stats (ignored for non-towers)
    public float Range, Damage, Interval;

    public bool IsTower => Range > 0f;

    /// <summary>Everything you can build, in hotbar order (keys 1-6).</summary>
    public static readonly BuildType[] Hotbar =
        { BuildType.Tower, BuildType.Miner, BuildType.Cannon, BuildType.Frost, BuildType.Storm, BuildType.Inferno };

    public static BuildingDef Get(BuildType type) => type switch
    {
        BuildType.Hub => new BuildingDef { Name = "Hub", MaxHp = 500, Description = "Protect this! Upgrade it to unlock new things.",
                                           UpgradeText = "+HP, stronger gun, more gold/sec" },
        BuildType.Tower => new BuildingDef { Name = "Crystal Tower", Cost = 50, MaxHp = 120, HubLevel = 1,
                                             Range = 3.5f, Damage = 8f, Interval = 0.7f,
                                             Description = "Shoots the nearest enemy", UpgradeText = "+40% damage, +range, fires faster" },
        BuildType.Miner => new BuildingDef { Name = "Miner", Cost = 30, MaxHp = 80, HubLevel = 1,
                                             Description = "Place on ore: collects it over time", UpgradeText = "Collects more per trip" },
        BuildType.Cannon => new BuildingDef { Name = "Cannon", Cost = 90, OreCost = 4, Ore = OreType.Iron, MaxHp = 180, HubLevel = 2,
                                              Range = 4.2f, Damage = 22f, Interval = 2.2f,
                                              Description = "Slow, but shells explode and hit groups", UpgradeText = "+40% damage, +range, fires faster" },
        BuildType.Frost => new BuildingDef { Name = "Frost Spire", Cost = 110, OreCost = 4, Ore = OreType.SoulCrystal, MaxHp = 140, HubLevel = 3,
                                             Range = 2.8f, Damage = 3f, Interval = 1.2f,
                                             Description = "Chills everything nearby: enemies move at half speed", UpgradeText = "+range, stronger chill" },
        BuildType.Storm => new BuildingDef { Name = "Storm Spire", Cost = 150, OreCost = 4, Ore = OreType.Bloodstone, MaxHp = 140, HubLevel = 4,
                                             Range = 4f, Damage = 16f, Interval = 1.5f,
                                             Description = "Lightning that jumps between up to 4 enemies", UpgradeText = "+40% damage, +range, fires faster" },
        BuildType.Inferno => new BuildingDef { Name = "Inferno", Cost = 170, OreCost = 4, Ore = OreType.Emberite, MaxHp = 160, HubLevel = 5,
                                               Range = 2.3f, Damage = 10f, Interval = 0.5f,
                                               Description = "Sets every enemy close by on fire", UpgradeText = "+40% burn, +range" },
        _ => default
    };

    /// <summary>"50g" or "90g + 4 Iron".</summary>
    public string CostText => OreCost > 0 ? $"{Cost}g + {OreCost} {PixelArt.OreName(Ore)}" : $"{Cost}g";
}

/// <summary>
/// What each Hub level costs and unlocks. Upgrade the Hub by standing next to it and pressing E.
/// Higher levels need ore from the dangerous lands around the Meadow.
/// </summary>
public static class HubLevels
{
    public struct Step
    {
        public int Gold;
        public OreType Ore;
        public int OreAmount;
        public string[] Unlocks;
    }

    /// <summary>The cost and rewards for reaching a level (2..5).</summary>
    public static Step To(int level) => level switch
    {
        2 => new Step { Gold = 150, Ore = OreType.Iron, OreAmount = 6, Unlocks = new[] { "Cannon", "Recall: press H to teleport home" } },
        3 => new Step { Gold = 300, Ore = OreType.SoulCrystal, OreAmount = 6, Unlocks = new[] { "Frost Spire", "Hub repairs nearby buildings" } },
        4 => new Step { Gold = 500, Ore = OreType.Bloodstone, OreAmount = 6, Unlocks = new[] { "Storm Spire", "Miners work 50% faster" } },
        5 => new Step { Gold = 800, Ore = OreType.Emberite, OreAmount = 6, Unlocks = new[] { "Inferno", "Sun Beacon: all towers +25% damage" } },
        _ => default
    };

    public const int RecallLevel = 2, RepairLevel = 3, FastMinersLevel = 4, SunBeaconLevel = 5;
}

/// <summary>
/// Anything the player owns on the map. Enemies attack the nearest Building.
/// Extra behavior (shooting, mining) lives in separate components like Tower and Miner.
/// Buildings level up from 1 to MaxLevel; each upgrade also fully repairs them.
/// </summary>
[RequireComponent(typeof(Health))]
public class Building : MonoBehaviour
{
    public const int MaxLevel = 5;

    public BuildType Type { get; private set; }
    public List<Vector2Int> Cells { get; private set; }
    public Health Health { get; private set; }
    public int Level { get; private set; } = 1;

    /// <summary>Squashes the sprite when it's hit, fires, upgrades (see SpriteAnimator). Set by the GameManager.</summary>
    public SpriteAnimator Anim { get; set; }

    /// <summary>Total gold spent on this building (build + upgrades).</summary>
    public int Invested { get; private set; }

    /// <summary>Roughly how far from the center the building's edge is (for enemy reach).</summary>
    public float Radius { get; private set; }

    public int SellValue => Invested / 2;
    public bool IsMaxLevel => Level >= MaxLevel;

    /// <summary>Gold for the next upgrade: the build cost times the current level (the Hub uses HubLevels instead).</summary>
    public int UpgradeCost => Type == BuildType.Hub ? HubLevels.To(Level + 1).Gold : BuildingDef.Get(Type).Cost * Level;

    /// <summary>True for the tall towers (their health bar and prompts sit higher up).</summary>
    public bool IsTall => Type == BuildType.Tower || Type == BuildType.Frost || Type == BuildType.Storm || Type == BuildType.Inferno;

    readonly List<GameObject> pips = new();

    public void Setup(BuildType type, List<Vector2Int> cells, float radius)
    {
        Type = type;
        Cells = cells;
        Radius = radius;
        Invested = BuildingDef.Get(type).Cost;
        Health = GetComponent<Health>();
        float barHeight = IsTall ? 1.3f : type == BuildType.Cannon ? 0.9f : radius + 0.15f;
        Health.Init(BuildingDef.Get(type).MaxHp, radius * 1.6f, barHeight);
        Health.Died += () => GameManager.Instance.OnBuildingDestroyed(this);
    }

    /// <summary>Raise the level by one. The GameManager checks and charges the cost first (goldPaid).</summary>
    public void Upgrade(int goldPaid)
    {
        if (IsMaxLevel) return;
        Invested += goldPaid;
        Level++;

        // More health, and a full repair.
        Health.SetMax(BuildingDef.Get(Type).MaxHp * Mathf.Pow(1.3f, Level - 1), fillUp: true);

        var tower = GetComponent<Tower>();
        if (tower != null)
        {
            tower.ApplyLevel(Level);
            // Tower lights match their range so you can see what they cover.
            if (Type != BuildType.Hub)
            {
                var light = GetComponentInChildren<Light2D>();
                if (light != null) light.pointLightOuterRadius = tower.range;
            }
        }

        var miner = GetComponent<Miner>();
        if (miner != null) miner.ApplyLevel(Level);

        UpdatePips();

        // Celebrate: a hop, a ring and golden sparkles.
        if (Anim != null) { Anim.Hop(0.3f); Anim.Pulse(1.2f); }
        Fx.Ring(transform.position, Radius + 1f, new Color(1f, 0.85f, 0.4f), 0.45f);
        Juice.Sparkle(transform.position + Vector3.up * 0.3f, new Color(1f, 0.85f, 0.4f), 18, Radius + 0.2f);
    }

    /// <summary>Little gold diamonds under the building showing its level (none at level 1).</summary>
    void UpdatePips()
    {
        foreach (var p in pips) Destroy(p);
        pips.Clear();
        if (Level <= 1) return;

        const float spacing = 0.2f;
        float startX = -(Level - 1) * spacing / 2f;
        for (int i = 0; i < Level; i++)
        {
            var sr = new GameObject("Level Pip").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            sr.transform.localPosition = new Vector3(startX + i * spacing, -Radius + 0.1f, 0f);
            sr.sprite = SpriteFactory.LevelPip();
            sr.sortingOrder = 8;
            SpriteFactory.MakeUnlit(sr);
            pips.Add(sr.gameObject);
        }
    }
}
