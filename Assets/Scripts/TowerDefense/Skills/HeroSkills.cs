using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The hero's learned skills and unspent points. Lives on the Player.
/// </summary>
public class HeroSkills : MonoBehaviour
{
    public int Points { get; private set; } = 1; // one to pick a class right away
    public HeroClass Class { get; private set; } = HeroClass.None;

    /// <summary>Fires whenever a skill is learned, so stats can be recalculated.</summary>
    public event Action Changed;

    readonly Dictionary<string, int> ranks = new();

    public int Rank(string id) => ranks.TryGetValue(id, out int r) ? r : 0;
    public bool Has(string id) => Rank(id) > 0;

    public void AddPoint() => Points++;

    public bool CanLearn(SkillDef s, int heroLevel, out string reason)
    {
        reason = null;
        if (Rank(s.Id) >= s.MaxRank) { reason = "Maxed"; return false; }
        if (s.Class != HeroClass.None && Class != HeroClass.None && Class != s.Class)
        {
            reason = $"You are a {SkillTree.ClassName(Class)}";
            return false;
        }
        if (s.Class != HeroClass.None && s.Row > 0)
        {
            var previous = SkillTree.Column(s.Class).First(x => x.Row == s.Row - 1);
            if (!Has(previous.Id)) { reason = $"Needs {previous.Name}"; return false; }
        }
        if (heroLevel < s.RequiredLevel) { reason = $"Hero level {s.RequiredLevel}"; return false; }
        if (Points <= 0) { reason = "No skill points"; return false; }
        return true;
    }

    public bool Learn(SkillDef s, int heroLevel)
    {
        if (!CanLearn(s, heroLevel, out _)) return false;
        Points--;
        ranks[s.Id] = Rank(s.Id) + 1;
        if (s.Class != HeroClass.None && Class == HeroClass.None) Class = s.Class;
        Changed?.Invoke();
        return true;
    }
}
