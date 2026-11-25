using System.Collections;
using UnityEngine;

/// <summary>
/// A never-ending stream of raiders from the wild lands you've opened (see AreaLocks) that slowly gets faster and tougher,
/// plus a bigger "wave" rush from one direction every so often.
/// Raiders are the monsters of the land they come from (wolves from the Dark Forest, skeletons from
/// the Crypt...), and every few waves that land's boss comes along too.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Constant trickle")]
    [Tooltip("Seconds between spawns at the very start.")]
    public float startInterval = 8f;
    [Tooltip("Fastest the trickle can get.")]
    public float minInterval = 2f;
    [Tooltip("Seconds of survival until the trickle reaches max speed.")]
    public float rampSeconds = 900f;
    [Tooltip("Enemy HP grows by this fraction every minute (0.5 = +50%/min).")]
    public float hpGrowthPerMinute = 0.25f;
    [Tooltip("Seconds before the first enemy appears.")]
    public float gracePeriod = 30f;
    [Tooltip("How far past the edge of the Meadow raiders appear (in tiles).")]
    public float spawnBeyondMeadow = 5f;

    [Header("Big waves")]
    public float waveInterval = 120f;
    public int waveBaseSize = 4;
    public int waveSizeGrowth = 2;
    [Tooltip("Every Nth wave brings a boss.")]
    public int bossEveryNWaves = 5;

    public int WaveNumber { get; private set; }
    public float TimeToNextWave => waveTimer;
    public bool NextWaveIsBoss => bossEveryNWaves > 0 && (WaveNumber + 1) % bossEveryNWaves == 0;

    /// <summary>How much tougher enemies are right now (grows over time).</summary>
    public float HpMultiplier => 1f + hpGrowthPerMinute * ((GameManager.Instance != null ? GameManager.Instance.ElapsedTime : 0f) / 60f);

    float trickleTimer;
    float waveTimer;

    void OnEnable() => ResetSpawner();

    public void ResetSpawner()
    {
        StopAllCoroutines();
        WaveNumber = 0;
        trickleTimer = gracePeriod;
        waveTimer = waveInterval + gracePeriod;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.IsGameOver || gm.Map == null) return;

        float t = gm.ElapsedTime;

        trickleTimer -= Time.deltaTime;
        if (trickleTimer <= 0f)
        {
            SpawnOne(RandomSpawnPoint(OpenLandAngle()));
            trickleTimer = Mathf.Lerp(startInterval, minInterval, t / rampSeconds);
        }

        waveTimer -= Time.deltaTime;
        if (waveTimer <= 0f)
        {
            bool boss = NextWaveIsBoss;
            WaveNumber++;
            StartCoroutine(SpawnWave(waveBaseSize + waveSizeGrowth * (WaveNumber - 1), boss));
            waveTimer = waveInterval;
        }
    }

    IEnumerator SpawnWave(int count, bool withBoss)
    {
        var gm = GameManager.Instance;
        float angle = OpenLandAngle();
        Vector3 origin = RandomSpawnPoint(angle);
        Biome biome = gm.Map.BiomeAt(origin);
        if (biome == Biome.Meadow) biome = Biome.DarkForest;

        if (withBoss)
        {
            var bossKind = EnemyDefs.BossOf(biome);
            gm.Announce($"BOSS WAVE {WaveNumber}! {EnemyDefs.Get(bossKind).Name} marches out of {PixelArt.BiomeName(biome)}!");
            Enemy.Spawn(bossKind, origin, HpMultiplier * 0.7f);
        }
        else gm.Announce($"Wave {WaveNumber} incoming from {PixelArt.BiomeName(biome)} ({DirectionName(angle)})!");

        for (int i = 0; i < count; i++)
        {
            if (gm.IsGameOver) yield break;
            SpawnOne(RandomSpawnPoint(angle + Random.Range(-12f, 12f)), biome);
            yield return new WaitForSeconds(0.25f);
        }
    }

    void SpawnOne(Vector3 position, Biome? from = null)
    {
        var gm = GameManager.Instance;
        float t = gm.ElapsedTime;
        Enemy.Spawn(EnemyDefs.Pick(from ?? gm.Map.BiomeAt(position), t), position, HpMultiplier);
    }

    /// <summary>
    /// A point just past the edge of the Meadow, in a direction from the Hub (kept inside the map):
    /// walk outward until we leave the Meadow, then a few more tiles.
    /// </summary>
    Vector3 RandomSpawnPoint(float angleDeg)
    {
        var gm = GameManager.Instance;
        Rect b = gm.Map.WorldBounds;
        Vector3 hub = gm.Hub.transform.position;
        float a = angleDeg * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a));
        float dist = 4f;
        while (dist < 200f && b.Contains(hub + dir * dist) && gm.Map.BiomeAt(hub + dir * dist) == Biome.Meadow) dist += 1f;
        Vector3 p = hub + dir * (dist + spawnBeyondMeadow + Random.Range(-1.5f, 1.5f));
        p.x = Mathf.Clamp(p.x, b.xMin + 3f, b.xMax - 3f);
        p.y = Mathf.Clamp(p.y, b.yMin + 3f, b.yMax - 3f);
        return p;
    }

    /// <summary>A random direction whose raiders come out of a land you've opened (sealed lands stay quiet).</summary>
    float OpenLandAngle()
    {
        var gm = GameManager.Instance;
        for (int i = 0; i < 40; i++)
        {
            float angle = Random.Range(0f, 360f);
            if (gm.Locks == null || gm.Locks.IsUnlocked(gm.Map.BiomeAt(RandomSpawnPoint(angle)))) return angle;
        }
        // Fallback: straight towards the Dark Forest's dungeon.
        Vector3 to = gm.Map.CellToWorld(gm.Map.DungeonSite(Biome.DarkForest)) - gm.Hub.transform.position;
        return Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
    }

    static string DirectionName(float angleDeg)
    {
        float a = Mathf.Repeat(angleDeg, 360f);
        return a < 45f || a >= 315f ? "east" : a < 135f ? "north" : a < 225f ? "west" : "south";
    }
}
