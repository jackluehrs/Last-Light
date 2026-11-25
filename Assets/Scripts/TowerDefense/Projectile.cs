using UnityEngine;

/// <summary>A homing tower shot. Disappears if its target dies first. Cannon shells explode on impact.</summary>
public class Projectile : MonoBehaviour
{
    Enemy target;
    float damage;
    float speed;
    float splash;
    float startDist, age;
    Color color;
    Vector3 baseScale;

    public static void Fire(Vector3 from, Enemy target, float damage, float speed, Color color, Sprite sprite = null, float splash = 0f)
    {
        var go = new GameObject("Projectile");
        go.transform.position = from;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : SpriteFactory.Bolt();
        sr.color = color;
        sr.sortingOrder = 15;
        SpriteFactory.MakeUnlit(sr); // shots stay visible in the dark

        var p = go.AddComponent<Projectile>();
        p.target = target;
        p.damage = damage;
        p.speed = speed;
        p.splash = splash;
        p.color = color == Color.white && splash > 0f ? new Color(0.7f, 0.7f, 0.7f) : color;
        p.startDist = Mathf.Max(0.5f, Vector3.Distance(from, target.transform.position));
        p.baseScale = go.transform.localScale;
    }

    void Update()
    {
        if (target == null || target.Health.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 to = target.transform.position - transform.position;
        float step = speed * Time.deltaTime;
        if (to.magnitude <= step + 0.1f)
        {
            if (splash > 0f) Explode(target.transform.position);
            else
            {
                target.TakeTowerDamage(damage);
                Juice.Burst(target.transform.position, color, 5, 2f, glow: true, size: 1f);
            }
            Destroy(gameObject);
            return;
        }
        transform.position += to.normalized * step;

        // Animate: shards spin and twinkle; cannonballs grow as they "arc" up toward the camera and back down.
        age += Time.deltaTime;
        if (splash > 0f)
        {
            float progress = Mathf.Clamp01(1f - to.magnitude / startDist);
            transform.localScale = baseScale * (1f + 0.7f * 4f * progress * (1f - progress));
            transform.Rotate(0f, 0f, 400f * Time.deltaTime);
            if (Random.value < 20f * Time.deltaTime) Juice.Trail(transform.position, new Color(0.75f, 0.72f, 0.68f, 0.6f), 2f);
        }
        else
        {
            transform.Rotate(0f, 0f, 720f * Time.deltaTime);
            transform.localScale = baseScale * (1f + 0.2f * Mathf.Sin(age * 40f));
            if (Random.value < 30f * Time.deltaTime) Juice.Trail(transform.position, color);
        }
    }

    void Explode(Vector3 at)
    {
        Fx.Ring(at, splash, new Color(1f, 0.75f, 0.4f), 0.3f);
        Fx.Flash(at, splash + 1.5f, new Color(1f, 0.6f, 0.3f), 0.25f);
        Juice.Burst(at, new Color(1f, 0.6f, 0.25f), 12, 3.5f, glow: true);
        Juice.Dust(at, 8, splash * 0.7f, new Color(0.45f, 0.42f, 0.4f, 0.8f));
        Juice.Shake(0.08f);
        foreach (var e in GameManager.Instance.Enemies.ToArray())
            if (e != null && !e.Health.IsDead && Vector2.Distance(e.transform.position, at) <= splash + e.HitRadius)
                e.TakeTowerDamage(damage);
    }
}
