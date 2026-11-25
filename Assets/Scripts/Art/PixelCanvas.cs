using System;
using System.Collections.Generic;

/// <summary>One RGBA pixel colour. Kept free of UnityEngine so the art can be previewed outside Unity.</summary>
public struct Px
{
    public byte R, G, B, A;
    public Px(byte r, byte g, byte b, byte a = 255) { R = r; G = g; B = b; A = a; }

    public static readonly Px Clear = new Px(0, 0, 0, 0);
    public bool IsClear => A == 0;

    /// <summary>"6a3fb5" or "6a3fb580" (with alpha).</summary>
    public static Px Hex(string hex)
    {
        byte P(int i) => Convert.ToByte(hex.Substring(i, 2), 16);
        return new Px(P(0), P(2), P(4), hex.Length >= 8 ? P(6) : (byte)255);
    }

    /// <summary>Darker (f &lt; 1) or lighter (f &gt; 1) copy.</summary>
    public Px Shade(float f)
    {
        byte C(byte v) => (byte)Math.Max(0, Math.Min(255, (int)(v * f)));
        return new Px(C(R), C(G), C(B), A);
    }

    public Px WithAlpha(byte a) => new Px(R, G, B, a);

    public static Px Lerp(Px a, Px b, float t)
    {
        byte L(byte x, byte y) => (byte)(x + (y - x) * t);
        return new Px(L(a.R, b.R), L(a.G, b.G), L(a.B, b.B), L(a.A, b.A));
    }
}

/// <summary>
/// A small pixel image to draw art into. (0,0) is the BOTTOM-left, like Unity textures.
/// Pixels can also be marked "glow": those get copied into a second sprite that's drawn
/// unlit, so they shine through the night (eyes, crystals, windows, flames).
/// </summary>
public class PixelCanvas
{
    public readonly int W, H;
    public readonly Px[] Data;
    public readonly bool[] Glow;

    /// <summary>Where the sprite's pivot sits, as a fraction of width/height.</summary>
    public float PivotX = 0.5f, PivotY = 0.5f;

    public static readonly Px OutlineColor = Px.Hex("1a1422");

    public PixelCanvas(int w, int h)
    {
        W = w; H = h;
        Data = new Px[w * h];
        Glow = new bool[w * h];
    }

    public bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
    public Px Get(int x, int y) => In(x, y) ? Data[y * W + x] : Px.Clear;
    public bool IsGlow(int x, int y) => In(x, y) && Glow[y * W + x];
    public bool HasGlow { get { foreach (var g in Glow) if (g) return true; return false; } }

    public void Set(int x, int y, Px c, bool glow = false)
    {
        if (!In(x, y)) return;
        Data[y * W + x] = c;
        Glow[y * W + x] = glow && !c.IsClear;
    }

    public void Fill(int x, int y, int w, int h, Px c, bool glow = false)
    {
        for (int i = x; i < x + w; i++)
            for (int j = y; j < y + h; j++)
                Set(i, j, c, glow);
    }

    /// <summary>A straight line between two points (thickness 1 or 2).</summary>
    public void Line(int x0, int y0, int x1, int y1, Px c, int thick = 1, bool glow = false)
    {
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
        while (true)
        {
            Set(x0, y0, c, glow);
            if (thick > 1) { Set(x0 + 1, y0, c, glow); Set(x0, y0 + 1, c, glow); }
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    public void HLine(int x0, int x1, int y, Px c) { for (int x = x0; x <= x1; x++) Set(x, y, c); }
    public void VLine(int x, int y0, int y1, Px c) { for (int y = y0; y <= y1; y++) Set(x, y, c); }

    /// <summary>Filled ellipse. shade(x, y, dx, dy) picks each pixel's colour (dx, dy in -1..1).</summary>
    public void Ellipse(float cx, float cy, float rx, float ry, Func<int, int, float, float, Px> shade)
    {
        for (int x = (int)(cx - rx - 1); x <= (int)(cx + rx + 1); x++)
            for (int y = (int)(cy - ry - 1); y <= (int)(cy + ry + 1); y++)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                if (dx * dx + dy * dy <= 1f) Set(x, y, shade(x, y, dx, dy));
            }
    }

    public void Ellipse(float cx, float cy, float rx, float ry, Px c) => Ellipse(cx, cy, rx, ry, (x, y, dx, dy) => c);

    /// <summary>Draw another canvas on top (clear pixels are skipped).</summary>
    public void Draw(PixelCanvas o, int ox, int oy)
    {
        for (int x = 0; x < o.W; x++)
            for (int y = 0; y < o.H; y++)
            {
                var c = o.Get(x, y);
                if (!c.IsClear) Set(ox + x, oy + y, c, o.IsGlow(x, y));
            }
    }

    /// <summary>Puts a dark 1-pixel line around everything that's drawn (the chunky pixel-art look).</summary>
    public void Outline() => Outline(OutlineColor);

    public void Outline(Px color)
    {
        var add = new List<int>();
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                if (!Get(x, y).IsClear) continue;
                if (Solid(x - 1, y) || Solid(x + 1, y) || Solid(x, y - 1) || Solid(x, y + 1)) add.Add(y * W + x);
            }
        foreach (int i in add) Data[i] = color;
    }

    bool Solid(int x, int y) { var c = Get(x, y); return c.A > 100 && !(c.R == OutlineColor.R && c.G == OutlineColor.G && c.B == OutlineColor.B); }

    /// <summary>Mirror left-right.</summary>
    public PixelCanvas FlipX()
    {
        var c = new PixelCanvas(W, H) { PivotX = 1f - PivotX, PivotY = PivotY };
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
                c.Set(W - 1 - x, y, Get(x, y), IsGlow(x, y));
        return c;
    }

    /// <summary>
    /// Build a canvas from rows of text, TOP row first. Each character is looked up in the palette
    /// ("P=6a3fb5 p=45297a ..."); '.' or a space is transparent. Characters listed in glowChars glow.
    /// A 1-pixel border is added and the art is outlined.
    /// </summary>
    public static PixelCanvas FromArt(string[] rows, string palette, string glowChars = "", bool outline = true)
    {
        var pal = ParsePalette(palette);
        int h = rows.Length, w = 0;
        foreach (var r in rows) w = Math.Max(w, r.Length);
        int pad = outline ? 1 : 0;
        var c = new PixelCanvas(w + pad * 2, h + pad * 2);
        for (int row = 0; row < h; row++)
            for (int x = 0; x < rows[row].Length; x++)
            {
                char ch = rows[row][x];
                if (ch == '.' || ch == ' ') continue;
                if (!pal.TryGetValue(ch, out var col))
                    throw new ArgumentException($"Pixel art uses '{ch}' but the palette doesn't define it.");
                c.Set(x + pad, h - 1 - row + pad, col, glowChars.IndexOf(ch) >= 0);
            }
        if (outline) c.Outline();
        return c;
    }

    public static Dictionary<char, Px> ParsePalette(string palette)
    {
        var pal = new Dictionary<char, Px>();
        foreach (var entry in palette.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            pal[entry[0]] = Px.Hex(entry.Substring(2));
        return pal;
    }
}
