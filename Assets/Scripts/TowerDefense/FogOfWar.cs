using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Fog of war:
///   - knows every light in the game, so it can answer "is this spot lit?"
///   - remembers which map tiles have ever been lit (the minimap only shows those)
/// The darkness itself comes from NightLighting turning the world's ambient light off.
/// </summary>
public static class FogOfWar
{
    static readonly List<Light2D> lights = new();

    /// <summary>Lights only count as "lighting a spot" inside this part of their radius (the edge is too dim to see).</summary>
    const float VisibleFraction = 0.8f;

    /// <summary>NightLighting.AddPointLight calls this for every light it makes.</summary>
    public static void Register(Light2D light) { if (light != null) lights.Add(light); }

    /// <summary>True if any light is shining on this world position.</summary>
    public static bool IsLit(Vector3 pos)
    {
        for (int i = lights.Count - 1; i >= 0; i--)
        {
            var l = lights[i];
            if (l == null) { lights.RemoveAt(i); continue; }
            if (!l.isActiveAndEnabled || l.intensity <= 0.05f) continue;
            float r = l.pointLightOuterRadius * VisibleFraction;
            if (((Vector2)(l.transform.position - pos)).sqrMagnitude <= r * r) return true;
        }
        return false;
    }

    // ---------- Explored tiles (for the minimap) ----------

    static bool[] explored;
    static int mapW, mapH;
    static float revealTimer;

    /// <summary>Changes whenever new tiles get explored, so the minimap knows to redraw.</summary>
    public static int Version { get; private set; }

    public static void ResetExplored(MapGenerator map)
    {
        mapW = map.width; mapH = map.height;
        explored = new bool[mapW * mapH];
        Version++;
    }

    public static bool IsExplored(int x, int y) =>
        explored != null && x >= 0 && y >= 0 && x < mapW && y < mapH && explored[x + y * mapW];

    public static bool IsExplored(MapGenerator map, Vector3 world)
    {
        var c = map.WorldToCell(world);
        return IsExplored(c.x, c.y);
    }

    /// <summary>Called every frame by NightLighting: marks tiles under any light as explored (a few times a second).</summary>
    public static void Tick(MapGenerator map)
    {
        if (explored == null || map == null) return;
        revealTimer -= Time.deltaTime;
        if (revealTimer > 0f) return;
        revealTimer = 0.2f;

        bool changed = false;
        foreach (var l in lights)
        {
            if (l == null || !l.isActiveAndEnabled) continue;
            float r = l.pointLightOuterRadius * VisibleFraction;
            if (r < 1.5f) continue; // tiny lights (bullets etc.) don't map anything
            var c = map.WorldToCell(l.transform.position);
            int ri = Mathf.CeilToInt(r);
            for (int dx = -ri; dx <= ri; dx++)
                for (int dy = -ri; dy <= ri; dy++)
                {
                    int x = c.x + dx, y = c.y + dy;
                    if (x < 0 || y < 0 || x >= mapW || y >= mapH) continue;
                    if (dx * dx + dy * dy > r * r) continue;
                    int i = x + y * mapW;
                    if (explored[i]) continue;
                    explored[i] = true;
                    changed = true;
                }
        }
        if (changed) Version++;
    }
}

/// <summary>
/// Put on enemies: hides their glowing eyes and health bar unless they're standing in light,
/// so in the dark you truly can't see them coming.
/// </summary>
public class FogHidden : MonoBehaviour
{
    SpriteRenderer body;
    SpriteRenderer[] extras;
    float refreshTimer;

    public bool Visible { get; private set; } = true;

    void Start()
    {
        // The sprite usually lives on an animated "Body" child (see SpriteAnimator).
        var b = transform.Find("Body");
        body = b != null ? b.GetComponent<SpriteRenderer>() : GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        // Re-collect now and then, because the health bar and glow can be created after us.
        refreshTimer -= Time.deltaTime;
        if (extras == null || refreshTimer <= 0f)
        {
            extras = GetComponentsInChildren<SpriteRenderer>(true);
            refreshTimer = 1f;
        }

        Visible = FogOfWar.IsLit(transform.position);
        foreach (var sr in extras)
            if (sr != null && sr != body) sr.enabled = Visible;
    }
}
