using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns the pixel art from PixelArt.cs into Unity sprites, so the game needs no art assets.
/// Every sprite is cached, so asking for the same one twice is free.
/// </summary>
public static class SpriteFactory
{
    const int PPU = 16; // pixels per world unit, same as the map tiles
    static readonly Dictionary<string, Sprite> cache = new();
    static readonly Color Clear = new Color(0, 0, 0, 0);

    static Sprite Make(string key, int w, int h, Func<int, int, Color> pixel, Vector2? pivot = null, int ppu = PPU)
    {
        if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = key
        };
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, pixel(x, y));
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), pivot ?? new Vector2(0.5f, 0.5f), ppu);
        sprite.name = key;
        cache[key] = sprite;
        return sprite;
    }

    static Material unlit;

    /// <summary>
    /// A material that ignores lighting, for things that should glow or stay readable in the dark
    /// (flames, enemy eyes, health bars, the build preview).
    /// </summary>
    public static Material Unlit
    {
        get
        {
            if (unlit == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader != null) unlit = new Material(shader) { name = "Sprite Unlit (generated)" };
            }
            return unlit;
        }
    }

    /// <summary>Use the unlit material on a renderer (does nothing if the shader can't be found).</summary>
    public static void MakeUnlit(Renderer r)
    {
        if (Unlit != null) r.sharedMaterial = Unlit;
    }

    static float Dist(int x, int y, float cx, float cy) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

    /// <summary>A 1x1 white pixel that is exactly 1 world unit big. Tint and scale it for bars.</summary>
    public static Sprite Pixel(bool pivotLeft = false) =>
        Make(pivotLeft ? "PixelLeft" : "Pixel", 1, 1, (x, y) => Color.white,
             pivotLeft ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f), 1);

    /// <summary>A white ring 1 world unit across. Scale it to show a tower's range.</summary>
    public static Sprite Ring() => Make("Ring", 128, 128, (x, y) =>
    {
        float d = Dist(x, y, 63.5f, 63.5f);
        if (d > 63.5f) return Clear;
        if (d > 61.5f) return new Color(1, 1, 1, 0.9f);
        return new Color(1, 1, 1, 0.08f);
    }, null, 128);

    /// <summary>A filled white circle. Tint it for enemies, bullets, etc.</summary>
    public static Sprite Circle(int pixels) => Make("Circle" + pixels, pixels, pixels, (x, y) =>
    {
        float r = pixels / 2f;
        float d = Dist(x, y, r - 0.5f, r - 0.5f);
        if (d > r - 0.2f) return Clear;
        if (d > r - 1.2f) return new Color(0.75f, 0.75f, 0.75f, 1); // darker outline (gets tinted)
        return Color.white;
    });

    // ---------- Pixel art (drawn in PixelArt.cs) ----------

    static readonly Dictionary<string, Sprite> glowCache = new();

    /// <summary>Turn a PixelCanvas into a texture. glowOnly keeps just the glowing pixels.</summary>
    public static Texture2D ToTexture(PixelCanvas c, bool glowOnly = false)
    {
        var tex = new Texture2D(c.W, c.H, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        var pixels = new Color32[c.W * c.H];
        for (int i = 0; i < pixels.Length; i++)
        {
            var p = c.Data[i];
            pixels[i] = glowOnly && !c.Glow[i] ? new Color32(0, 0, 0, 0) : new Color32(p.R, p.G, p.B, p.A);
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    static Sprite FromCanvas(string key, Func<PixelCanvas> draw)
    {
        if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
        var canvas = draw();
        var pivot = new Vector2(canvas.PivotX, canvas.PivotY);

        var tex = ToTexture(canvas);
        tex.name = key;
        var sprite = Sprite.Create(tex, new Rect(0, 0, canvas.W, canvas.H), pivot, PPU);
        sprite.name = key;
        cache[key] = sprite;

        if (canvas.HasGlow)
        {
            var glowTex = ToTexture(canvas, glowOnly: true);
            glowTex.name = key + " Glow";
            var glow = Sprite.Create(glowTex, new Rect(0, 0, canvas.W, canvas.H), pivot, PPU);
            glow.name = key + " Glow";
            glowCache[key] = glow;
        }
        return sprite;
    }

    /// <summary>The glowing parts of a pixel-art sprite (eyes, crystals, windows), or null if it has none.</summary>
    public static Sprite GlowOf(Sprite s) => s != null && glowCache.TryGetValue(s.name, out var g) ? g : null;

    /// <summary>
    /// Adds (or updates) an unlit child that draws the sprite's glowing pixels on top,
    /// so they shine in the dark. Returns the child renderer (or null if nothing glows).
    /// </summary>
    public static SpriteRenderer AddGlow(SpriteRenderer body)
    {
        var existing = body.transform.Find("Glow");
        var glowSprite = GlowOf(body.sprite);
        if (glowSprite == null)
        {
            if (existing != null) existing.gameObject.SetActive(false);
            return null;
        }
        SpriteRenderer sr = existing != null ? existing.GetComponent<SpriteRenderer>()
                                             : new GameObject("Glow").AddComponent<SpriteRenderer>();
        sr.gameObject.SetActive(true);
        sr.transform.SetParent(body.transform, false);
        sr.sprite = glowSprite;
        sr.sortingOrder = body.sortingOrder + 1;
        sr.flipX = body.flipX;
        MakeUnlit(sr);
        return sr;
    }

    /// <summary>The hero's look for their class.</summary>
    public static Sprite Hero(HeroClass c) => c switch
    {
        HeroClass.Knight => FromCanvas("Hero Knight", PixelArt.Knight),
        HeroClass.Ranger => FromCanvas("Hero Ranger", PixelArt.Archer),
        HeroClass.Pyromancer => FromCanvas("Hero Pyromancer", PixelArt.Wizard),
        _ => FromCanvas("Hero", PixelArt.Adventurer)
    };

    public static Sprite Enemy(EnemyKind k) => k switch
    {
        EnemyKind.Runner => FromCanvas("Goblin Scout", PixelArt.GoblinScout),
        EnemyKind.Brute => FromCanvas("Golem", PixelArt.Golem),
        EnemyKind.Wolf => FromCanvas("Wolf", PixelArt.Wolf),
        EnemyKind.Spider => FromCanvas("Spider", PixelArt.Spider),
        EnemyKind.Bat => FromCanvas("Bat", PixelArt.Bat),
        EnemyKind.Vampire => FromCanvas("Vampire", PixelArt.Vampire),
        EnemyKind.Skeleton => FromCanvas("Skeleton", PixelArt.Skeleton),
        EnemyKind.Ghost => FromCanvas("Ghost", PixelArt.Ghost),
        EnemyKind.Imp => FromCanvas("Imp", PixelArt.Imp),
        EnemyKind.MagmaSlime => FromCanvas("Magma Slime", PixelArt.MagmaSlime),
        EnemyKind.SpiderQueen => FromCanvas("Spider Queen", PixelArt.SpiderQueen),
        EnemyKind.VampireLord => FromCanvas("Vampire Lord", PixelArt.VampireLord),
        EnemyKind.Lich => FromCanvas("Lich", PixelArt.Lich),
        EnemyKind.RockGiant => FromCanvas("Rock Giant", PixelArt.RockGiant),
        _ => FromCanvas("Goblin", PixelArt.Goblin)
    };

    public static Sprite OreNode(OreType t) => FromCanvas("Ore " + t, () => PixelArt.OreNode(t));
    public static Sprite OreIcon(OreType t) => FromCanvas("Ore Icon " + t, () => PixelArt.OreIcon(t));
    public static Sprite EnemyShot(int kind) => FromCanvas("Enemy Shot " + kind, () => PixelArt.EnemyShot(kind));
    public static Sprite Cannonball() => FromCanvas("Cannonball", PixelArt.Cannonball);
    public static Sprite DungeonEntrance(Biome b) => FromCanvas("Dungeon Entrance " + b, () => PixelArt.DungeonEntrance(b));
    public static Sprite WallTorch() => FromCanvas("Wall Torch", PixelArt.WallTorch);
    public static Sprite StairsUp() => FromCanvas("Stairs Up", PixelArt.StairsUp);
    public static Sprite Portal() => FromCanvas("Portal", PixelArt.Portal);
    public static Sprite SealGate(Biome b) => FromCanvas("Seal Gate " + b, () => PixelArt.SealGate(b));
    public static Sprite Artifact(Biome opens) => FromCanvas("Artifact " + opens, () => PixelArt.Artifact(opens));
    public static Sprite IconLock() => FromCanvas("Icon Lock", PixelArt.IconLock);
    public static Sprite IconSwords() => FromCanvas("Icon Swords", PixelArt.IconSwords);

    /// <summary>The sword swing arc, pointing right.</summary>
    public static Sprite Slash() => FromCanvas("Slash", PixelArt.Slash);

    /// <summary>A torch: wooden stick with a flame on top.</summary>
    public static Sprite Torch() => FromCanvas("Torch", PixelArt.Torch);

    /// <summary>A Ranger arrow pointing right (rotate it to aim).</summary>
    public static Sprite ArrowSprite() => FromCanvas("Arrow", PixelArt.Arrow);

    /// <summary>A tower's crystal shot.</summary>
    public static Sprite Bolt() => FromCanvas("Bolt", PixelArt.Bolt);

    public static Sprite Fireball() => FromCanvas("Fireball", PixelArt.Fireball);

    public static Sprite LevelPip() => FromCanvas("Level Pip", PixelArt.LevelPip);

    // HUD icons
    public static Sprite IconCoin() => FromCanvas("Icon Coin", PixelArt.IconCoin);
    public static Sprite IconHeart() => FromCanvas("Icon Heart", PixelArt.IconHeart);
    public static Sprite IconSkull() => FromCanvas("Icon Skull", PixelArt.IconSkull);
    public static Sprite IconHourglass() => FromCanvas("Icon Hourglass", PixelArt.IconHourglass);
    public static Sprite IconStar() => FromCanvas("Icon Star", PixelArt.IconStar);

    /// <summary>Soft cloudy noise for the drifting mist. Two variants so layers don't line up.</summary>
    public static Sprite Mist(int variant)
    {
        string key = "Mist" + variant;
        if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

        const int w = 160, h = 100;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear, // smooth, not pixelated
            wrapMode = TextureWrapMode.Clamp,
            name = key
        };
        float ox = variant * 57.3f, oy = variant * 91.7f;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                float n = Mathf.PerlinNoise(ox + x * 0.035f, oy + y * 0.035f) * 0.65f
                        + Mathf.PerlinNoise(ox + x * 0.09f, oy + y * 0.09f) * 0.35f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.75f, n));
                tex.SetPixel(x, y, new Color(0.85f, 0.9f, 1f, a));
            }
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1);
        sprite.name = key;
        cache[key] = sprite;
        return sprite;
    }

    // ---------- Buildings ----------

    public static Sprite Hub() => FromCanvas("Hub", PixelArt.Hub);
    public static Sprite Tower() => FromCanvas("Tower", PixelArt.Tower);
    public static Sprite Miner() => FromCanvas("Miner", PixelArt.Miner);
    public static Sprite Ore() => FromCanvas("Ore", PixelArt.Ore);

    public static Sprite ForBuilding(BuildType type) => type switch
    {
        BuildType.Hub => Hub(),
        BuildType.Tower => Tower(),
        BuildType.Miner => Miner(),
        BuildType.Cannon => FromCanvas("Cannon", PixelArt.Cannon),
        BuildType.Frost => FromCanvas("Frost Spire", PixelArt.FrostSpire),
        BuildType.Storm => FromCanvas("Storm Spire", PixelArt.StormSpire),
        BuildType.Inferno => FromCanvas("Inferno", PixelArt.Inferno),
        _ => Pixel()
    };
}
