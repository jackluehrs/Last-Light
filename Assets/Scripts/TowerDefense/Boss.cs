using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Special attacks for boss enemies. Each boss has two abilities on timers, and below half health
/// it becomes enraged and uses them faster.
///   Spider Queen: calls spiderlings, spits webs that slow you.
///   Vampire Lord: blinks next to you, summons bats, fires a fan of blood bolts. Heals when it bites.
///   Lich King:    a ring of soul bolts in every direction, raises skeletons.
///   Rock Giant:   ground slam (watch for the red circle!), throws boulders.
/// Movement and biting are handled by the normal Enemy component.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class Boss : MonoBehaviour
{
    Enemy enemy;
    float timerA = 3f, timerB = 5f;
    bool enraged;
    readonly List<Enemy> minions = new();

    const int MaxMinions = 6;

    void Start()
    {
        enemy = GetComponent<Enemy>();
    }

    float Pace => enraged ? 0.65f : 1f;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.IsGameOver || enemy.Health.IsDead || enemy.IsStunned) return;
        if (!enemy.HasGoal) return;

        if (!enraged && enemy.Health.Current < enemy.Health.Max * 0.5f)
        {
            enraged = true;
            gm.Announce($"{enemy.Def.Name} is enraged!");
            Fx.Ring(transform.position, 3f, new Color(1f, 0.3f, 0.3f), 0.5f);
            enemy.Anim.Pulse(2f);
            enemy.Anim.Hop(0.4f);
            Juice.Shake(0.35f);
        }

        minions.RemoveAll(m => m == null);
        Vector3 aim = enemy.GoalPosition;

        timerA -= Time.deltaTime;
        timerB -= Time.deltaTime;

        switch (enemy.Kind)
        {
            case EnemyKind.SpiderQueen:
                if (timerA <= 0f) { timerA = 4f * Pace; Volley(aim, 1, enraged ? 5 : 3, 25f, 9f, 5.5f); }
                if (timerB <= 0f) { timerB = 8f * Pace; Summon(EnemyKind.Spider, enraged ? 4 : 3); }
                break;

            case EnemyKind.VampireLord:
                if (timerA <= 0f) { timerA = 6f * Pace; Blink(aim); }
                if (timerB <= 0f)
                {
                    timerB = 5f * Pace;
                    if (minions.Count < 3) Summon(EnemyKind.Bat, 4);
                    else Volley(aim, 4, 5, 15f, 12f, 7f);
                }
                break;

            case EnemyKind.Lich:
                if (timerA <= 0f) { timerA = 3.5f * Pace; Nova(enraged ? 16 : 12, 11f); }
                if (timerB <= 0f) { timerB = 9f * Pace; Summon(EnemyKind.Skeleton, 2); Volley(aim, 2, 3, 12f, 10f, 6f); }
                break;

            case EnemyKind.RockGiant:
                if (timerA <= 0f) { timerA = 5f * Pace; StartCoroutine(Slam(aim)); }
                if (timerB <= 0f) { timerB = 3.5f * Pace; Volley(aim, 3, enraged ? 3 : 1, 18f, 22f, 5f); }
                break;
        }
    }

    /// <summary>Fire `count` shots in a fan toward a point.</summary>
    void Volley(Vector3 at, int kind, int count, float spreadDeg, float damage, float speed)
    {
        Vector2 dir = (at - transform.position).normalized;
        enemy.Anim.Lunge(dir, 0.2f);
        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) / 2f) * spreadDeg;
            EnemyBolt.Fire(transform.position, Quaternion.Euler(0, 0, offset) * dir, damage, kind, speed, 10f);
        }
    }

    /// <summary>Shots in every direction.</summary>
    void Nova(int count, float damage)
    {
        Fx.Ring(transform.position, 1.5f, new Color(0.4f, 1f, 0.6f), 0.4f);
        enemy.Anim.Pulse(1.5f);
        float start = Random.value * 360f;
        for (int i = 0; i < count; i++)
        {
            float a = (start + i * 360f / count) * Mathf.Deg2Rad;
            EnemyBolt.Fire(transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), damage, 2, 4.5f, 10f);
        }
    }

    void Summon(EnemyKind kind, int count)
    {
        var gm = GameManager.Instance;
        for (int i = 0; i < count && minions.Count < MaxMinions; i++)
        {
            Vector3 p = transform.position + (Vector3)(Random.insideUnitCircle.normalized * 1.2f);
            var m = Enemy.Spawn(kind, p, gm.Spawner.HpMultiplier,
                                enemy.Mode == EnemyMode.Dungeon ? EnemyMode.Dungeon : EnemyMode.Raider, enemy.Dungeon);
            minions.Add(m);
        }
        Fx.Ring(transform.position, 1.8f, new Color(0.8f, 0.4f, 1f), 0.4f);
        enemy.Anim.Hop(0.35f);
        Juice.Sparkle(transform.position, new Color(0.8f, 0.4f, 1f), 12, 1f);
    }

    /// <summary>Teleport to just behind the target.</summary>
    void Blink(Vector3 at)
    {
        Vector2 from = transform.position;
        Vector2 offset = ((Vector2)at - from).normalized * 1.4f;
        Vector3 to = at + (Vector3)offset;
        Fx.Flash(transform.position, 3f, new Color(1f, 0.2f, 0.3f), 0.3f);
        Fx.Ring(transform.position, 1.2f, new Color(0.9f, 0.1f, 0.2f), 0.3f);
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.position = to; else transform.position = to;
        Fx.Ring(to, 1.2f, new Color(0.9f, 0.1f, 0.2f), 0.3f);
        Juice.Burst(from, new Color(0.7f, 0.1f, 0.2f), 12, 3f, glow: true);
        enemy.Anim.Appear(duration: 0.25f);
    }

    /// <summary>Shows a red warning circle, then smashes everything inside it.</summary>
    IEnumerator Slam(Vector3 at)
    {
        const float radius = 1.8f, warn = 1f;
        Fx.Warning(at, radius, warn);
        enemy.Anim.Windup(warn); // crouches and trembles, then springs up and slams
        yield return new WaitForSeconds(warn);
        if (enemy == null || enemy.Health.IsDead) yield break;

        Fx.Ring(at, radius, new Color(1f, 0.6f, 0.2f), 0.35f);
        Fx.Flash(at, radius + 1.5f, new Color(1f, 0.5f, 0.2f), 0.3f);
        Juice.Shake(0.45f);
        Juice.Dust(at, 24, radius);
        Juice.Burst(at, new Color(0.55f, 0.45f, 0.4f), 14, 4f);
        var gm = GameManager.Instance;
        var p = gm.Player;
        if (p != null && p.IsAlive && Vector2.Distance(p.transform.position, at) <= radius + PlayerController.Radius)
            p.Health.TakeDamage(35f);
        foreach (var b in gm.Buildings.ToArray())
            if (b != null && Vector2.Distance(b.transform.position, at) <= radius + b.Radius)
                b.Health.TakeDamage(60f);
    }
}
