using UnityEngine;

public enum EnemyKind
{
    // Meadow (and anywhere)
    Grunt, Runner, Brute,
    // Dark Forest
    Wolf, Spider,
    // Bloodmoor
    Bat, Vampire,
    // Crypt
    Skeleton, Ghost,
    // Ashlands
    Imp, MagmaSlime,
    // Bosses
    SpiderQueen, VampireLord, Lich, RockGiant,
}

/// <summary>Stats for one kind of enemy. Edit the table in EnemyDefs to rebalance.</summary>
public class EnemyDef
{
    public string Name;
    public float Hp, Speed, Dps;
    /// <summary>How big it's drawn (1 = sprite's natural size).</summary>
    public float Scale = 0.8f;
    /// <summary>How close a sword or bullet must get to hit it.</summary>
    public float HitRadius = 0.3f;
    public int Xp;

    /// <summary>Where it lives. null = shows up everywhere.</summary>
    public Biome? Home;
    /// <summary>Seconds into the game before it starts showing up in raids.</summary>
    public float MinTime;
    /// <summary>How often it's picked compared to the others.</summary>
    public float Weight = 1f;

    // Specials
    public float RangedRange;     // > 0: stops at this distance and shoots
    public float ShotInterval = 2f;
    public float ShotDamage;
    public int ShotKind;          // see EnemyBolt / PixelArt.EnemyShot (5 = fireball)
    public float Lifesteal;       // heals this fraction of damage dealt
    public bool SlowsOnHit;       // spider bites slow the hero
    public float TowerDamageTaken = 1f; // ghosts: towers struggle to hit them
    public bool Wobbles;          // bats and ghosts weave as they fly
    public bool IsBoss;
}

public static class EnemyDefs
{
    public static EnemyDef Get(EnemyKind k) => k switch
    {
        EnemyKind.Grunt => new EnemyDef { Name = "Goblin", Hp = 22, Speed = 1.3f, Dps = 10, Scale = 0.8f, HitRadius = 0.3f, Xp = 3, Weight = 10 },
        EnemyKind.Runner => new EnemyDef { Name = "Goblin Scout", Hp = 10, Speed = 2.4f, Dps = 5, Scale = 0.65f, HitRadius = 0.24f, Xp = 2, MinTime = 180, Weight = 4 },
        EnemyKind.Brute => new EnemyDef { Name = "Golem", Hp = 90, Speed = 0.7f, Dps = 25, Scale = 1f, HitRadius = 0.44f, Xp = 8, MinTime = 420, Weight = 1.5f },

        EnemyKind.Wolf => new EnemyDef { Name = "Wolf", Hp = 18, Speed = 2.6f, Dps = 8, Scale = 0.85f, HitRadius = 0.3f, Xp = 3, Home = Biome.DarkForest, MinTime = 60, Weight = 6, Wobbles = false },
        EnemyKind.Spider => new EnemyDef { Name = "Giant Spider", Hp = 30, Speed = 1.5f, Dps = 8, Scale = 0.8f, HitRadius = 0.32f, Xp = 4, Home = Biome.DarkForest, MinTime = 120, Weight = 4, SlowsOnHit = true },

        EnemyKind.Bat => new EnemyDef { Name = "Bat", Hp = 8, Speed = 3f, Dps = 4, Scale = 0.7f, HitRadius = 0.22f, Xp = 2, Home = Biome.Bloodmoor, MinTime = 60, Weight = 6, Wobbles = true },
        EnemyKind.Vampire => new EnemyDef { Name = "Vampire", Hp = 45, Speed = 1.4f, Dps = 12, Scale = 0.85f, HitRadius = 0.32f, Xp = 6, Home = Biome.Bloodmoor, MinTime = 240, Weight = 3, Lifesteal = 0.6f },

        EnemyKind.Skeleton => new EnemyDef { Name = "Skeleton", Hp = 24, Speed = 1.1f, Dps = 6, Scale = 0.8f, HitRadius = 0.3f, Xp = 4, Home = Biome.Crypt, MinTime = 60, Weight = 6,
                                             RangedRange = 4f, ShotInterval = 1.8f, ShotDamage = 7, ShotKind = 0 },
        EnemyKind.Ghost => new EnemyDef { Name = "Ghost", Hp = 30, Speed = 1f, Dps = 10, Scale = 0.85f, HitRadius = 0.32f, Xp = 5, Home = Biome.Crypt, MinTime = 180, Weight = 3,
                                          TowerDamageTaken = 0.4f, Wobbles = true },

        EnemyKind.Imp => new EnemyDef { Name = "Fire Imp", Hp = 20, Speed = 1.9f, Dps = 6, Scale = 0.7f, HitRadius = 0.26f, Xp = 4, Home = Biome.Ashlands, MinTime = 60, Weight = 6,
                                        RangedRange = 4.5f, ShotInterval = 2.2f, ShotDamage = 9, ShotKind = 5 },
        EnemyKind.MagmaSlime => new EnemyDef { Name = "Magma Slime", Hp = 110, Speed = 0.6f, Dps = 18, Scale = 0.9f, HitRadius = 0.4f, Xp = 9, Home = Biome.Ashlands, MinTime = 300, Weight = 2 },

        EnemyKind.SpiderQueen => new EnemyDef { Name = "The Spider Queen", Hp = 900, Speed = 1.2f, Dps = 20, Scale = 1.5f, HitRadius = 0.8f, Xp = 150, Home = Biome.DarkForest, IsBoss = true, SlowsOnHit = true },
        EnemyKind.VampireLord => new EnemyDef { Name = "The Vampire Lord", Hp = 1000, Speed = 1.5f, Dps = 24, Scale = 1.5f, HitRadius = 0.7f, Xp = 180, Home = Biome.Bloodmoor, IsBoss = true, Lifesteal = 0.5f },
        EnemyKind.Lich => new EnemyDef { Name = "The Lich King", Hp = 1100, Speed = 0.9f, Dps = 15, Scale = 1.6f, HitRadius = 0.7f, Xp = 200, Home = Biome.Crypt, IsBoss = true },
        EnemyKind.RockGiant => new EnemyDef { Name = "The Rock Giant", Hp = 1600, Speed = 0.7f, Dps = 35, Scale = 1.6f, HitRadius = 0.9f, Xp = 250, Home = Biome.Ashlands, IsBoss = true },
        _ => new EnemyDef { Name = "?", Hp = 10, Speed = 1, Dps = 5 }
    };

    public static readonly EnemyKind[] All = (EnemyKind[])System.Enum.GetValues(typeof(EnemyKind));

    /// <summary>The boss of a biome's dungeon (and of boss waves from that side).</summary>
    public static EnemyKind BossOf(Biome b) => b switch
    {
        Biome.Bloodmoor => EnemyKind.VampireLord,
        Biome.Crypt => EnemyKind.Lich,
        Biome.Ashlands => EnemyKind.RockGiant,
        _ => EnemyKind.SpiderQueen,
    };

    /// <summary>A random (weighted) non-boss enemy that fits a biome. ignoreTime = any of them (for guards and dungeons).</summary>
    public static EnemyKind Pick(Biome biome, float time, bool ignoreTime = false, bool biomeOnly = false)
    {
        float total = 0f;
        foreach (var k in All) total += WeightFor(k, biome, time, ignoreTime, biomeOnly);
        if (total <= 0f) return EnemyKind.Grunt;
        float r = Random.value * total;
        foreach (var k in All)
        {
            r -= WeightFor(k, biome, time, ignoreTime, biomeOnly);
            if (r <= 0f) return k;
        }
        return EnemyKind.Grunt;
    }

    static float WeightFor(EnemyKind k, Biome biome, float time, bool ignoreTime, bool biomeOnly)
    {
        var d = Get(k);
        if (d.IsBoss) return 0f;
        if (!ignoreTime && time < d.MinTime) return 0f;
        if (d.Home == null) return biomeOnly && biome != Biome.Meadow ? 0f : d.Weight;
        return d.Home == biome ? d.Weight : 0f;
    }
}
