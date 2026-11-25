using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// A shot fired by an enemy (skeleton bones, imp fireballs, boss attacks).
/// Flies straight; hurts the hero or the first building it touches; stopped by walls and trees.
/// kind: 0 bone, 1 web (slows the hero), 2 soul bolt, 3 boulder, 4 blood bolt, 5 fireball.
/// </summary>
public class EnemyBolt : MonoBehaviour
{
    Vector2 velocity;
    float damage, distanceLeft, spin;
    int kind;
    float age;

    public static void Fire(Vector3 from, Vector2 direction, float damage, int kind, float speed = 6f, float range = 8f)
    {
        var sr = new GameObject("Enemy Shot").AddComponent<SpriteRenderer>();
        sr.transform.position = from;
        sr.sprite = kind == 5 ? SpriteFactory.Fireball() : SpriteFactory.EnemyShot(kind);
        sr.sortingOrder = 16;
        SpriteFactory.MakeUnlit(sr);
        if (kind == 3) sr.transform.localScale = Vector3.one * 1.6f; // boulders are big

        var b = sr.gameObject.AddComponent<EnemyBolt>();
        b.velocity = direction.normalized * speed;
        b.damage = damage;
        b.kind = kind;
        b.distanceLeft = range;
        b.spin = kind == 0 || kind == 3 ? 540f : 0f;
        if (kind == 5) NightLighting.AddPointLight(sr.transform, Vector3.zero, 2f, new Color(1f, 0.55f, 0.2f), 0.8f);
        if (kind == 2) NightLighting.AddPointLight(sr.transform, Vector3.zero, 1.5f, new Color(0.4f, 1f, 0.6f), 0.7f);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.IsGameOver || gm.IsMenuOpen) return;

        Vector3 step = velocity * Time.deltaTime;
        transform.position += step;
        if (spin != 0f) transform.Rotate(0, 0, spin * Time.deltaTime);
        else
        {
            // Magic shots throb and leave a glowing trail.
            age += Time.deltaTime;
            transform.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(age * 28f));
            if (kind != 1 && Random.value < 30f * Time.deltaTime) Juice.Trail(transform.position, TrailColor);
        }
        distanceLeft -= step.magnitude;
        if (distanceLeft <= 0f) { Destroy(gameObject); return; }

        var p = gm.Player;
        if (p != null && p.IsAlive && Vector2.Distance(p.transform.position, transform.position) < PlayerController.Radius + 0.15f)
        {
            p.Health.TakeDamage(damage);
            if (kind == 1) p.Slow(0.5f, 2f);
            Impact();
            Destroy(gameObject);
            return;
        }

        foreach (var b in gm.Buildings)
        {
            if (b == null || b.Health.IsDead) continue;
            if (Vector2.Distance(b.transform.position, transform.position) > b.Radius) continue;
            b.Health.TakeDamage(damage);
            Impact();
            Destroy(gameObject);
            return;
        }

        // Walls and trees stop shots
        foreach (var hit in Physics2D.OverlapPointAll(transform.position))
            if (hit.GetComponent<TilemapCollider2D>() != null) { Impact(); Destroy(gameObject); return; }
    }

    Color TrailColor => kind switch
    {
        2 => new Color(0.4f, 1f, 0.6f),
        4 => new Color(0.9f, 0.15f, 0.25f),
        5 => new Color(1f, 0.55f, 0.2f),
        _ => new Color(0.9f, 0.9f, 0.85f),
    };

    /// <summary>Bits fly off where the shot lands.</summary>
    void Impact()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (kind == 3) { Juice.Dust(transform.position, 8, 0.6f); Juice.Shake(0.12f); }
        if (sr != null) Juice.BurstFromSprite(transform.position, sr.sprite, kind == 3 ? 10 : 5, 2.2f);
    }
}
