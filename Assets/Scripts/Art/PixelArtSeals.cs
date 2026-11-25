using System;

/// <summary>The magic seals that close off the wild lands, and the artifacts that open them.</summary>
public static partial class PixelArt
{
    /// <summary>Colour of each land's seal magic.</summary>
    public static string SealColorHex(Biome b) => b switch
    {
        Biome.Crypt => "6cf0b0",     // ghostly green
        Biome.Bloodmoor => "ff5a6e", // blood red
        Biome.Ashlands => "ffa040",  // ember orange
        _ => "b07aff",
    };

    /// <summary>One tile of the shimmering rune wall along a sealed land's border (all of it glows).</summary>
    public static PixelCanvas SealWall(Biome b, int variant)
    {
        var c = new PixelCanvas(16, 16);
        var main = Px.Hex(SealColorHex(b));
        var faint = main.WithAlpha(70);
        var mid = main.WithAlpha(140);
        var rnd = new Random(variant * 31 + (int)b);
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
            {
                // Soft vertical streaks, like a curtain of light.
                double wave = Math.Sin(x * 0.9 + variant * 1.7) * 0.5 + 0.5;
                if (wave > 0.55 || rnd.NextDouble() < 0.12) c.Set(x, y, faint, glow: true);
            }
        // A rune in the middle of some tiles.
        if (variant % 2 == 0)
        {
            c.Line(8, 4, 8, 11, main, glow: true);
            c.Line(5, 8, 11, 8, mid, glow: true);
            c.Set(6, 10, main, glow: true); c.Set(10, 6, main, glow: true);
            c.Set(6, 6, mid, glow: true); c.Set(10, 10, mid, glow: true);
        }
        return c;
    }

    /// <summary>A stone arch filled with a swirling seal, standing where the road enters a locked land.</summary>
    public static PixelCanvas SealGate(Biome b)
    {
        var c = new PixelCanvas(32, 32);
        var s = DungeonStone(b);
        var glowCol = Px.Hex(SealColorHex(b));

        // Two pillars and an arch
        for (int y = 0; y <= 22; y++)
            for (int x = 0; x <= 31; x++)
            {
                bool pillar = (x >= 2 && x <= 7) || (x >= 24 && x <= 29);
                if (pillar) c.Set(x, y, Brick(x, y, s[0], s[1], s[2], 7));
            }
        for (int y = 20; y <= 29; y++)
            for (int x = 2; x <= 29; x++)
            {
                float dx = (x - 15.5f) / 13.5f, dy = (y - 18f) / 11f;
                float r = dx * dx + dy * dy;
                if (r <= 1f && r >= 0.42f) c.Set(x, y, Brick(x, y, s[0], s[1], s[2], 9));
            }
        // Keystone with a glowing lock rune
        c.Fill(14, 26, 4, 4, s[0]);
        c.Set(15, 27, glowCol, glow: true); c.Set(16, 27, glowCol, glow: true); c.Set(15, 28, glowCol, glow: true); c.Set(16, 28, glowCol, glow: true);

        // The seal itself: a swirling disc of light in the doorway (all of it glows)
        var sealPixels = new System.Collections.Generic.List<int>();
        c.Ellipse(15.5f, 13f, 7.5f, 12f, (x, y, dx, dy) =>
        {
            sealPixels.Add(y * c.W + x);
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            float swirl = (float)Math.Sin(Math.Atan2(dy, dx) * 4 + d * 8);
            if (d > 0.85f) return glowCol;
            return swirl > 0.4f ? glowCol.Shade(1.3f) : swirl > -0.3f ? glowCol.Shade(0.85f).WithAlpha(220) : glowCol.Shade(0.55f).WithAlpha(180);
        });
        foreach (int i in sealPixels) if (i >= 0 && i < c.Glow.Length && !c.Data[i].IsClear) c.Glow[i] = true;
        c.Outline();
        c.PivotY = 0.25f;
        return c;
    }

    /// <summary>The artifact that opens a land: Crypt Key, Blood Sigil, Ember Heart.</summary>
    public static PixelCanvas Artifact(Biome opens) => opens switch
    {
        Biome.Crypt => PixelCanvas.FromArt(new[]
        {
            ".GGG....",
            "G.g.G...",
            "GgGgG...",
            ".GGG....",
            "..G.....",
            "..GGG...",
            "..G.....",
            "..GG....",
        }, "G=6cf0b0 g=d8fff0", glowChars: "Gg"),
        Biome.Bloodmoor => PixelCanvas.FromArt(new[]
        {
            "...R....",
            "..RrR...",
            ".RrWrR..",
            "RrWWWrR.",
            ".RrWrR..",
            "..RrR...",
            "...R....",
        }, "R=c02a40 r=ff5a6e W=ffd0d8", glowChars: "rW"),
        _ => PixelCanvas.FromArt(new[]
        {
            ".OO.OO..",
            "OyyOyyO.",
            "OyYYYyO.",
            ".OyYyO..",
            "..OyO...",
            "...O....",
        }, "O=c04a10 y=ffa040 Y=fff0a0", glowChars: "yY"),
    };

    public static string ArtifactName(Biome opens) => opens switch
    {
        Biome.Crypt => "Crypt Key",
        Biome.Bloodmoor => "Blood Sigil",
        Biome.Ashlands => "Ember Heart",
        _ => "Artifact",
    };
}
