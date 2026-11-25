using UnityEngine;

/// <summary>
/// How an enemy behaves:
///   Raider  - comes in from the edge of the map and attacks your buildings (and you, if you get close).
///   Guard   - stands near an ore node in the wild lands and attacks anyone who comes close.
///   Dungeon - waits in a dungeon room until the hero shows up.
/// </summary>
public enum EnemyMode { Raider, Guard, Dungeon }

/// <summary>
/// Any monster, from goblins to bosses. Stats come from EnemyDefs; bosses also get a Boss component for their attacks.
/// Overworld enemies ignore trees (they come out of the wilds); dungeon enemies bump into walls.
/// </summary>
[RequireComponent(typeof(Health))]
public class Enemy : MonoBehaviour
{
    public EnemyKind Kind { get; private set; }
    public EnemyDef Def { get; private set; }
    public Health Health { get; private set; }
    public EnemyMode Mode { get; private set; }
    /// <summary>The dungeon this enemy lives in (Dungeon mode only).</summary>
    public Dungeon Dungeon { get; private set; }
    public bool IsBoss => Def.IsBoss;

    /// <summary>Roughly how big the enemy is, for sword hits.</summary>
    public float HitRadius { get; private set; }

    /// <summary>True if the hero landed the last hit (hero kills give full XP, tower kills give half).</summary>
    public bool LastHitByHero { get; set; }

    /// <summary>Where it's heading / attacking right now (bosses aim their attacks here).</summary>
    public Vector3 GoalPosition { get; private set; }
    public bool HasGoal { get; private set; }

    float speed;
    const float AttackReach = 0.3f;
    // Raiders chase the hero if they come within this distance.
    const float PlayerAggroRange = 3f;

    Vector3 home;
    Building target;
    float retargetTimer;
    float shotTimer;
    float wobbleSeed;
    Rigidbody2D rb;

    // Status effects from hero skills and towers
    SpriteRenderer body, glow;
    Color baseColor = Color.white;
    float stunTimer, burnTimer, burnDps, slowTimer, slowAmount;
    bool burnFromHero;

    /// <summary>Moves the sprite: walking, hits, attacks, dying (see SpriteAnimator).</summary>
    public SpriteAnimator Anim { get; private set; }
    float attackAnimTimer;

    /// <summary>Which kind of motion suits each monster.</summary>
    static AnimStyle StyleFor(EnemyKind kind) => kind switch
    {
        EnemyKind.Brute or EnemyKind.RockGiant => AnimStyle.Heavy,
        EnemyKind.Wolf => AnimStyle.Runner,
        EnemyKind.Spider or EnemyKind.SpiderQueen => AnimStyle.Crawler,
        EnemyKind.Bat => AnimStyle.Flyer,
        EnemyKind.Ghost or EnemyKind.Lich or EnemyKind.VampireLord => AnimStyle.Floater,
        EnemyKind.MagmaSlime => AnimStyle.Hopper,
        _ => AnimStyle.Walker,
    };

    public bool IsStunned => stunTimer > 0f;

    /// <summary>Can't move or attack for a while (bosses shrug most of it off).</summary>
    public void Stun(float seconds) => stunTimer = Mathf.Max(stunTimer, IsBoss ? seconds * 0.3f : seconds);

    /// <summary>Takes damage over time. Keeps the stronger burn.</summary>
    public void Burn(float damagePerSecond, float seconds, bool fromHero = true)
    {
        burnDps = Mathf.Max(burnDps, damagePerSecond);
        burnTimer = Mathf.Max(burnTimer, seconds);
        burnFromHero = fromHero;
    }

    /// <summary>Move slower (fraction 0.5 = half speed) for a while.</summary>
    public void Slow(float fraction, float seconds)
    {
        slowAmount = Mathf.Max(slowAmount, fraction);
        slowTimer = Mathf.Max(slowTimer, seconds);
    }

    /// <summary>Damage from a tower (ghosts are hard for towers to hit).</summary>
    public void TakeTowerDamage(float amount)
    {
        LastHitByHero = false;
        Health.TakeDamage(amount * Def.TowerDamageTaken);
    }

    /// <summary>Knock the enemy back (bosses barely move).</summary>
    public void Push(Vector2 offset)
    {
        if (IsBoss) offset *= 0.15f;
        if (rb != null) rb.position += offset;
        else transform.position += (Vector3)offset;
    }

    /// <summary>Burn, stun, slow and tint. Returns true if the enemy can't act this frame.</summary>
    bool UpdateStatus()
    {
        if (burnTimer > 0f)
        {
            burnTimer -= Time.deltaTime;
            LastHitByHero = burnFromHero;
            Health.TakeDamage(burnDps * Time.deltaTime);
            if (Health.IsDead) return true;
            if (burnTimer <= 0f) burnDps = 0f;
        }
        if (stunTimer > 0f) stunTimer -= Time.deltaTime;
        if (slowTimer > 0f) { slowTimer -= Time.deltaTime; if (slowTimer <= 0f) slowAmount = 0f; }

        Color tint = baseColor;
        if (burnTimer > 0f) tint = Color.Lerp(baseColor, new Color(1f, 0.55f, 0.1f, baseColor.a), 0.5f + 0.3f * Mathf.Sin(Time.time * 20f));
        if (slowTimer > 0f) tint = Color.Lerp(tint, new Color(0.55f, 0.85f, 1f, baseColor.a), 0.5f);
        if (stunTimer > 0f) tint = Color.Lerp(tint, new Color(0.6f, 0.8f, 1f, baseColor.a), 0.6f);
        if (Anim.Flash > 0f) tint = Color.Lerp(tint, new Color(1f, 0.35f, 0.35f, baseColor.a), Anim.Flash); // just got hit
        if (burnTimer > 0f && Random.value < 8f * Time.deltaTime)
            Juice.Sparkle(transform.position, new Color(1f, 0.55f, 0.15f), 1, HitRadius * 0.6f); // embers
        body.color = tint;
        Anim.Dizzy = stunTimer > 0f;
        return stunTimer > 0f;
    }

    /// <summary>Make an enemy. hpMultiplier grows the longer you survive.</summary>
    public static Enemy Spawn(EnemyKind kind, Vector3 position, float hpMultiplier, EnemyMode mode = EnemyMode.Raider, Dungeon dungeon = null)
    {
        var def = EnemyDefs.Get(kind);

        // Harder lands have harder monsters (see AreaLocks for the numbers).
        var locks = AreaLocks.Instance;
        if (locks != null)
        {
            Biome land = dungeon != null ? dungeon.Biome
                       : def.Home ?? (GameManager.Instance.Map != null ? GameManager.Instance.Map.BiomeAt(position) : Biome.DarkForest);
            if (land == Biome.Meadow) land = Biome.DarkForest;
            float hpScale = locks.HealthScale(land), dmgScale = locks.DamageScale(land);
            hpMultiplier *= hpScale;
            def.Dps *= dmgScale;
            def.ShotDamage *= dmgScale;
            def.Xp = Mathf.RoundToInt(def.Xp * hpScale); // tougher monsters are worth more
        }

        var go = new GameObject(def.Name);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * def.Scale;

        // The sprite sits on a child "Body" so the animator can bob and squash it freely.
        var sr = SpriteAnimator.CreateBody(go);
        sr.sprite = SpriteFactory.Enemy(kind);
        sr.sortingOrder = def.IsBoss ? 11 : 10;

        // Glowing eyes (and crystals, lava...). With fog of war they only show when the enemy is in light (FogHidden).
        SpriteFactory.AddGlow(sr);
        GlowPulse.On(sr, GlowPulse.Mode.Blink); // eyes blink now and then

        var e = go.AddComponent<Enemy>();
        e.Kind = kind;
        e.Def = def;
        e.Mode = mode;
        e.Dungeon = dungeon;
        e.home = position;
        e.HitRadius = def.HitRadius;
        e.speed = def.Speed * Random.Range(0.9f, 1.1f);
        e.wobbleSeed = Random.value * 10f;
        e.shotTimer = Random.Range(0.5f, def.ShotInterval);
        e.body = sr;
        e.glow = sr.transform.Find("Glow")?.GetComponent<SpriteRenderer>();
        if (kind == EnemyKind.Ghost) e.baseColor = new Color(1f, 1f, 1f, 0.75f);
        e.Health = go.GetComponent<Health>();
        float barHeight = sr.sprite.bounds.extents.y + 0.12f / def.Scale;
        e.Health.Init(def.Hp * hpMultiplier, def.IsBoss ? 1.4f : 0.8f, barHeight);
        e.Health.Died += e.OnDied;
        e.Anim = SpriteAnimator.Attach(go, sr, StyleFor(kind), def.IsBoss ? 0.8f : 1f);
        e.Health.Damaged += amount => e.Anim.OnDamaged(amount, e.Health.Max);
        e.Anim.Appear(duration: def.IsBoss ? 0.6f : 0.35f);

        // In dungeons, enemies are physical so they bump into walls (and each other).
        if (mode == EnemyMode.Dungeon)
        {
            e.rb = go.AddComponent<Rigidbody2D>();
            e.rb.gravityScale = 0f;
            e.rb.freezeRotation = true;
            e.rb.linearDamping = 4f;
            e.rb.mass = def.IsBoss ? 50f : 1f;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = def.HitRadius * 0.8f / def.Scale;
        }

        if (def.IsBoss) go.AddComponent<Boss>();
        go.AddComponent<FogHidden>(); // no glowing eyes or health bar in the dark

        GameManager.Instance.Enemies.Add(e);
        return e;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.IsGameOver) return;
        if (UpdateStatus()) { Stop(); return; } // stunned (or just burned to death)

        PickGoal(gm, out Transform goal, out Health goalHealth, out float goalRadius);
        HasGoal = goal != null;
        if (goal == null)
        {
            // Nothing to do: guards wander back home, everyone else waits.
            if (Mode == EnemyMode.Guard && Vector3.Distance(transform.position, home) > 0.5f)
                Move((home - transform.position).normalized, speed * 0.6f);
            else Stop();
            return;
        }
        GoalPosition = goal.position;

        Vector3 to = goal.position - transform.position;
        to.z = 0f;
        float dist = to.magnitude;
        if (Mathf.Abs(to.x) > 0.05f) Face(to.x < 0f);

        // Ranged enemies stop at a distance and shoot.
        if (Def.RangedRange > 0f && dist <= Def.RangedRange)
        {
            Stop();
            shotTimer -= Time.deltaTime;
            if (shotTimer <= 0f)
            {
                shotTimer = Def.ShotInterval;
                Anim.Recoil(to);
                EnemyBolt.Fire(transform.position + Vector3.up * 0.2f, to, Def.ShotDamage, Def.ShotKind);
            }
            return;
        }

        float reach = goalRadius + AttackReach + HitRadius * 0.5f;
        if (dist > reach)
        {
            float s = speed * (1f - slowAmount);
            Vector2 dir = to / dist;
            if (Def.Wobbles) dir = (dir + new Vector2(-dir.y, dir.x) * Mathf.Sin(Time.time * 5f + wobbleSeed) * 0.7f).normalized;
            Move(dir, s);
        }
        else
        {
            Stop();
            // Damage is dealt smoothly every frame; the animation shows it as a bite/swing every so often.
            attackAnimTimer -= Time.deltaTime;
            if (attackAnimTimer <= 0f)
            {
                attackAnimTimer = IsBoss ? 0.9f : 0.55f;
                Anim.Lunge(to, IsBoss ? 0.25f : 0.2f);
            }
            float dmg = Def.Dps * Time.deltaTime;
            goalHealth.TakeDamage(dmg);
            if (Def.Lifesteal > 0f) Health.Heal(dmg * Def.Lifesteal);
            if (Def.SlowsOnHit && gm.Player != null && goalHealth == gm.Player.Health) gm.Player.Slow(0.45f, 1f);
        }
    }

    /// <summary>Decide what to go after, depending on the enemy's mode.</summary>
    void PickGoal(GameManager gm, out Transform goal, out Health goalHealth, out float goalRadius)
    {
        goal = null; goalHealth = null; goalRadius = 0f;
        var player = gm.Player;
        bool playerHere = player != null && player.IsAlive && gm.Dungeons.Current == Dungeon;
        float playerDist = playerHere ? Vector3.Distance(transform.position, player.transform.position) : float.MaxValue;

        void TargetPlayer(ref Transform g, ref Health h, ref float r) { g = player.transform; h = player.Health; r = PlayerController.Radius; }

        switch (Mode)
        {
            case EnemyMode.Dungeon:
                if (playerDist < (IsBoss ? 14f : 7f)) TargetPlayer(ref goal, ref goalHealth, ref goalRadius);
                return;

            case EnemyMode.Guard:
                // Guards in a sealed land stay behind the seal.
                bool sealedIn = gm.Locks != null && !gm.Locks.IsUnlocked(gm.Map.BiomeAt(home));
                if (!sealedIn && playerDist < 5f && Vector3.Distance(player.transform.position, home) < 10f)
                {
                    TargetPlayer(ref goal, ref goalHealth, ref goalRadius);
                    return;
                }
                // Defend the area: attack buildings put up close to home (like Miners on "their" ore).
                var near = gm.FindClosestBuilding(home);
                if (near != null && Vector3.Distance(near.transform.position, home) < 4f)
                {
                    goal = near.transform; goalHealth = near.Health; goalRadius = near.Radius;
                }
                return;

            default: // Raider
                retargetTimer -= Time.deltaTime;
                if (target == null || retargetTimer <= 0f)
                {
                    target = gm.FindClosestBuilding(transform.position);
                    retargetTimer = 0.5f;
                }
                float buildingDist = target != null
                    ? Vector3.Distance(transform.position, target.transform.position) - target.Radius
                    : float.MaxValue;
                float aggro = IsBoss ? 7f : PlayerAggroRange;
                if (playerDist < aggro && playerDist < buildingDist)
                    TargetPlayer(ref goal, ref goalHealth, ref goalRadius);
                else if (target != null)
                {
                    goal = target.transform; goalHealth = target.Health; goalRadius = target.Radius;
                }
                return;
        }
    }

    void Move(Vector2 dir, float spd)
    {
        if (rb != null) rb.linearVelocity = dir * spd;
        else transform.position += (Vector3)(dir * (spd * Time.deltaTime));
    }

    void Stop()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    void Face(bool left)
    {
        if (body.flipX == left) return;
        body.flipX = left;
        if (glow != null) glow.flipX = left;
    }

    void OnDied()
    {
        Anim.Die(duration: IsBoss ? 1.2f : 0.45f);
        if (IsBoss) { Juice.Shake(0.6f); Juice.Sparkle(transform.position, new Color(1f, 0.85f, 0.4f), 30, 1.2f); }
        GameManager.Instance.OnEnemyKilled(this, Def.Xp);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.Enemies.Remove(this);
    }
}
