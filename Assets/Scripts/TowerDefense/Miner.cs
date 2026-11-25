using UnityEngine;

/// <summary>
/// Sits on an ore node and collects it on a timer.
/// Gold ore: gold every trip. Special ores (Iron, Soul Crystal, Bloodstone, Emberite): a little of that ore, more slowly.
/// </summary>
public class Miner : MonoBehaviour
{
    public int goldPerTick = 3;
    public float tickInterval = 5f;
    [Tooltip("Seconds per trip on special (non-gold) ore.")]
    public float specialInterval = 9f;

    public OreType ore = OreType.Gold;
    int level = 1;
    float timer;

    public void ApplyLevel(int newLevel) => level = newLevel;

    /// <summary>How much one trip brings back.</summary>
    public int AmountPerTrip(GameManager gm) => ore == OreType.Gold
        ? goldPerTick + 2 * (level - 1) + gm.MinerBonusGold // Prospector skill adds to every gold trip
        : 1 + (level - 1) / 2;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.IsGameOver) return;

        float interval = ore == OreType.Gold ? tickInterval : specialInterval;
        if (gm.Hub != null && gm.Hub.Level >= HubLevels.FastMinersLevel) interval /= 1.5f;

        timer += Time.deltaTime;
        if (timer < interval) return;
        timer -= interval;

        gm.AddOre(ore, AmountPerTrip(gm), transform.position + Vector3.up * 0.4f);

        // A little hop and a sparkle in the ore's color.
        var anim = GetComponent<SpriteAnimator>();
        if (anim != null) { anim.Hop(0.15f); anim.Pulse(0.8f); }
        Juice.Sparkle(transform.position + Vector3.up * 0.2f, GameManager.OreColor(ore), 6, 0.3f);
    }
}
