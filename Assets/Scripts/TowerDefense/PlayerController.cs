using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The hero.
///   WASD / arrow keys   move (trees block you)
///   Space   attack toward the mouse (hold to keep attacking). Knights/Pyromancers swing a sword, Rangers shoot arrows.
///   Q / F   class abilities (see HeroAbilities)
///   E       upgrade the building you're standing next to / enter or leave a dungeon
///   H       recall home (once the Hub is level 2)
///   K       open the skill tree
/// Stats come from the values below, plus hero level, plus learned skills (see Recalculate()).
/// </summary>
[RequireComponent(typeof(Health), typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4.5f;

    [Header("Health")]
    public float maxHealth = 100f;
    public float regenPerSecond = 4f;
    [Tooltip("Seconds without taking damage before regen starts.")]
    public float regenDelay = 4f;
    public float respawnSeconds = 5f;

    [Header("Sword")]
    public float swordDamage = 14f;
    public float swordRange = 1.4f;
    [Tooltip("Total width of the swing in degrees.")]
    public float swordArc = 130f;
    public float swordCooldown = 0.4f;
    public float knockback = 0.4f;

    [Header("Arrows (Ranger)")]
    [Tooltip("Arrow damage as a fraction of sword damage.")]
    public float arrowDamageFraction = 0.8f;
    public float arrowRange = 9f;
    public float arrowSpeed = 14f;

    [Header("Mining")]
    [Tooltip("How close (in tiles) the hero must be to an ore node to mine it.")]
    public float mineReach = 1.3f;
    [Tooltip("Seconds between pickaxe hits.")]
    public float mineInterval = 0.5f;
    public int goldPerMineHit = 1;

    [Header("Leveling")]
    [Tooltip("XP needed to go from level 1 to 2.")]
    public float xpForLevel2 = 20f;
    [Tooltip("Each level after that needs this much more XP.")]
    public float xpGrowthPerLevel = 15f;
    [Tooltip("Small automatic boosts each level. Most power comes from the skill tree.")]
    public float healthPerLevel = 8f;
    public float damagePerLevel = 1f;

    public int Level { get; private set; } = 1;
    public float Xp { get; private set; }
    public float XpToNext => xpForLevel2 + xpGrowthPerLevel * (Level - 1);

    public Health Health { get; private set; }
    public HeroSkills Skills { get; private set; }
    public bool IsAlive => Health != null && !Health.IsDead;
    public float RespawnTimer { get; private set; }
    public const float Radius = 0.35f;

    // ---- Final stats after level + skills (recalculated whenever either changes) ----
    public float SwordDamage { get; private set; }
    public float SwordArc { get; private set; }
    public float AttackCooldown { get; private set; }
    public float MoveSpeed { get; private set; }
    public float ArrowDamage { get; private set; }
    public float TorchRadius { get; private set; }
    public bool IsRanged => Skills.Class == HeroClass.Ranger;

    public Vector2 Aim => aim;
    public Vector2 MoveDirection => moveInput;
    public Rigidbody2D Body => body;

    /// <summary>Moves the hero's sprite: walking, swings, hits, dashing (see SpriteAnimator).</summary>
    public SpriteAnimator Anim { get; private set; }

    Rigidbody2D body;
    CircleCollider2D col;
    SpriteRenderer sr;
    SpriteRenderer slash;
    GameObject torch;
    LightFlicker torchFlicker;
    Vector2 moveInput;
    Vector2 aim = Vector2.right;
    float attackTimer;
    float slashTimer;
    float sinceHurt;
    float hurtFlash;
    SpriteRenderer glow; // glowing parts of the hero's sprite (the Pyromancer's fire orb)
    float regenRate, regenWait;

    // Dash (Ranger skill)
    Vector2 dashVelocity;
    float dashTimer;
    public bool IsDashing => dashTimer > 0f;

    public static PlayerController Spawn(Vector3 position)
    {
        var go = new GameObject("Player");
        go.transform.position = position;
        return go.AddComponent<PlayerController>();
    }

    void Awake()
    {
        // The sprite sits on a child "Body" so the animator can bob and squash it freely.
        sr = SpriteAnimator.CreateBody(gameObject);
        sr.sprite = SpriteFactory.Hero(HeroClass.None);
        sr.sortingOrder = 12;
        Anim = SpriteAnimator.Attach(gameObject, sr, AnimStyle.Walker);

        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        col = GetComponent<CircleCollider2D>();
        col.radius = 0.3f;

        Health = GetComponent<Health>();
        Health.Init(maxHealth, 0.8f, 0.6f);
        Health.Damaged += amount =>
        {
            Anim.OnDamaged(amount, Health.Max);
            if (amount >= 3f) Juice.Shake(Mathf.Clamp(amount / Health.Max * 2f, 0.08f, 0.3f));
            sinceHurt = 0f; hurtFlash = 0.15f;
            if (recallTimer > 0f) { recallTimer = 0f; GameManager.Instance.ShowPopup(transform.position + Vector3.up, "Recall interrupted!", new Color(1f, 0.5f, 0.5f)); }
        };
        Health.Died += OnDied;

        // Add the skill parts ourselves rather than relying on [RequireComponent].
        Skills = GetComponent<HeroSkills>();
        if (Skills == null) Skills = gameObject.AddComponent<HeroSkills>();
        if (GetComponent<HeroAbilities>() == null) gameObject.AddComponent<HeroAbilities>();
        Skills.Changed += Recalculate;

        slash = new GameObject("Sword Slash").AddComponent<SpriteRenderer>();
        slash.transform.SetParent(transform, false);
        slash.sprite = SpriteFactory.Slash();
        slash.sortingOrder = 13;
        slash.enabled = false;
        SpriteFactory.MakeUnlit(slash);

        CreateTorch();
        Recalculate();
    }

    /// <summary>A torch in the hero's free hand that lights up the area around them.</summary>
    void CreateTorch()
    {
        var flame = new GameObject("Torch").AddComponent<SpriteRenderer>();
        torch = flame.gameObject;
        torch.transform.SetParent(transform, false);
        torch.transform.localPosition = new Vector3(-0.3f, -0.15f, 0f);
        flame.sprite = SpriteFactory.Torch();
        Idle.On(flame.gameObject, Idle.Kind.Flicker);
        flame.sortingOrder = 13;
        SpriteFactory.MakeUnlit(flame); // the flame itself glows

        var lighting = GameManager.Instance != null ? GameManager.Instance.Lighting : null;
        float radius = lighting != null ? lighting.torchRadius : 5.5f;
        Color color = lighting != null ? lighting.torchColor : new Color(1f, 0.72f, 0.4f);
        float intensity = lighting != null ? lighting.torchIntensity : 1.2f;

        var light = NightLighting.AddPointLight(torch.transform, new Vector3(0f, 0.35f, 0f), radius, color, intensity);
        torchFlicker = light.gameObject.AddComponent<LightFlicker>();
    }

    /// <summary>Work out final stats from the base values, hero level and learned skills.</summary>
    public void Recalculate()
    {
        var s = Skills;

        // Dress the hero for their class
        var look = SpriteFactory.Hero(s.Class);
        if (sr.sprite != look)
        {
            sr.sprite = look;
            glow = SpriteFactory.AddGlow(sr);
            Anim.RefreshFeet();
        }
        int lv = Level - 1;

        float hp = maxHealth + healthPerLevel * lv + 20f * s.Rank("vitality");
        if (s.Has("knight")) hp += 30f;
        Health.SetMax(hp, fillUp: false);
        Health.DamageMultiplier = 1f - 0.1f * s.Rank("ironskin");

        SwordDamage = (swordDamage + damagePerLevel * lv) * (s.Has("knight") ? 1.25f : 1f);
        SwordArc = s.Has("knight") ? 200f : swordArc;
        AttackCooldown = swordCooldown * Mathf.Pow(0.97f, lv);

        ArrowDamage = (swordDamage + damagePerLevel * lv) * arrowDamageFraction * (1f + 0.15f * s.Rank("swift"));

        float speedBonus = (s.Has("ranger") ? 0.1f : 0f) + 0.1f * s.Rank("swift");
        MoveSpeed = moveSpeed * (1f + speedBonus);

        regenRate = regenPerSecond + 3f * s.Rank("secondwind");
        regenWait = Mathf.Max(1f, regenDelay - s.Rank("secondwind"));

        float baseTorch = GameManager.Instance != null ? GameManager.Instance.Lighting.torchRadius : 5.5f;
        float torchBonus = (s.Has("pyro") ? 0.3f : 0f) + 0.2f * s.Rank("brightflame");
        TorchRadius = baseTorch * (1f + torchBonus);
        if (torchFlicker != null) torchFlicker.SetBaseRadius(TorchRadius);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        moveInput = Vector2.zero;
        if (gm == null || gm.IsGameOver || gm.IsMenuOpen) return;

        if (!IsAlive)
        {
            RespawnTimer -= Time.deltaTime;
            if (RespawnTimer <= 0f) Respawn();
            return;
        }

        var kb = Keyboard.current;
        var mouse = Mouse.current;

        // --- Movement ---
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) moveInput.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) moveInput.y -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveInput.x += 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveInput.x -= 1;
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        }

        // --- Aim at the mouse ---
        if (mouse != null && Camera.main != null)
        {
            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -Camera.main.transform.position.z));
            Vector2 dir = world - transform.position;
            if (dir.sqrMagnitude > 0.01f) aim = dir.normalized;
        }
        sr.flipX = aim.x < 0f;
        if (glow != null) glow.flipX = sr.flipX;
        // Keep the torch in the free hand (the Ranger's bow is in the other hand, so theirs is swapped).
        float torchSide = Skills.Class == HeroClass.Ranger ? 0.38f : -0.38f;
        torch.transform.localPosition = new Vector3(sr.flipX ? -torchSide : torchSide, -0.2f, 0f);

        // --- Attack ---
        attackTimer -= Time.deltaTime;
        bool attackHeld = kb != null && kb.spaceKey.isPressed;
        if (attackHeld && attackTimer <= 0f)
        {
            // Next to ore with no enemies close? Mine it. Otherwise fight.
            float danger = IsRanged ? 3f : swordRange + 0.5f;
            if (!EnemyWithin(danger) && gm.FindMinableOre(transform.position, mineReach, out var oreCell, out var orePos))
                Mine(oreCell, orePos);
            else if (IsRanged) ShootArrows();
            else Swing();
        }

        // --- E: go through a dungeon door, or upgrade the building you're standing next to ---
        if (kb != null && kb.eKey.wasPressedThisFrame) gm.Interact();

        // --- H: recall home (unlocked at Hub level 2) ---
        if (kb != null && kb.hKey.wasPressedThisFrame) TryStartRecall();
        UpdateRecall();
        if (slowTimer > 0f && (slowTimer -= Time.deltaTime) <= 0f) slowAmount = 0f;

        // --- Slash effect: the arc sweeps across, grows and fades ---
        if (slashTimer > 0f)
        {
            slashTimer -= Time.deltaTime;
            float p = 1f - Mathf.Clamp01(slashTimer / SlashTime);
            float sweep = Mathf.Lerp(45f, -45f, Mathf.SmoothStep(0f, 1f, p)) * slashSide;
            float angle = slashAngle + sweep;
            Vector2 d = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            slash.transform.localPosition = (Vector3)(d * 0.55f);
            slash.transform.localRotation = Quaternion.Euler(0, 0, angle);
            slash.transform.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.15f, p);
            slash.color = new Color(1f, 1f, 1f, 1f - p * p);
            if (slashTimer <= 0f) slash.enabled = false;
        }

        // --- Dash ---
        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) Health.Invulnerable = false;
        }

        // --- Regen + hurt flash ---
        sinceHurt += Time.deltaTime;
        if (sinceHurt > regenWait) Health.Heal(regenRate * Time.deltaTime);
        hurtFlash -= Time.deltaTime;
        Color c = hurtFlash > 0f ? new Color(1f, 0.4f, 0.4f) : Color.white;
        if (IsDashing) c.a = 0.5f;
        sr.color = c;
        if (glow != null) glow.color = new Color(1f, 1f, 1f, c.a);
        Anim.Dashing = IsDashing;

        // Kick up a little dust now and then while running.
        if (moveInput.sqrMagnitude > 0.1f && Random.value < 4f * Time.deltaTime)
            Juice.Dust(transform.position + Vector3.down * 0.3f, 1, 0.2f);
    }

    void FixedUpdate()
    {
        body.linearVelocity = IsDashing ? dashVelocity : moveInput * (MoveSpeed * (1f - slowAmount));
    }

    /// <summary>Quickly move a set distance, untouchable for the duration.</summary>
    public void StartDash(Vector2 direction, float distance, float duration)
    {
        dashVelocity = direction.normalized * (distance / duration);
        dashTimer = duration;
        Health.Invulnerable = true;
    }

    // ---------- Slow, teleport and recall ----------

    float slowTimer, slowAmount;

    /// <summary>Move slower (0.5 = half speed) for a while. Spider bites and webs do this.</summary>
    public void Slow(float fraction, float seconds)
    {
        slowAmount = Mathf.Max(slowAmount, fraction);
        slowTimer = Mathf.Max(slowTimer, seconds);
    }

    /// <summary>Instantly move the hero somewhere (dungeon doors, recall).</summary>
    public void TeleportTo(Vector3 position)
    {
        position.z = 0f;
        transform.position = position;
        body.position = position;
        body.linearVelocity = Vector2.zero;
    }

    [Header("Recall (Hub level 2+)")]
    public float recallTime = 2f;
    public float recallCooldown = 20f;
    float recallTimer, recallCooldownLeft;

    /// <summary>0..1 while channeling a recall, otherwise -1.</summary>
    public float RecallProgress => recallTimer > 0f ? 1f - recallTimer / recallTime : -1f;

    void TryStartRecall()
    {
        var gm = GameManager.Instance;
        Vector3 pop = transform.position + Vector3.up;
        if (gm.Hub.Level < HubLevels.RecallLevel) { gm.ShowPopup(pop, "Recall unlocks at Hub level 2", Color.white); return; }
        if (recallTimer > 0f) return;
        if (recallCooldownLeft > 0f) { gm.ShowPopup(pop, $"Recall: {Mathf.CeilToInt(recallCooldownLeft)}s", Color.white); return; }
        recallTimer = recallTime;
        Fx.Ring(transform.position, 1.5f, new Color(1f, 0.85f, 0.4f), recallTime);
    }

    void UpdateRecall()
    {
        recallCooldownLeft -= Time.deltaTime;
        if (recallTimer <= 0f) return;
        recallTimer -= Time.deltaTime;
        if (recallTimer > 0f) return;

        var gm = GameManager.Instance;
        recallCooldownLeft = recallCooldown;
        Fx.Flash(transform.position, 3f, new Color(1f, 0.85f, 0.4f), 0.4f);
        if (gm.Dungeons.Current != null) gm.Dungeons.Exit(toHub: true);
        else { TeleportTo(gm.PlayerSpawnPoint); gm.SnapCamera(); }
        Fx.Flash(transform.position, 3f, new Color(1f, 0.85f, 0.4f), 0.4f);
    }

    bool EnemyWithin(float distance)
    {
        foreach (var e in GameManager.Instance.Enemies)
            if (e != null && !e.Health.IsDead && Vector2.Distance(e.transform.position, transform.position) <= distance)
                return true;
        return false;
    }

    void Mine(Vector2Int cell, Vector3 orePos)
    {
        attackTimer = mineInterval;
        Vector2 dir = ((Vector2)(orePos - transform.position)).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = aim;
        ShowSlash(dir);
        Anim.Lunge(dir, 0.14f); // pickaxe chop
        GameManager.Instance.MineOre(cell, goldPerMineHit);
    }

    const float SlashTime = 0.15f;
    float slashAngle, slashSide = 1f;

    void ShowSlash(Vector2 dir)
    {
        slashAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        slashSide = -slashSide; // alternate forehand / backhand
        slash.enabled = true;
        slashTimer = SlashTime;
    }

    void Swing()
    {
        attackTimer = AttackCooldown;

        ShowSlash(aim); // the slash arc points at the mouse
        Anim.Lunge(aim, 0.16f);

        // Hit every enemy inside the arc. (Copy the list: enemies can die and remove themselves.)
        foreach (var e in GameManager.Instance.Enemies.ToArray())
        {
            if (e == null || e.Health.IsDead) continue;
            Vector2 to = e.transform.position - transform.position;
            if (to.magnitude > swordRange + e.HitRadius) continue;
            if (Vector2.Angle(aim, to) > SwordArc / 2f && to.magnitude > 0.4f) continue;

            MeleeHit(e, SwordDamage, to.normalized * knockback);
        }
    }

    /// <summary>
    /// Damage an enemy up close, applying melee skill effects (Bloodlust healing, Pyromancer burning).
    /// Also used by the Whirlwind ability.
    /// </summary>
    public void MeleeHit(Enemy e, float damage, Vector2 push)
    {
        e.LastHitByHero = true;
        e.Health.TakeDamage(damage);
        Health.Heal(damage * 0.1f * Skills.Rank("bloodlust"));
        if (e == null || e.Health.IsDead) return;
        if (Skills.Has("pyro")) e.Burn(4f + Level * 0.5f, 3f);
        e.Push(push);
    }

    void ShootArrows()
    {
        attackTimer = AttackCooldown * 1.1f;
        int count = 1 + Skills.Rank("multishot");
        Anim.Recoil(aim, 0.6f);
        const float spread = 12f; // degrees between arrows
        for (int i = 0; i < count; i++)
        {
            // Fan the arrows out evenly around the aim direction.
            float offset = (i - (count - 1) / 2f) * spread;
            Vector2 dir = Quaternion.Euler(0, 0, offset) * aim;
            Arrow.Fire(transform.position, dir, ArrowDamage, arrowSpeed, arrowRange);
        }
    }

    public void AddXp(float amount)
    {
        Xp += amount;
        while (Xp >= XpToNext)
        {
            Xp -= XpToNext;
            LevelUp();
        }
    }

    void LevelUp()
    {
        Level++;
        Skills.AddPoint();
        Recalculate();
        if (IsAlive) Health.ResetToFull(); // level-ups heal you fully
        Anim.Hop(0.35f);
        Juice.Sparkle(transform.position, new Color(0.4f, 0.9f, 1f), 24, 0.6f);
        Fx.Ring(transform.position, 1.6f, new Color(0.4f, 0.9f, 1f), 0.5f);

        var gm = GameManager.Instance;
        gm.ShowPopup(transform.position + Vector3.up * 0.9f, "LEVEL UP!", new Color(0.4f, 0.9f, 1f));
        gm.Announce($"Hero reached level {Level}! You have a skill point. Press K to spend it.");
    }

    void OnDied()
    {
        RespawnTimer = respawnSeconds;
        Anim.Die(duration: 0.7f);
        Juice.Shake(0.35f);
        sr.enabled = false;
        if (glow != null) glow.enabled = false;
        col.enabled = false;
        slash.enabled = false;
        torch.SetActive(false);
        dashTimer = 0f;
        Health.Invulnerable = false;
        body.linearVelocity = Vector2.zero;
        recallTimer = 0f;
        slowAmount = 0f;
        GameManager.Instance.Dungeons.OnPlayerDied();
        GameManager.Instance.Announce($"You were defeated! Respawning at the Hub in {respawnSeconds:0} seconds...");
    }

    void Respawn()
    {
        var gm = GameManager.Instance;
        transform.position = gm.PlayerSpawnPoint;
        body.position = gm.PlayerSpawnPoint;
        Health.ResetToFull();
        sr.enabled = true;
        if (glow != null) glow.enabled = true;
        torch.SetActive(true);
        col.enabled = true;
        sinceHurt = 0f;
        Anim.Appear(duration: 0.45f);
        Fx.Ring(transform.position, 1.4f, new Color(1f, 0.85f, 0.4f), 0.5f);
        Juice.Sparkle(transform.position, new Color(1f, 0.85f, 0.4f), 20, 0.5f);
    }
}
