using UnityEngine;

/// <summary>How a SpriteAnimator moves its sprite.</summary>
public enum AnimStyle
{
    Walker,   // hero, goblins, skeletons, imps: bouncy walk, gentle breathing when still
    Runner,   // wolves: fast gallop with a strong bob
    Heavy,    // golems, giants: slow stomping steps that squash on every footfall
    Hopper,   // slimes: always bouncing, squash on landing, stretch in the air
    Flyer,    // bats: hovering bob and fast wing-flap squash
    Floater,  // ghosts, liches: slow drifting float and sway
    Crawler,  // spiders: low, skittering jitter
    Building, // hub, gates: slow "breathing"
    Tower,    // towers: breathing + recoil when they fire
    Worker,   // miners: rhythmic hammering
}

/// <summary>
/// Procedural animation for one pixel-art sprite - no animation clips needed.
///
/// The sprite lives on a child object ("Body", see CreateBody) so this script can bob, squash,
/// tilt and shove it around without moving the real object (its position, collider and health bar stay put).
///
/// It animates automatically based on how fast the object is moving, and other scripts can call
/// one-shot reactions: Hit(), Lunge(), Recoil(), Pulse(), Hop(), Spin(), Windup(), Appear() and Die().
/// </summary>
public class SpriteAnimator : MonoBehaviour
{
    public AnimStyle style = AnimStyle.Walker;
    [Tooltip("Overall size of the motion (1 = normal).")]
    public float intensity = 1f;
    [Tooltip("True: this script also tints the sprite red when hit. False: the owner script handles color and reads Flash.")]
    public bool ownsColor;

    public SpriteRenderer Body { get; private set; }

    /// <summary>0..1, goes to 1 when hit and fades quickly. Scripts that set their own color blend toward red with it.</summary>
    public float Flash { get; private set; }

    /// <summary>Set by owners while stunned: the sprite wobbles dizzily.</summary>
    public bool Dizzy { get; set; }

    /// <summary>Set by owners while dashing: stretched and leaves afterimages.</summary>
    public bool Dashing { get; set; }

    /// <summary>True while a spawn-in animation is still playing.</summary>
    public bool Appearing => appearT < 1f;

    // ---- motion tracking ----
    Vector3 lastPos;
    float speedSmoothed, phase, seed, feetY;
    float facing = 1f; // +1 right, -1 left (from the sprite's flipX)

    // ---- springy one-shots (deviation from rest, plus velocity) ----
    Vector2 punchScale, punchScaleVel;
    Vector2 punchOffset, punchOffsetVel;
    float punchTilt, punchTiltVel;

    float shakeTimer, shakeStrength;
    float hopT = 1f, hopHeight;
    float spinT = 1f, spinDuration;
    float windupT, windupDuration;
    float appearT = 1f, appearDuration = 0.35f;
    bool appearDrop;
    float lastHitTime = -10f, afterimageTimer;

    /// <summary>Adds a child "Body" object with a SpriteRenderer for the animator to move. Returns the renderer.</summary>
    public static SpriteRenderer CreateBody(GameObject root)
    {
        var sr = new GameObject("Body").AddComponent<SpriteRenderer>();
        sr.transform.SetParent(root.transform, false);
        return sr;
    }

    /// <summary>Put an animator on `root` that moves `body` (which must be a child of root).</summary>
    public static SpriteAnimator Attach(GameObject root, SpriteRenderer body, AnimStyle style, float intensity = 1f)
    {
        var a = root.GetComponent<SpriteAnimator>();
        if (a == null) a = root.AddComponent<SpriteAnimator>();
        a.Body = body;
        a.style = style;
        a.intensity = intensity;
        a.ownsColor = style == AnimStyle.Building || style == AnimStyle.Tower || style == AnimStyle.Worker;
        a.seed = Random.value * 100f;
        a.lastPos = root.transform.position;
        a.RefreshFeet();
        return a;
    }

    /// <summary>Call after changing the body's sprite, so squashes stay anchored at its feet.</summary>
    public void RefreshFeet() => feetY = Body != null && Body.sprite != null ? Body.sprite.bounds.min.y : 0f;

    // =====================================================================
    //  One-shot reactions
    // =====================================================================

    /// <summary>Took damage: squash, shake and flash. Small, constant damage (biting, burning) only twitches now and then.</summary>
    public void OnDamaged(float amount, float maxHp)
    {
        bool small = amount < 1f;
        if (Time.time - lastHitTime < (small ? 0.3f : 0.08f)) return;
        lastHitTime = Time.time;
        float strength = small ? 0.35f : Mathf.Clamp(amount / Mathf.Max(1f, maxHp) * 8f, 0.5f, 1.4f);
        Hit(strength);
    }

    public void Hit(float strength = 1f)
    {
        punchScaleVel += new Vector2(3.5f, -3.5f) * strength;
        shakeTimer = 0.12f + 0.06f * strength;
        shakeStrength = 0.05f * strength;
        Flash = Mathf.Max(Flash, Mathf.Clamp01(0.5f + strength * 0.5f));
    }

    /// <summary>Attack: jab the sprite toward a world direction and snap back.</summary>
    public void Lunge(Vector2 worldDir, float distance = 0.18f)
    {
        if (worldDir.sqrMagnitude < 0.0001f) return;
        punchOffset = LocalDir(worldDir) * distance;
        punchOffsetVel = Vector2.zero;
        punchScaleVel += new Vector2(2f, -1f);
        punchTilt = -Mathf.Sign(worldDir.x == 0f ? facing : worldDir.x) * 10f;
    }

    /// <summary>Firing: kick back away from the shot and squash down.</summary>
    public void Recoil(Vector2 shotDir, float amount = 1f)
    {
        if (shotDir.sqrMagnitude > 0.0001f) punchOffset = -LocalDir(shotDir) * 0.08f * amount;
        punchScaleVel += new Vector2(2.5f, -3f) * amount;
    }

    /// <summary>A bulge outward (frost pulse, collecting ore, upgrades).</summary>
    public void Pulse(float amount = 1f) => punchScaleVel += new Vector2(3f, 3f) * amount;

    /// <summary>A little jump (level up, upgrade).</summary>
    public void Hop(float height = 0.3f) { hopT = 0f; hopHeight = height; }

    /// <summary>A full spin (whirlwind).</summary>
    public void Spin(float duration = 0.3f) { spinT = 0f; spinDuration = duration; }

    /// <summary>Crouch and tremble for a while, then release with a big stretch (boss slam).</summary>
    public void Windup(float seconds) { windupT = 0f; windupDuration = seconds; }

    /// <summary>Spawn in: pop up from nothing, or (drop = true) fall into place with a thud and dust.</summary>
    public void Appear(bool drop = false, float duration = 0.35f)
    {
        appearT = 0f; appearDuration = duration; appearDrop = drop;
        LateUpdate(); // apply the first frame right away so there's no flicker at full size
    }

    /// <summary>
    /// Leave behind a death animation (the real object is usually destroyed right after).
    /// Creatures squash flat and burst into bits; buildings crumble.
    /// </summary>
    public void Die(bool crumble = false, float duration = -1f)
    {
        if (Body == null) return;
        if (duration < 0f) duration = crumble ? 0.9f : 0.45f;
        Corpse.From(Body, crumble, duration);
    }

    // =====================================================================
    //  Per-frame animation
    // =====================================================================

    void LateUpdate()
    {
        if (Body == null) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // paused

        // How fast are we going? (Works whether the object is moved by physics or by script.)
        Vector3 pos = transform.position;
        float speed = ((Vector2)(pos - lastPos)).magnitude / dt;
        lastPos = pos;
        if (speed > 30f) speed = 0f; // teleported
        speedSmoothed = Mathf.Lerp(speedSmoothed, speed, 1f - Mathf.Exp(-12f * dt));
        float moving = Mathf.Clamp01(speedSmoothed / 1.2f);
        facing = Body.flipX ? -1f : 1f;

        float t = Time.time + seed;
        float k = intensity;
        Vector2 offset = Vector2.zero;
        Vector2 scale = Vector2.one;
        float tilt = 0f;

        // ---- the style's looping motion ----
        switch (style)
        {
            case AnimStyle.Walker:
            case AnimStyle.Runner:
            {
                bool run = style == AnimStyle.Runner;
                phase += dt * Mathf.Lerp(0f, run ? 16f : 12f, moving) * Mathf.Clamp(speedSmoothed / 3f, 0.6f, 1.6f);
                float step = Mathf.Abs(Mathf.Sin(phase));
                offset.y = step * (run ? 0.09f : 0.06f) * moving;
                tilt = Mathf.Sin(phase) * (run ? 4f : 7f) * moving + (run ? -6f * moving * facing : 0f);
                float breathe = Mathf.Sin(t * 2.6f) * (1f - moving);
                scale = new Vector2(1f - 0.02f * breathe, 1f + 0.035f * breathe);
                scale.y *= 1f + 0.05f * (step - 0.5f) * moving;
                break;
            }
            case AnimStyle.Heavy:
            {
                phase += dt * 7f * moving;
                float step = Mathf.Abs(Mathf.Sin(phase));
                offset.y = step * 0.05f * moving;
                tilt = Mathf.Sin(phase) * 3.5f * moving;
                float land = Mathf.Pow(1f - step, 6f) * moving; // the instant a foot hits the ground
                float breathe = Mathf.Sin(t * 1.6f) * (1f - moving);
                scale = new Vector2(1f + 0.08f * land - 0.015f * breathe, 1f - 0.09f * land + 0.03f * breathe);
                break;
            }
            case AnimStyle.Hopper:
            {
                phase += dt * Mathf.Lerp(5f, 9f, moving);
                float h = Mathf.Abs(Mathf.Sin(phase));
                offset.y = h * Mathf.Lerp(0.04f, 0.16f, moving);
                float squash = Mathf.Pow(1f - h, 5f);
                scale = new Vector2(1f + 0.18f * squash - 0.06f * h, 1f - 0.2f * squash + 0.1f * h);
                break;
            }
            case AnimStyle.Flyer:
            {
                float flap = Mathf.Sin(t * 20f);
                offset.y = 0.12f + Mathf.Sin(t * 5f) * 0.08f;
                scale = new Vector2(1f + 0.14f * flap, 1f - 0.07f * flap);
                tilt = -8f * moving * facing + Mathf.Sin(t * 3f) * 4f;
                break;
            }
            case AnimStyle.Floater:
            {
                offset.y = 0.06f + Mathf.Sin(t * 2.1f) * 0.08f;
                tilt = Mathf.Sin(t * 1.3f) * 4f - 5f * moving * facing;
                float b = Mathf.Sin(t * 2.1f + 1f);
                scale = new Vector2(1f + 0.03f * b, 1f - 0.03f * b);
                break;
            }
            case AnimStyle.Crawler:
            {
                phase += dt * 26f * moving;
                float jitter = Mathf.Sin(phase);
                offset.y = Mathf.Abs(jitter) * 0.025f * moving;
                tilt = jitter * 3f * moving;
                float breathe = Mathf.Sin(t * 3f) * (1f - moving);
                scale = new Vector2(1f + 0.04f * jitter * moving + 0.02f * breathe, 1f - 0.04f * jitter * moving - 0.02f * breathe);
                break;
            }
            case AnimStyle.Building:
            case AnimStyle.Tower:
            {
                float b = Mathf.Sin(t * 1.5f);
                scale = new Vector2(1f - 0.008f * b, 1f + 0.014f * b);
                break;
            }
            case AnimStyle.Worker:
            {
                // A hammer rhythm: a quick squash, then a slow rise.
                float beat = (t * 1.6f) % 1f;
                float hit = beat < 0.12f ? 1f - beat / 0.12f : 0f;
                scale = new Vector2(1f + 0.05f * hit, 1f - 0.06f * hit + 0.02f * Mathf.Sin(beat * Mathf.PI));
                break;
            }
        }

        // ---- status: dizzy / dash ----
        if (Dizzy) tilt += Mathf.Sin(t * 18f) * 9f;
        if (Dashing)
        {
            scale = new Vector2(scale.x * 1.25f, scale.y * 0.82f);
            tilt -= 10f * facing;
            afterimageTimer -= dt;
            if (afterimageTimer <= 0f) { afterimageTimer = 0.03f; Corpse.Afterimage(Body); }
        }

        // ---- springs settle the one-shot reactions ----
        Spring(ref punchScale, ref punchScaleVel, 320f, 14f, dt);
        Spring(ref punchOffset, ref punchOffsetVel, 260f, 20f, dt);
        punchTiltVel += (-260f * punchTilt - 18f * punchTiltVel) * dt;
        punchTilt += punchTiltVel * dt;

        scale += punchScale;
        offset += punchOffset;
        tilt += punchTilt;

        if (shakeTimer > 0f)
        {
            shakeTimer -= dt;
            offset += Random.insideUnitCircle * shakeStrength;
        }

        if (hopT < 1f)
        {
            hopT = Mathf.Min(1f, hopT + dt / 0.35f);
            offset.y += Mathf.Sin(hopT * Mathf.PI) * hopHeight;
            if (hopT >= 1f) punchScaleVel += new Vector2(2.5f, -2.5f); // land
        }

        if (spinT < 1f)
        {
            spinT = Mathf.Min(1f, spinT + dt / spinDuration);
            tilt += -facing * 360f * Mathf.SmoothStep(0f, 1f, spinT);
        }

        if (windupDuration > 0f)
        {
            windupT += dt;
            if (windupT < windupDuration)
            {
                float w = Mathf.Clamp01(windupT / windupDuration);
                scale = new Vector2(scale.x * (1f + 0.15f * w), scale.y * (1f - 0.2f * w));
                offset += Random.insideUnitCircle * 0.03f * w;
            }
            else
            {
                windupDuration = 0f;
                punchScaleVel += new Vector2(-4f, 6f); // spring up and SLAM
                hopT = 0f; hopHeight = 0.25f;
            }
        }

        if (appearT < 1f)
        {
            appearT = Mathf.Min(1f, appearT + dt / appearDuration);
            if (appearDrop)
            {
                // Fall in from above, accelerating, then thud.
                float fall = 1f - appearT * appearT;
                offset.y += fall * 1.4f;
                scale = new Vector2(scale.x * 0.9f, scale.y * 1.1f);
                if (appearT >= 1f)
                {
                    punchScaleVel += new Vector2(4f, -4f);
                    Juice.Dust(transform.position + Vector3.up * feetY * transform.lossyScale.y, 10, 0.6f);
                }
            }
            else
            {
                scale *= EaseOutBack(appearT);
            }
        }

        // ---- apply, keeping the feet planted while squashing and tilting ----
        var rot = Quaternion.Euler(0f, 0f, tilt * k);
        Vector2 s = Vector2.LerpUnclamped(Vector2.one, scale, k);
        Vector3 feet = new Vector3(0f, feetY, 0f);
        Vector3 scaledFeet = new Vector3(0f, feetY * s.y, 0f);
        Vector3 pivotFix = feet - rot * scaledFeet;

        Body.transform.localScale = new Vector3(s.x, s.y, 1f);
        Body.transform.localRotation = rot;
        Body.transform.localPosition = (Vector3)(offset * k) + pivotFix;

        // ---- hit flash ----
        if (Flash > 0f) Flash = Mathf.Max(0f, Flash - dt * 6f);
        if (ownsColor) Body.color = Color.Lerp(Color.white, new Color(1f, 0.4f, 0.4f), Flash);
    }

    /// <summary>World direction to a direction in the body's local space (the root may be scaled).</summary>
    Vector2 LocalDir(Vector2 worldDir)
    {
        Vector3 ls = transform.lossyScale;
        return new Vector2(worldDir.x / Mathf.Max(0.01f, ls.x), worldDir.y / Mathf.Max(0.01f, ls.y)).normalized;
    }

    static void Spring(ref Vector2 x, ref Vector2 v, float stiffness, float damping, float dt)
    {
        dt = Mathf.Min(dt, 0.033f); // stay stable on slow frames
        v += (-stiffness * x - damping * v) * dt;
        x += v * dt;
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.9f, c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }
}
