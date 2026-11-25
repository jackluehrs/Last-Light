using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Turns the game into a foggy night when you press Play:
///   - with Fog Of War on (default), turns the scene's Global Light 2D off: anything not lit is pitch black
///   - otherwise dims it to a dark blue-gray
///   - gives the hub, towers and miners their own lights so your base is visible
///   - adds slow-drifting mist that glows where light hits it
/// The player's torch lives on the player (see PlayerController).
/// The editor stays bright so the map is easy to work on.
/// </summary>
public class NightLighting : MonoBehaviour
{
    [Header("Fog of war")]
    [Tooltip("On: the world is pitch black, you only see what a light shines on. Off: dim night you can still see through.")]
    public bool fogOfWar = true;

    [Header("Darkness (used when fog of war is off)")]
    public Color ambientColor = new Color(0.45f, 0.55f, 0.8f);
    [Range(0f, 1f)] public float ambientIntensity = 0.12f;

    [Header("Torch")]
    public float torchRadius = 5.5f;
    public Color torchColor = new Color(1f, 0.72f, 0.4f);
    public float torchIntensity = 1.2f;

    [Header("Building lights")]
    public float hubLightRadius = 7f;
    public float towerLightRadius = 3.5f;
    public float minerLightRadius = 2f;

    [Header("Mist")]
    [Range(0f, 1f)] public float mistOpacity = 0.35f;
    public float mistDriftSpeed = 0.05f;

    Transform[] mistLayers;

    /// <summary>Called once by the GameManager after the map exists.</summary>
    public void Setup()
    {
        if (mistLayers != null) return;
        DimGlobalLight();
        CreateMist();
    }

    void DimGlobalLight()
    {
        var found = new System.Collections.Generic.List<Light2D>();
        foreach (var light in FindObjectsByType<Light2D>())
        {
            if (light.lightType != Light2D.LightType.Global) continue;
            light.color = ambientColor;
            light.intensity = fogOfWar ? 0f : ambientIntensity;
            found.Add(light);
        }
        globalLights = found.ToArray();
    }

    /// <summary>Makes a soft round point light as a child of the given object.</summary>
    public static Light2D AddPointLight(Transform parent, Vector3 localPos, float radius, Color color, float intensity)
    {
        var go = new GameObject("Light");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.25f;
        light.color = color;
        light.intensity = intensity;
        light.falloffIntensity = 0.6f;
        FogOfWar.Register(light);
        return light;
    }

    /// <summary>Called by the GameManager whenever a building is made.</summary>
    public void AddBuildingLight(Building b)
    {
        switch (b.Type)
        {
            case BuildType.Hub:
                AddPointLight(b.transform, Vector3.zero, hubLightRadius, new Color(1f, 0.85f, 0.6f), 1.1f)
                    .gameObject.AddComponent<LightFlicker>();
                break;
            case BuildType.Miner:
                AddPointLight(b.transform, Vector3.zero, minerLightRadius, new Color(1f, 0.85f, 0.4f), 0.7f);
                break;
            default:
                // Towers: a light the same size as their range, so you can see what they can hit.
                var def = BuildingDef.Get(b.Type);
                Color c = b.Type switch
                {
                    BuildType.Cannon => new Color(1f, 0.8f, 0.55f),
                    BuildType.Frost => new Color(0.6f, 0.9f, 1f),
                    BuildType.Storm => new Color(0.75f, 0.55f, 1f),
                    BuildType.Inferno => new Color(1f, 0.5f, 0.2f),
                    _ => new Color(0.55f, 0.85f, 1f),
                };
                var light = AddPointLight(b.transform, Vector3.up * (b.Type == BuildType.Cannon ? 0.4f : 0.9f), def.Range, c, 0.8f);
                if (b.Type == BuildType.Inferno) light.gameObject.AddComponent<LightFlicker>();
                break;
        }
    }

    /// <summary>Hub level 5: the Hub blazes like a small sun, lighting up the whole Meadow.</summary>
    public void SunBeacon(Building hub)
    {
        var light = hub.GetComponentInChildren<Light2D>();
        if (light != null)
        {
            var flicker = light.GetComponent<LightFlicker>();
            if (flicker != null) flicker.SetBaseRadius(hubLightRadius * 2.2f);
            else light.pointLightOuterRadius = hubLightRadius * 2.2f;
            light.color = new Color(1f, 0.9f, 0.65f);
        }
    }

    // ---------- Each land has its own mood ----------

    [Header("Mood of each land (darkness colour and brightness)")]
    public Color darkForestAmbient = new Color(0.3f, 0.5f, 0.45f);
    [Range(0f, 1f)] public float darkForestIntensity = 0.07f;
    public Color bloodmoorAmbient = new Color(0.85f, 0.3f, 0.4f);
    [Range(0f, 1f)] public float bloodmoorIntensity = 0.13f;
    public Color cryptAmbient = new Color(0.4f, 0.75f, 0.65f);
    [Range(0f, 1f)] public float cryptIntensity = 0.1f;
    public Color ashlandsAmbient = new Color(0.95f, 0.45f, 0.25f);
    [Range(0f, 1f)] public float ashlandsIntensity = 0.14f;
    [Tooltip("Dungeons are this much darker than their land outside.")]
    [Range(0f, 1f)] public float dungeonDarkness = 0.5f;

    Light2D[] globalLights;

    /// <summary>Slowly shifts the darkness colour to match where the hero is.</summary>
    void UpdateMood()
    {
        var gm = GameManager.Instance;
        if (globalLights == null || gm == null || gm.Player == null) return;
        var biome = gm.PlayerBiome;
        (Color col, float inten) = biome switch
        {
            Biome.DarkForest => (darkForestAmbient, darkForestIntensity),
            Biome.Bloodmoor => (bloodmoorAmbient, bloodmoorIntensity),
            Biome.Crypt => (cryptAmbient, cryptIntensity),
            Biome.Ashlands => (ashlandsAmbient, ashlandsIntensity),
            _ => (ambientColor, ambientIntensity),
        };
        if (gm.Dungeons.Current != null) inten *= dungeonDarkness;
        if (fogOfWar) inten = 0f; // total darkness: only lights let you see
        float k = 1f - Mathf.Exp(-Time.deltaTime * 1.5f);
        foreach (var l in globalLights)
        {
            if (l == null) continue;
            l.color = Color.Lerp(l.color, col, k);
            l.intensity = Mathf.Lerp(l.intensity, inten, k);
        }
    }

    void CreateMist()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Map == null) return;
        Rect b = gm.Map.WorldBounds;

        mistLayers = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            var sr = new GameObject("Mist " + (i + 1)).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            sr.sprite = SpriteFactory.Mist(i);
            sr.sortingOrder = 25; // above the world, below the build preview
            sr.color = new Color(1f, 1f, 1f, mistOpacity);
            // Bigger than the map so the edges never show while drifting.
            float w = b.width + 30f, h = b.height + 30f;
            sr.transform.localScale = new Vector3(w / sr.sprite.bounds.size.x, h / sr.sprite.bounds.size.y, 1f);
            sr.transform.position = new Vector3(b.center.x, b.center.y, 0f);
            mistLayers[i] = sr.transform;
        }
    }

    void Update()
    {
        UpdateMood();
        if (GameManager.Instance != null) FogOfWar.Tick(GameManager.Instance.Map);
        if (mistLayers == null || GameManager.Instance == null || GameManager.Instance.Map == null) return;
        Vector2 c = GameManager.Instance.Map.WorldBounds.center;
        float t = Time.time * mistDriftSpeed;
        // Two layers sway in different directions so the fog seems to roll.
        mistLayers[0].position = new Vector3(c.x + Mathf.Sin(t) * 6f, c.y + Mathf.Cos(t * 0.7f) * 3f, 0f);
        mistLayers[1].position = new Vector3(c.x - Mathf.Sin(t * 0.8f + 1f) * 6f, c.y + Mathf.Sin(t * 0.6f) * 3f, 0f);
    }
}

/// <summary>Makes a light flicker like a flame.</summary>
[RequireComponent(typeof(Light2D))]
public class LightFlicker : MonoBehaviour
{
    public float amount = 0.15f;
    public float speed = 7f;

    Light2D light2D;
    float baseIntensity, baseRadius, seed;

    void Start()
    {
        light2D = GetComponent<Light2D>();
        baseIntensity = light2D.intensity;
        baseRadius = light2D.pointLightOuterRadius;
        seed = Random.value * 100f;
    }

    /// <summary>Change the light's normal size (flicker happens around it).</summary>
    public void SetBaseRadius(float radius)
    {
        if (light2D == null) Start();
        baseRadius = radius;
        light2D.pointLightOuterRadius = radius; // so a late Start() picks up the new size too
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f; // -1..1
        light2D.intensity = baseIntensity * (1f + n * amount);
        light2D.pointLightOuterRadius = baseRadius * (1f + n * amount * 0.4f);
    }
}
