using UnityEngine;

/// <summary>
/// Little effects that make things feel alive: flying bits, dust puffs, sparkles, trails and screen shake.
/// Particles are chunky squares the size of one art pixel, so they match the pixel-art style.
/// </summary>
public static class Juice
{
    public const float ArtPixel = 1f / 16f; // one pixel of the sprites, in world units

    /// <summary>Bits flying out in all directions that pop up, fall and fade (death, debris, ore chips).</summary>
    public static void Burst(Vector3 at, Color color, int count, float speed = 2.5f, bool glow = false, float size = 1.5f)
    {
        for (int i = 0; i < count; i++)
        {
            Color c = Vary(color);
            var p = Particle.Make(at, c, ArtPixel * Random.Range(size * 0.7f, size * 1.3f), glow);
            p.velocity = Random.insideUnitCircle.normalized * Random.Range(0.3f, 1f) * speed;
            p.height = 0.05f;
            p.heightVel = Random.Range(1.5f, 3.5f);
            p.gravity = 12f;
            p.life = Random.Range(0.45f, 0.8f);
        }
    }

    /// <summary>Like Burst, but each bit takes a color from the sprite (so a goblin bursts into green bits).</summary>
    public static void BurstFromSprite(Vector3 at, Sprite sprite, int count, float speed = 2.5f, Color? tint = null)
    {
        for (int i = 0; i < count; i++)
        {
            Color c = SampleColor(sprite);
            if (tint.HasValue) c *= tint.Value;
            Burst(at + (Vector3)(Random.insideUnitCircle * 0.15f), c, 1, speed);
        }
    }

    /// <summary>Soft dust puffs that drift up and fade (landings, footfalls, crumbling).</summary>
    public static void Dust(Vector3 at, int count, float spread = 0.4f, Color? color = null)
    {
        Color baseC = color ?? new Color(0.78f, 0.72f, 0.62f, 0.75f);
        for (int i = 0; i < count; i++)
        {
            var p = Particle.Make(at + (Vector3)(Random.insideUnitCircle * spread * 0.4f), Vary(baseC),
                                  ArtPixel * Random.Range(2f, 3.5f), false);
            p.velocity = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1.2f) * spread * 2f;
            p.drag = 4f;
            p.heightVel = Random.Range(0.2f, 0.6f);
            p.life = Random.Range(0.4f, 0.7f);
            p.grow = 1.5f;
        }
    }

    /// <summary>Glowing sparkles that float upward (level ups, upgrades, magic). Visible in the dark.</summary>
    public static void Sparkle(Vector3 at, Color color, int count, float radius = 0.5f)
    {
        for (int i = 0; i < count; i++)
        {
            var p = Particle.Make(at + (Vector3)(Random.insideUnitCircle * radius), Vary(color),
                                  ArtPixel * Random.Range(1f, 2f), true);
            p.velocity = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.6f, 1.6f));
            p.drag = 1f;
            p.life = Random.Range(0.5f, 1f);
            p.twinkle = true;
        }
    }

    /// <summary>One fading dot left behind a flying shot.</summary>
    public static void Trail(Vector3 at, Color color, float size = 1.5f)
    {
        var p = Particle.Make(at + (Vector3)(Random.insideUnitCircle * 0.03f), color, ArtPixel * size, true);
        p.life = 0.25f;
        p.grow = -2f;
    }

    /// <summary>Shake the camera (0.1 = a nudge, 0.5 = a big boom).</summary>
    public static void Shake(float amount) => CameraFollow.AddShake(amount);

    /// <summary>A random solid color from a sprite's pixels.</summary>
    public static Color SampleColor(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null || !sprite.texture.isReadable) return Color.gray;
        var r = sprite.textureRect;
        for (int tries = 0; tries < 20; tries++)
        {
            Color c = sprite.texture.GetPixel((int)Random.Range(r.xMin, r.xMax), (int)Random.Range(r.yMin, r.yMax));
            // Skip see-through pixels and the near-black outline.
            if (c.a > 0.5f && c.r + c.g + c.b > 0.35f) return c;
        }
        return Color.gray;
    }

    static Color Vary(Color c)
    {
        float v = Random.Range(0.85f, 1.15f);
        return new Color(Mathf.Clamp01(c.r * v), Mathf.Clamp01(c.g * v), Mathf.Clamp01(c.b * v), c.a);
    }
}

/// <summary>One flying square. Has a pretend height so it can arc up off the ground and land.</summary>
public class Particle : MonoBehaviour
{
    public Vector2 velocity;
    public float height, heightVel, gravity, drag, life = 0.5f, grow;
    public bool twinkle;

    SpriteRenderer sr;
    Vector3 ground;
    float age, size;
    Color color;

    public static Particle Make(Vector3 at, Color color, float size, bool glow)
    {
        var sr = new GameObject("Particle").AddComponent<SpriteRenderer>();
        sr.transform.position = at;
        sr.sprite = SpriteFactory.Pixel();
        sr.color = color;
        sr.sortingOrder = 17;
        sr.transform.localScale = Vector3.one * size;
        if (glow) SpriteFactory.MakeUnlit(sr); // otherwise it's lit, so it hides in the dark like everything else
        var p = sr.gameObject.AddComponent<Particle>();
        p.sr = sr; p.ground = at; p.size = size; p.color = color;
        return p;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        if (age >= life) { Destroy(gameObject); return; }

        velocity *= Mathf.Exp(-drag * dt);
        ground += (Vector3)(velocity * dt);
        if (gravity > 0f)
        {
            heightVel -= gravity * dt;
            height += heightVel * dt;
            if (height < 0f) { height = 0f; heightVel *= -0.35f; velocity *= 0.5f; } // little bounce
        }
        else height += heightVel * dt;

        transform.position = ground + Vector3.up * height;
        float t = age / life;
        transform.localScale = Vector3.one * Mathf.Max(0.001f, size * (1f + grow * t));
        float a = color.a * (1f - t * t);
        if (twinkle) a *= 0.6f + 0.4f * Mathf.Sin(age * 30f);
        sr.color = new Color(color.r, color.g, color.b, a);
    }
}

/// <summary>
/// A copy of a sprite left behind to play a death animation, or a fading afterimage while dashing.
/// </summary>
public class Corpse : MonoBehaviour
{
    SpriteRenderer sr;
    float age, duration;
    bool crumble, afterimage;
    Vector3 startScale, startPos;
    Color startColor;

    static SpriteRenderer Copy(SpriteRenderer from, string name)
    {
        var sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.transform.position = from.transform.position;
        sr.transform.rotation = from.transform.rotation;
        sr.transform.localScale = from.transform.lossyScale;
        sr.sprite = from.sprite;
        sr.flipX = from.flipX;
        sr.sortingOrder = from.sortingOrder;
        sr.sharedMaterial = from.sharedMaterial;
        sr.color = from.color;
        return sr;
    }

    public static void From(SpriteRenderer body, bool crumble, float duration)
    {
        var sr = Copy(body, crumble ? "Rubble" : "Corpse");
        var c = sr.gameObject.AddComponent<Corpse>();
        c.sr = sr; c.crumble = crumble; c.duration = duration;
        c.startScale = sr.transform.localScale; c.startPos = sr.transform.position;
        c.startColor = new Color(sr.color.r, sr.color.g, sr.color.b, body.color.a);

        float size = body.bounds.extents.magnitude;
        Vector3 mid = body.bounds.center;
        if (crumble)
        {
            Juice.Dust(mid, 18, size);
            Juice.BurstFromSprite(mid, body.sprite, 16, 3f);
            Juice.Shake(0.25f);
        }
        else
        {
            Juice.BurstFromSprite(mid, body.sprite, Mathf.Clamp(Mathf.RoundToInt(size * 14f), 6, 30), 2.2f + size);
            Juice.Dust(new Vector3(mid.x, body.bounds.min.y), 4, size * 0.8f);
        }
    }

    public static void Afterimage(SpriteRenderer body)
    {
        var sr = Copy(body, "Afterimage");
        sr.sortingOrder = body.sortingOrder - 1;
        SpriteFactory.MakeUnlit(sr);
        var c = sr.gameObject.AddComponent<Corpse>();
        c.sr = sr; c.afterimage = true; c.duration = 0.2f;
        c.startScale = sr.transform.localScale; c.startPos = sr.transform.position;
        c.startColor = new Color(0.6f, 0.85f, 1f, 0.55f);
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = age / duration;
        if (t >= 1f) { Destroy(gameObject); return; }

        if (afterimage)
        {
            sr.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
            return;
        }

        if (crumble)
        {
            // Shake, sink into the ground and fade.
            float sink = t * t;
            transform.position = startPos + Vector3.down * sink * 0.5f + (Vector3)(Random.insideUnitCircle * 0.06f * (1f - t));
            transform.localScale = new Vector3(startScale.x * (1f + 0.1f * t), startScale.y * (1f - 0.6f * sink), 1f);
            sr.color = new Color(startColor.r * (1f - 0.4f * t), startColor.g * (1f - 0.4f * t), startColor.b * (1f - 0.4f * t), startColor.a * (1f - sink));
            if (Random.value < 0.25f) Juice.Dust(startPos + Vector3.down * 0.2f, 1, 0.6f);
            return;
        }

        // Creatures: a quick pop, then squash flat, flash and fade.
        float pop = t < 0.2f ? t / 0.2f : 1f;
        float flat = t < 0.2f ? 0f : (t - 0.2f) / 0.8f;
        float sx = 1f + 0.15f * Mathf.Sin(pop * Mathf.PI) + 0.35f * flat;
        float sy = 1f + 0.15f * Mathf.Sin(pop * Mathf.PI) - 0.8f * flat * flat;
        transform.localScale = new Vector3(startScale.x * sx, startScale.y * sy, 1f);
        float white = t < 0.2f ? 1f : 0f;
        Color c = Color.Lerp(startColor, new Color(1f, 0.6f, 0.6f, startColor.a), white);
        c.a = startColor.a * (1f - flat);
        sr.color = c;
    }
}

/// <summary>
/// Makes a glow layer (eyes, crystals, windows) shimmer: Pulse breathes softly, Blink shuts eyes now and then.
/// </summary>
public class GlowPulse : MonoBehaviour
{
    public enum Mode { Pulse, Blink }
    public Mode mode = Mode.Pulse;
    SpriteRenderer sr;
    float seed, blinkTimer, blinkLeft;

    /// <summary>Find the "Glow" child of a body and animate it.</summary>
    public static void On(SpriteRenderer body, Mode mode)
    {
        if (body == null) return;
        var glow = body.transform.Find("Glow");
        if (glow == null) return;
        var g = glow.GetComponent<GlowPulse>();
        if (g == null) g = glow.gameObject.AddComponent<GlowPulse>();
        g.mode = mode;
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        seed = Random.value * 10f;
        blinkTimer = Random.Range(1f, 4f);
    }

    void Update()
    {
        if (sr == null) return;
        Color c = sr.color;
        if (mode == Mode.Pulse)
            c.a = 0.7f + 0.3f * Mathf.Sin((Time.time + seed) * 2.4f);
        else
        {
            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0f) { blinkLeft = 0.12f; blinkTimer = Random.Range(1.5f, 5f); }
            blinkLeft -= Time.deltaTime;
            c.a = blinkLeft > 0f ? 0f : 1f;
        }
        sr.color = c;
    }
}

/// <summary>Simple looping motion for scenery: torches flicker, portals swirl, gates hum, ore glints.</summary>
public class Idle : MonoBehaviour
{
    public enum Kind { Flicker, Swirl, Hum, Glint }
    public Kind kind;
    Vector3 baseScale;
    float seed, glintTimer;

    public static Idle On(GameObject go, Kind kind)
    {
        var i = go.AddComponent<Idle>();
        i.kind = kind;
        return i;
    }

    void Start()
    {
        baseScale = transform.localScale;
        seed = Random.value * 10f;
        glintTimer = Random.Range(0.5f, 4f);
    }

    void Update()
    {
        float t = Time.time + seed;
        switch (kind)
        {
            case Kind.Flicker:
                transform.localScale = new Vector3(baseScale.x * (1f + 0.04f * Mathf.Sin(t * 13f)),
                                                   baseScale.y * (1f + 0.08f * Mathf.PerlinNoise(t * 6f, seed) - 0.04f), 1f);
                break;
            case Kind.Swirl:
                transform.localScale = baseScale * (1f + 0.07f * Mathf.Sin(t * 3f));
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.5f) * 8f);
                if (Random.value < 0.15f) Juice.Sparkle(transform.position, new Color(0.75f, 0.55f, 1f), 1, 0.7f);
                break;
            case Kind.Hum:
                transform.localScale = new Vector3(baseScale.x, baseScale.y * (1f + 0.015f * Mathf.Sin(t * 2f)), 1f);
                break;
            case Kind.Glint:
                // Ore nodes: every few seconds a sparkle catches the light.
                glintTimer -= Time.deltaTime;
                if (glintTimer <= 0f)
                {
                    glintTimer = Random.Range(2f, 5f);
                    var sr = GetComponent<SpriteRenderer>();
                    if (sr != null && FogOfWar.IsLit(transform.position))
                        Juice.Sparkle(transform.position + Vector3.up * 0.1f, Color.Lerp(Juice.SampleColor(sr.sprite), Color.white, 0.5f), 2, 0.3f);
                }
                break;
        }
    }
}
