using UnityEngine;

public enum TowerKind { Crystal, Cannon, Frost, Storm, Inferno }

/// <summary>
/// Attacks enemies in range. What it does depends on its kind:
///   Crystal - homing crystal shards at the nearest enemy (also the Hub's built-in gun)
///   Cannon  - slow exploding shells that hit groups
///   Frost   - a pulse that chills (slows) and nips everything nearby
///   Storm   - lightning that jumps from enemy to enemy
///   Inferno - sets every enemy close by on fire
/// </summary>
public class Tower : MonoBehaviour
{
    public TowerKind kind = TowerKind.Crystal;
    public float range = 3.5f;
    public float fireInterval = 0.7f;
    public float damage = 8f;
    public float projectileSpeed = 10f;
    public Color projectileColor = Color.white;

    /// <summary>How far above the building's center the shots start (the crystal / orb on top).</summary>
    public float crystalHeight = 0.9f;

    [Header("Special")]
    public float splashRadius = 1.3f;   // Cannon
    public float chillAmount = 0.5f;    // Frost: 0.5 = half speed
    public int chainCount = 4;          // Storm: how many enemies one bolt can hit

    float cooldown;
    SpriteAnimator anim;

    void Start() => anim = GetComponent<SpriteAnimator>();

    bool baseSaved;
    float baseRange, baseDamage, baseFireInterval, baseChill;

    /// <summary>Set up a tower from its building's stats.</summary>
    public void Configure(BuildType type)
    {
        var def = BuildingDef.Get(type);
        range = def.Range; damage = def.Damage; fireInterval = def.Interval;
        kind = type switch
        {
            BuildType.Cannon => TowerKind.Cannon,
            BuildType.Frost => TowerKind.Frost,
            BuildType.Storm => TowerKind.Storm,
            BuildType.Inferno => TowerKind.Inferno,
            _ => TowerKind.Crystal
        };
        crystalHeight = kind switch { TowerKind.Cannon => 0.45f, TowerKind.Inferno => 0.9f, _ => 0.9f };
        projectileSpeed = kind == TowerKind.Cannon ? 7f : 10f;
    }

    /// <summary>Scale stats for a building level (1 = starting stats).</summary>
    public void ApplyLevel(int level)
    {
        if (!baseSaved)
        {
            baseRange = range; baseDamage = damage; baseFireInterval = fireInterval; baseChill = chillAmount;
            baseSaved = true;
        }
        damage = baseDamage * (1f + 0.4f * (level - 1));
        range = baseRange + 0.4f * (level - 1);
        fireInterval = baseFireInterval * Mathf.Pow(0.9f, level - 1);
        chillAmount = Mathf.Min(0.75f, baseChill + 0.05f * (level - 1));
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.IsGameOver) return;

        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;

        Enemy target = gm.FindClosestEnemy(transform.position, range);
        if (target == null) return;
        cooldown = fireInterval;

        float dmg = damage * gm.TowerDamageMultiplier;
        Vector3 muzzle = transform.position + Vector3.up * crystalHeight;

        // Show the shot on the tower itself.
        Vector2 toTarget = target.transform.position - muzzle;
        if (anim != null)
        {
            switch (kind)
            {
                case TowerKind.Cannon: anim.Recoil(toTarget, 1.4f); Juice.Dust(muzzle, 4, 0.3f, new Color(0.6f, 0.6f, 0.6f, 0.7f)); break;
                case TowerKind.Frost: anim.Pulse(0.8f); break;
                case TowerKind.Inferno: anim.Pulse(0.35f); break;
                default: anim.Recoil(toTarget, 0.6f); break;
            }
        }
        if (kind == TowerKind.Inferno) Juice.Sparkle(muzzle, new Color(1f, 0.55f, 0.15f), 3, 0.25f);
        if (kind == TowerKind.Frost) Juice.Sparkle(transform.position, new Color(0.7f, 0.95f, 1f), 6, range * 0.6f);
        if (kind == TowerKind.Storm) Juice.Sparkle(muzzle, new Color(0.8f, 0.6f, 1f), 4, 0.2f);

        switch (kind)
        {
            case TowerKind.Crystal:
                Projectile.Fire(muzzle, target, dmg, projectileSpeed, projectileColor);
                break;

            case TowerKind.Cannon:
                Projectile.Fire(muzzle, target, dmg, projectileSpeed, Color.white, SpriteFactory.Cannonball(), splashRadius);
                break;

            case TowerKind.Frost:
                Fx.Ring(transform.position, range, new Color(0.55f, 0.9f, 1f), 0.5f);
                foreach (var e in gm.Enemies.ToArray())
                {
                    if (e == null || e.Health.IsDead) continue;
                    if (Vector2.Distance(e.transform.position, transform.position) > range + e.HitRadius) continue;
                    e.Slow(chillAmount, fireInterval + 0.6f);
                    e.TakeTowerDamage(dmg);
                }
                break;

            case TowerKind.Storm:
                ChainLightning(gm, muzzle, target, dmg);
                break;

            case TowerKind.Inferno:
                foreach (var e in gm.Enemies)
                {
                    if (e == null || e.Health.IsDead) continue;
                    if (Vector2.Distance(e.transform.position, transform.position) > range + e.HitRadius) continue;
                    e.Burn(dmg * e.Def.TowerDamageTaken, fireInterval + 1f, fromHero: false);
                }
                break;
        }
    }

    /// <summary>Hits the target, then jumps to the nearest enemy not hit yet (weaker each jump).</summary>
    void ChainLightning(GameManager gm, Vector3 from, Enemy first, float dmg)
    {
        var hit = new System.Collections.Generic.List<Enemy>();
        Enemy current = first;
        Vector3 prev = from;
        Color bolt = new Color(0.8f, 0.6f, 1f);
        for (int i = 0; i < chainCount && current != null; i++)
        {
            Fx.Lightning(prev, current.transform.position, bolt);
            hit.Add(current);
            prev = current.transform.position;
            current.TakeTowerDamage(dmg);
            dmg *= 0.8f;

            Enemy next = null;
            float best = 2.8f;
            foreach (var e in gm.Enemies)
            {
                if (e == null || e.Health.IsDead || hit.Contains(e)) continue;
                float d = Vector2.Distance(e.transform.position, prev);
                if (d < best) { best = d; next = e; }
            }
            current = next;
        }
    }
}
