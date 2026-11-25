using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum HeroClass { None, Knight, Ranger, Pyromancer }

/// <summary>One node in the skill tree.</summary>
public class SkillDef
{
    public string Id;
    public string Name;
    public string Description;
    /// <summary>Which column it lives in. None = the shared Survival column anyone can use.</summary>
    public HeroClass Class;
    public int Row;
    public int MaxRank = 1;
    public int RequiredLevel = 1;
    /// <summary>For active abilities: the key that uses it ("Q" or "F"). Null for passives.</summary>
    public string Key;
    public bool IsActive => Key != null;
}

/// <summary>
/// Every skill in the game. To add or rebalance a skill, edit this list and the
/// matching effect in PlayerController / HeroAbilities.
///
/// Rules:
///   - You get 1 skill point at the start and 1 more every hero level.
///   - The first point you put into a class column picks your class; the other two classes lock.
///   - Class skills must be learned top to bottom. Survival skills only need the hero level.
/// </summary>
public static class SkillTree
{
    public static readonly SkillDef[] All =
    {
        // ---------- Knight: tough melee fighter ----------
        new() { Id = "knight", Class = HeroClass.Knight, Row = 0, Name = "Knight's Oath",
                Description = "Become a Knight. +30 max HP, +25% sword damage, sword swings in a wider arc." },
        new() { Id = "whirlwind", Class = HeroClass.Knight, Row = 1, RequiredLevel = 3, Key = "Q", Name = "Whirlwind",
                Description = "Spin to hit every enemy around you for 2x sword damage. 8s cooldown." },
        new() { Id = "ironskin", Class = HeroClass.Knight, Row = 2, RequiredLevel = 4, MaxRank = 3, Name = "Iron Skin",
                Description = "Take 10% less damage per rank." },
        new() { Id = "warcry", Class = HeroClass.Knight, Row = 3, RequiredLevel = 6, Key = "F", Name = "War Cry",
                Description = "Stun enemies within 5 tiles for 2.5s and heal 25% of your HP. 20s cooldown." },
        new() { Id = "bloodlust", Class = HeroClass.Knight, Row = 4, RequiredLevel = 8, MaxRank = 2, Name = "Bloodlust",
                Description = "Melee hits heal you for 10% of the damage dealt per rank." },

        // ---------- Ranger: fast, fights from range ----------
        new() { Id = "ranger", Class = HeroClass.Ranger, Row = 0, Name = "Ranger's Path",
                Description = "Become a Ranger. Space shoots arrows at the mouse instead of swinging. +10% move speed." },
        new() { Id = "volley", Class = HeroClass.Ranger, Row = 1, RequiredLevel = 3, Key = "Q", Name = "Arrow Volley",
                Description = "Fire 16 arrows in every direction at once. 10s cooldown." },
        new() { Id = "multishot", Class = HeroClass.Ranger, Row = 2, RequiredLevel = 4, MaxRank = 2, Name = "Multishot",
                Description = "Each shot fires 1 extra arrow per rank in a spread." },
        new() { Id = "dash", Class = HeroClass.Ranger, Row = 3, RequiredLevel = 6, Key = "F", Name = "Shadow Dash",
                Description = "Dash 4 tiles the way you're moving. Can't be hurt while dashing. 6s cooldown." },
        new() { Id = "swift", Class = HeroClass.Ranger, Row = 4, RequiredLevel = 8, MaxRank = 3, Name = "Eagle Eye",
                Description = "+15% arrow damage and +10% move speed per rank." },

        // ---------- Pyromancer: fire magic from the torch ----------
        new() { Id = "pyro", Class = HeroClass.Pyromancer, Row = 0, Name = "Pyromancer's Pact",
                Description = "Become a Pyromancer. Torch light +30%. Sword hits set enemies on fire." },
        new() { Id = "fireball", Class = HeroClass.Pyromancer, Row = 1, RequiredLevel = 3, Key = "Q", Name = "Fireball",
                Description = "Throw a fireball at the mouse that explodes and burns everything nearby. 5s cooldown." },
        new() { Id = "brightflame", Class = HeroClass.Pyromancer, Row = 2, RequiredLevel = 4, MaxRank = 2, Name = "Bright Flame",
                Description = "Torch light +20% per rank. Enemies close to your torch burn." },
        new() { Id = "nova", Class = HeroClass.Pyromancer, Row = 3, RequiredLevel = 6, Key = "F", Name = "Flame Nova",
                Description = "Blast a ring of fire around you, damaging and burning enemies within 4 tiles. 15s cooldown." },
        new() { Id = "phoenix", Class = HeroClass.Pyromancer, Row = 4, RequiredLevel = 8, Name = "Phoenix",
                Description = "When you would die, rise again with 50% HP instead. Once every 90s." },

        // ---------- Survival: shared by every class ----------
        new() { Id = "vitality", Class = HeroClass.None, Row = 0, MaxRank = 3, Name = "Vitality",
                Description = "+20 max HP per rank." },
        new() { Id = "secondwind", Class = HeroClass.None, Row = 1, RequiredLevel = 2, MaxRank = 2, Name = "Second Wind",
                Description = "+3 HP/sec regen per rank, and regen starts 1s sooner per rank." },
        new() { Id = "prospector", Class = HeroClass.None, Row = 2, RequiredLevel = 3, MaxRank = 3, Name = "Prospector",
                Description = "Every Miner brings back +1 gold per trip per rank." },
        new() { Id = "engineer", Class = HeroClass.None, Row = 3, RequiredLevel = 5, MaxRank = 2, Name = "Engineer",
                Description = "Building upgrades cost 20% less per rank." },
        new() { Id = "warden", Class = HeroClass.None, Row = 4, RequiredLevel = 7, Name = "Warden",
                Description = "All towers deal +25% damage." },
    };

    public static SkillDef Get(string id) => All.First(s => s.Id == id);

    public static IEnumerable<SkillDef> Column(HeroClass c) => All.Where(s => s.Class == c).OrderBy(s => s.Row);

    public static string ClassName(HeroClass c) => c switch
    {
        HeroClass.Knight => "Knight",
        HeroClass.Ranger => "Ranger",
        HeroClass.Pyromancer => "Pyromancer",
        _ => "Survival"
    };

    public static Color ClassColor(HeroClass c) => c switch
    {
        HeroClass.Knight => new Color(0.55f, 0.75f, 1f),
        HeroClass.Ranger => new Color(0.5f, 0.95f, 0.5f),
        HeroClass.Pyromancer => new Color(1f, 0.6f, 0.3f),
        _ => new Color(0.9f, 0.9f, 0.8f)
    };
}
