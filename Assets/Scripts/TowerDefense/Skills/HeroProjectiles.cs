using UnityEngine;

/// <summary>A Ranger arrow: flies straight and hits the first enemy it touches.</summary>
public class Arrow : MonoBehaviour
{
    Vector2 velocity;
    float damage;
    float distanceLeft;

    public static void Fire(Vector3 from, Vector2 direction, float damage, float speed, float range)
    {
        var sr = new GameObject("Arrow").AddComponent<SpriteRenderer>();
        sr.transform.position = from;
        sr.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        sr.sprite = SpriteFactory.ArrowSprite();
        sr.sortingOrder = 15;
        SpriteFactory.MakeUnlit(sr);

        var a = sr.gameObject.AddComponent<Arrow>();
        a.velocity = direction.normalized * speed;
        a.damage = damage;
        a.distanceLeft = range;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.IsMenuOpen) return;

        Vector3 step = velocity * Time.deltaTime;
        transform.position += step;
        distanceLeft -= step.magnitude;
        if (distanceLeft <= 0f) { Destroy(gameObject); return; }

        foreach (var e in gm.Enemies)
        {
            if (e == null || e.Health.IsDead) continue;
            if (Vector2.Distance(e.transform.position, transform.position) > e.HitRadius + 0.15f) continue;
            e.LastHitByHero = true;
            e.Health.TakeDamage(damage);
            Juice.Burst(transform.position, new Color(0.66f, 0.45f, 0.23f), 3, 1.5f, size: 1f); // splinters
            Destroy(gameObject);
            return;
        }
    }
}

/// <summary>
/// A Pyromancer fireball: flies toward the mouse, explodes on the first enemy (or at max range),
/// damaging and burning everything nearby. Carries its own light, so it lights up the dark as it flies.
/// </summary>
public class FireballProjectile : MonoBehaviour
{
    const float Speed = 9f;
    const float Range = 8f;
    const float BlastRadius = 1.8f;

    Vector2 velocity;
    float damage, burnDps, distanceLeft;

    public static void Throw(Vector3 from, Vector2 direction, float damage, float burnDps)
    {
        var sr = new GameObject("Fireball").AddComponent<SpriteRenderer>();
        sr.transform.position = from;
        sr.sprite = SpriteFactory.Fireball();
        sr.sortingOrder = 15;
        SpriteFactory.MakeUnlit(sr);
        NightLighting.AddPointLight(sr.transform, Vector3.zero, 3f, new Color(1f, 0.6f, 0.25f), 1f);

        var f = sr.gameObject.AddComponent<FireballProjectile>();
        f.velocity = direction.normalized * Speed;
        f.damage = damage;
        f.burnDps = burnDps;
        f.distanceLeft = Range;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.IsMenuOpen) return;

        Vector3 step = velocity * Time.deltaTime;
        transform.position += step;
        transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(Time.time * 30f)); // flicker
        transform.Rotate(0f, 0f, 500f * Time.deltaTime);
        Juice.Trail(transform.position, Random.value < 0.5f ? new Color(1f, 0.6f, 0.2f) : new Color(1f, 0.9f, 0.5f), 2f);
        distanceLeft -= step.magnitude;

        bool hit = distanceLeft <= 0f;
        if (!hit)
            foreach (var e in gm.Enemies)
                if (e != null && !e.Health.IsDead &&
                    Vector2.Distance(e.transform.position, transform.position) <= e.HitRadius + 0.25f)
                { hit = true; break; }

        if (hit) Explode();
    }

    void Explode()
    {
        Fx.Ring(transform.position, BlastRadius, new Color(1f, 0.55f, 0.15f), 0.35f);
        Fx.Flash(transform.position, BlastRadius + 1.5f, new Color(1f, 0.55f, 0.2f), 0.35f);
        Juice.Burst(transform.position, new Color(1f, 0.55f, 0.15f), 16, 4f, glow: true);
        Juice.Shake(0.15f);
        foreach (var e in GameManager.Instance.Enemies.ToArray())
        {
            if (e == null || e.Health.IsDead) continue;
            if (Vector2.Distance(e.transform.position, transform.position) > BlastRadius + e.HitRadius) continue;
            e.LastHitByHero = true;
            e.Health.TakeDamage(damage);
            if (e != null && !e.Health.IsDead) e.Burn(burnDps, 3f);
        }
        Destroy(gameObject);
    }
}

/// <summary>Quick visual effects: expanding rings and bursts of light.</summary>
public class Fx : MonoBehaviour
{
    float duration, age, maxScale;
    bool warning, fixedScale;
    SpriteRenderer sr;
    UnityEngine.Rendering.Universal.Light2D light2D;
    Color color;

    /// <summary>A ring that grows out to the given radius and fades.</summary>
    public static void Ring(Vector3 at, float radius, Color color, float duration)
    {
        var sr = new GameObject("Fx Ring").AddComponent<SpriteRenderer>();
        sr.transform.position = at;
        sr.sprite = SpriteFactory.Ring();
        sr.sortingOrder = 28;
        SpriteFactory.MakeUnlit(sr);
        var fx = sr.gameObject.AddComponent<Fx>();
        fx.sr = sr; fx.color = color; fx.duration = duration; fx.maxScale = radius * 2f;
    }

    /// <summary>A red circle that pulses on the ground: something is about to hit here!</summary>
    public static void Warning(Vector3 at, float radius, float duration)
    {
        var sr = new GameObject("Fx Warning").AddComponent<SpriteRenderer>();
        sr.transform.position = at;
        sr.transform.localScale = Vector3.one * radius * 2f;
        sr.sprite = SpriteFactory.Ring();
        sr.sortingOrder = 4;
        SpriteFactory.MakeUnlit(sr);
        var fx = sr.gameObject.AddComponent<Fx>();
        fx.sr = sr; fx.color = new Color(1f, 0.25f, 0.2f); fx.duration = duration; fx.warning = true;
    }

    /// <summary>A jagged lightning bolt between two points.</summary>
    public static void Lightning(Vector3 from, Vector3 to, Color color, float duration = 0.18f)
    {
        const int segments = 4;
        Vector3 prev = from;
        Vector2 perp = Vector2.Perpendicular(((Vector2)(to - from)).normalized);
        for (int i = 1; i <= segments; i++)
        {
            Vector3 next = Vector3.Lerp(from, to, i / (float)segments);
            if (i < segments) next += (Vector3)(perp * Random.Range(-0.3f, 0.3f));
            Line(prev, next, color, 0.09f, duration);
            prev = next;
        }
        Flash(to, 1.5f, color, duration);
    }

    /// <summary>A straight glowing line that fades out.</summary>
    public static void Line(Vector3 from, Vector3 to, Color color, float width, float duration)
    {
        var sr = new GameObject("Fx Line").AddComponent<SpriteRenderer>();
        Vector3 d = to - from;
        sr.transform.position = (from + to) / 2f;
        sr.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        sr.transform.localScale = new Vector3(d.magnitude, width, 1f);
        sr.sprite = SpriteFactory.Pixel();
        sr.sortingOrder = 28;
        SpriteFactory.MakeUnlit(sr);
        var fx = sr.gameObject.AddComponent<Fx>();
        fx.sr = sr; fx.color = color; fx.duration = duration; fx.fixedScale = true;
    }

    /// <summary>A short burst of light that lights up the dark.</summary>
    public static void Flash(Vector3 at, float radius, Color color, float duration)
    {
        var go = new GameObject("Fx Flash");
        go.transform.position = at;
        var light = NightLighting.AddPointLight(go.transform, Vector3.zero, radius, color, 1.5f);
        var fx = go.AddComponent<Fx>();
        fx.light2D = light; fx.color = color; fx.duration = duration;
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = age / duration;
        if (t >= 1f) { Destroy(gameObject); return; }
        if (sr != null && warning)
            sr.color = new Color(color.r, color.g, color.b, 0.4f + 0.5f * Mathf.Abs(Mathf.Sin(age * 14f)));
        else if (sr != null)
        {
            if (!fixedScale) transform.localScale = Vector3.one * Mathf.Lerp(0.2f, maxScale, Mathf.Sqrt(t));
            sr.color = new Color(color.r, color.g, color.b, 1f - t);
        }
        if (light2D != null) light2D.intensity = 1.5f * (1f - t);
    }
}
