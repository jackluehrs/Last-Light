using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The hero's class abilities.
///   Q / F use the active abilities your class has learned (see SkillTree for which is which).
/// Also runs passive skill effects that tick over time (Bright Flame's burning aura, Phoenix).
/// </summary>
public class HeroAbilities : MonoBehaviour
{
    PlayerController player;
    HeroSkills skills;

    float qCooldown, fCooldown;
    float qCooldownMax = 1f, fCooldownMax = 1f;
    float auraTimer;
    float phoenixCooldown;
    const float PhoenixCooldownMax = 90f;

    void Start()
    {
        player = GetComponent<PlayerController>();
        skills = GetComponent<HeroSkills>();
        player.Health.TryCheatDeath = TryPhoenix;
    }

    /// <summary>The learned active skill on a key ("Q" or "F") for the current class, or null.</summary>
    public SkillDef AbilityOn(string key)
    {
        foreach (var s in SkillTree.All)
            if (s.Key == key && s.Class == skills.Class && skills.Has(s.Id)) return s;
        return null;
    }

    /// <summary>0 = ready, 1 = just used. For the HUD.</summary>
    public float CooldownFraction(string key) =>
        key == "Q" ? Mathf.Clamp01(qCooldown / qCooldownMax) : Mathf.Clamp01(fCooldown / fCooldownMax);

    public float CooldownSeconds(string key) => key == "Q" ? qCooldown : fCooldown;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.IsGameOver || gm.IsMenuOpen) return;

        qCooldown -= Time.deltaTime;
        fCooldown -= Time.deltaTime;
        phoenixCooldown -= Time.deltaTime;
        if (!player.IsAlive) return;

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.qKey.wasPressedThisFrame) TryUse("Q");
            if (kb.fKey.wasPressedThisFrame) TryUse("F");
        }

        BrightFlameAura();
    }

    void TryUse(string key)
    {
        var skill = AbilityOn(key);
        if (skill == null) return;
        float cd = key == "Q" ? qCooldown : fCooldown;
        if (cd > 0f)
        {
            GameManager.Instance.ShowPopup(transform.position + Vector3.up * 0.9f, $"{skill.Name}: {Mathf.CeilToInt(cd)}s", Color.white);
            return;
        }

        float cooldown = skill.Id switch
        {
            "whirlwind" => Whirlwind(),
            "warcry" => WarCry(),
            "volley" => Volley(),
            "dash" => Dash(),
            "fireball" => Fireball(),
            "nova" => FlameNova(),
            _ => 0f
        };
        if (cooldown <= 0f) return; // didn't happen (e.g. dash while standing still)

        if (key == "Q") { qCooldown = qCooldownMax = cooldown; }
        else { fCooldown = fCooldownMax = cooldown; }
    }

    // ---------- Knight ----------

    float Whirlwind()
    {
        const float radius = 2.2f;
        Fx.Ring(transform.position, radius, new Color(0.8f, 0.9f, 1f), 0.3f);
        player.Anim.Spin(0.3f);
        foreach (var e in GameManager.Instance.Enemies.ToArray())
        {
            if (e == null || e.Health.IsDead) continue;
            Vector2 to = e.transform.position - transform.position;
            if (to.magnitude > radius + e.HitRadius) continue;
            player.MeleeHit(e, player.SwordDamage * 2f, to.normalized * 0.8f);
        }
        return 8f;
    }

    float WarCry()
    {
        const float radius = 5f;
        Fx.Ring(transform.position, radius, new Color(0.5f, 0.7f, 1f), 0.5f);
        player.Anim.Pulse(1.5f);
        player.Anim.Hop(0.25f);
        Juice.Shake(0.2f);
        foreach (var e in GameManager.Instance.Enemies)
            if (e != null && Vector2.Distance(e.transform.position, transform.position) <= radius)
                e.Stun(2.5f);
        player.Health.Heal(player.Health.Max * 0.25f);
        GameManager.Instance.ShowPopup(transform.position + Vector3.up * 0.9f, "WAR CRY!", new Color(0.6f, 0.8f, 1f));
        return 20f;
    }

    // ---------- Ranger ----------

    float Volley()
    {
        const int arrows = 16;
        player.Anim.Pulse(1.2f);
        for (int i = 0; i < arrows; i++)
        {
            float a = i * Mathf.PI * 2f / arrows;
            Arrow.Fire(transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), player.ArrowDamage * 1.5f,
                       player.arrowSpeed, player.arrowRange);
        }
        return 10f;
    }

    float Dash()
    {
        Vector2 dir = player.MoveDirection;
        if (dir.sqrMagnitude < 0.01f) dir = player.Aim; // standing still: dash toward the mouse
        player.StartDash(dir, 4f, 0.18f);
        return 6f;
    }

    // ---------- Pyromancer ----------

    float Fireball()
    {
        float damage = 25f + 3f * player.Level;
        player.Anim.Lunge(player.Aim, 0.2f);
        FireballProjectile.Throw(transform.position, player.Aim, damage, burnDps: 6f + player.Level * 0.5f);
        return 5f;
    }

    float FlameNova()
    {
        const float radius = 4f;
        Fx.Ring(transform.position, radius, new Color(1f, 0.5f, 0.15f), 0.45f);
        Fx.Flash(transform.position, radius + 1f, new Color(1f, 0.55f, 0.2f), 0.4f);
        player.Anim.Pulse(2f);
        Juice.Shake(0.25f);
        Juice.Sparkle(transform.position, new Color(1f, 0.55f, 0.15f), 30, radius * 0.5f);
        float damage = 20f + 2f * player.Level;
        foreach (var e in GameManager.Instance.Enemies.ToArray())
        {
            if (e == null || e.Health.IsDead) continue;
            if (Vector2.Distance(e.transform.position, transform.position) > radius + e.HitRadius) continue;
            e.LastHitByHero = true;
            e.Health.TakeDamage(damage);
            if (e != null && !e.Health.IsDead) e.Burn(8f + player.Level * 0.5f, 4f);
        }
        return 15f;
    }

    /// <summary>Bright Flame: enemies close to your torch keep burning.</summary>
    void BrightFlameAura()
    {
        int rank = skills.Rank("brightflame");
        if (rank == 0) return;
        auraTimer -= Time.deltaTime;
        if (auraTimer > 0f) return;
        auraTimer = 0.5f;

        float radius = player.TorchRadius * 0.45f;
        foreach (var e in GameManager.Instance.Enemies)
            if (e != null && Vector2.Distance(e.transform.position, transform.position) <= radius)
                e.Burn(3f * rank, 1f);
    }

    /// <summary>Phoenix: survive a killing blow once every 90 seconds.</summary>
    bool TryPhoenix()
    {
        if (skills == null || !skills.Has("phoenix") || phoenixCooldown > 0f) return false;
        phoenixCooldown = PhoenixCooldownMax;
        Fx.Ring(transform.position, 3f, new Color(1f, 0.7f, 0.2f), 0.6f);
        Fx.Flash(transform.position, 4f, new Color(1f, 0.6f, 0.2f), 0.6f);
        player.Anim.Hop(0.4f);
        Juice.Sparkle(transform.position, new Color(1f, 0.7f, 0.2f), 30, 0.6f);
        GameManager.Instance.ShowPopup(transform.position + Vector3.up * 0.9f, "PHOENIX!", new Color(1f, 0.7f, 0.2f));
        return true;
    }

    public float PhoenixCooldown => Mathf.Max(0f, phoenixCooldown);
}
