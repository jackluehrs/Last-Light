using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The look of the HUD: dark navy rounded cards, grid-lined inset panels, small tag chips and a pixel font.
/// Everything here is drawn with Unity's immediate-mode GUI (call these from OnGUI).
/// </summary>
public static class Ui
{
    // ---------- Colours ----------
    public static readonly Color PanelBg = Hex("1a2330"), PanelBorder = Hex("2c3848");
    public static readonly Color InsetBg = Hex("131b26"), InsetBorder = Hex("232e3d"), GridLine = new Color(1f, 1f, 1f, 0.035f);
    public static readonly Color ChipBg = Hex("2a3646");
    public static readonly Color White = Hex("eef1f6"), Muted = Hex("8d9aab");
    public static readonly Color GoldText = Hex("ffd75e"), XpBlue = Hex("5ec8ff"), Bad = Hex("ff7a7a"), Good = Hex("8be07a");
    public static readonly Color HpGreen = Hex("7cd05c"), HpRed = Hex("e04848");

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }

    // ---------- Font & text styles ----------

    static Font font;
    static bool fontLoaded;

    /// <summary>The pixel font (Assets/Resources/Fonts/PixelifySans.ttf). Falls back to Unity's default if missing.</summary>
    public static Font Font
    {
        get
        {
            if (!fontLoaded) { font = Resources.Load<Font>("Fonts/PixelifySans"); fontLoaded = true; }
            return font;
        }
    }

    static readonly Dictionary<int, GUIStyle> styles = new();

    /// <summary>A white text style (colour it with the Text() colour argument or rich-text tags).</summary>
    public static GUIStyle Style(int size, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false, bool wrap = false)
    {
        int key = size * 1000 + (int)align * 10 + (bold ? 1 : 0) + (wrap ? 2 : 0) * 100;
        if (styles.TryGetValue(key, out var s) && s != null) return s;
        s = new GUIStyle(GUI.skin.label)
        {
            fontSize = size, alignment = align, wordWrap = wrap, richText = true,
            fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
            padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0),
            clipping = TextClipping.Overflow,
        };
        if (Font != null) s.font = Font;
        s.normal.textColor = Color.white;
        styles[key] = s;
        return s;
    }

    /// <summary>Draws text in a colour, with an optional 1px drop shadow so it reads over the game world.</summary>
    public static void Text(Rect r, string text, GUIStyle style, Color color, bool shadow = false)
    {
        var old = GUI.contentColor;
        if (shadow)
        {
            GUI.contentColor = new Color(0.05f, 0.04f, 0.08f, color.a * 0.85f);
            GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), text, style);
        }
        GUI.contentColor = color;
        GUI.Label(r, text, style);
        GUI.contentColor = old;
    }

    public static Vector2 Measure(string text, GUIStyle style) => style.CalcSize(new GUIContent(text));

    // ---------- Shapes ----------

    static Texture2D roundFill, roundBorder, grid;
    static GUIStyle fillStyle, borderStyle;

    const int TexSize = 12, Corner = 4;

    static void MakeTextures()
    {
        if (roundFill != null) return;
        roundFill = NewTex(TexSize, (x, y) => InsideRounded(x, y) ? Color.white : Color.clear);
        roundBorder = NewTex(TexSize, (x, y) =>
            InsideRounded(x, y) && (!InsideRounded(x - 1, y) || !InsideRounded(x + 1, y) || !InsideRounded(x, y - 1) || !InsideRounded(x, y + 1))
                ? Color.white : Color.clear);
        grid = NewTex(12, (x, y) => x == 0 || y == 0 ? Color.white : Color.clear);
        grid.wrapMode = TextureWrapMode.Repeat;

        fillStyle = new GUIStyle { normal = { background = roundFill }, border = new RectOffset(Corner, Corner, Corner, Corner) };
        borderStyle = new GUIStyle { normal = { background = roundBorder }, border = new RectOffset(Corner, Corner, Corner, Corner) };
    }

    /// <summary>Pixel-stepped rounded corners (radius 3).</summary>
    static bool InsideRounded(int x, int y)
    {
        if (x < 0 || y < 0 || x >= TexSize || y >= TexSize) return false;
        int cx = Mathf.Min(x, TexSize - 1 - x), cy = Mathf.Min(y, TexSize - 1 - y);
        // cut the corner pixels: (0,0),(0,1),(1,0) and (0,2),(2,0) for a soft 3px round
        if (cx + cy < 2) return false;
        if (cx == 0 && cy == 2 || cx == 2 && cy == 0) return false;
        return true;
    }

    static Texture2D NewTex(int size, System.Func<int, int, Color> px)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        for (int x = 0; x < size; x++) for (int y = 0; y < size; y++) t.SetPixel(x, y, px(x, y));
        t.Apply();
        return t;
    }

    /// <summary>A rounded card in any fill and border colour.</summary>
    public static void Card(Rect r, Color fill, Color border)
    {
        if (Event.current.type != EventType.Repaint) return;
        MakeTextures();
        var old = GUI.color;
        GUI.color = fill * old;
        fillStyle.Draw(r, false, false, false, false);
        GUI.color = border * old;
        borderStyle.Draw(r, false, false, false, false);
        GUI.color = old;
    }

    /// <summary>The main dark-navy card used for every HUD panel.</summary>
    public static void Panel(Rect r) => Card(r, PanelBg, PanelBorder);

    /// <summary>A darker inset with faint grid lines (like the art frames in the reference).</summary>
    public static void Inset(Rect r) => Inset(r, InsetBorder);

    public static void Inset(Rect r, Color border)
    {
        Card(r, InsetBg, border);
        if (Event.current.type != EventType.Repaint) return;
        var old = GUI.color;
        GUI.color = GridLine * old;
        var inner = new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4);
        GUI.DrawTextureWithTexCoords(inner, grid, new Rect(0, 0, inner.width / 12f, inner.height / 12f));
        GUI.color = old;
    }

    /// <summary>A small rounded tag with text, like "mana burn" in the reference. Returns its width.</summary>
    public static float Chip(Vector2 at, string text, Color textColor, int size = 11, Color? bg = null)
    {
        var style = Style(size, TextAnchor.MiddleCenter);
        float w = Measure(text, style).x + 12f, h = size + 7f;
        var r = new Rect(at.x, at.y, w, h);
        Card(r, bg ?? ChipBg, (bg ?? ChipBg) * 1.25f);
        Text(r, text, style, textColor);
        return w;
    }

    /// <summary>A chip whose right edge sits at the given x.</summary>
    public static float ChipRight(float right, float y, string text, Color textColor, int size = 11, Color? bg = null)
    {
        float w = Measure(text, Style(size, TextAnchor.MiddleCenter)).x + 12f;
        return Chip(new Vector2(right - w, y), text, textColor, size, bg);
    }

    /// <summary>A rounded progress bar.</summary>
    public static void Bar(Rect r, float fraction, Color color)
    {
        Card(r, Hex("0c1119"), Hex("2c3848"));
        fraction = Mathf.Clamp01(fraction);
        if (fraction <= 0f || Event.current.type != EventType.Repaint) return;
        var fill = new Rect(r.x + 2, r.y + 2, (r.width - 4) * fraction, r.height - 4);
        var old = GUI.color;
        GUI.color = color * old;
        GUI.DrawTexture(fill, Texture2D.whiteTexture);
        GUI.color = Color.Lerp(color, Color.white, 0.45f) * old; // shiny top edge
        GUI.DrawTexture(new Rect(fill.x, fill.y, fill.width, 1f), Texture2D.whiteTexture);
        GUI.color = old;
    }

    /// <summary>Draws a sprite (whole texture) fitted inside the rect, keeping its shape.</summary>
    public static void Icon(Rect r, Sprite sprite, float alpha = 1f)
    {
        if (sprite == null || Event.current.type != EventType.Repaint) return;
        var tex = sprite.texture;
        var tr = sprite.textureRect;
        float scale = Mathf.Min(r.width / tr.width, r.height / tr.height);
        float w = tr.width * scale, h = tr.height * scale;
        var dst = new Rect(r.x + (r.width - w) / 2f, r.y + (r.height - h) / 2f, w, h);
        var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        var old = GUI.color;
        GUI.color = new Color(old.r, old.g, old.b, old.a * alpha);
        GUI.DrawTextureWithTexCoords(dst, tex, uv);
        GUI.color = old;
    }

    /// <summary>A dark see-through layer over the whole screen (behind menus).</summary>
    public static void Dim(float w, float h, float alpha = 0.6f)
    {
        if (Event.current.type != EventType.Repaint) return;
        var old = GUI.color;
        GUI.color = new Color(0.03f, 0.04f, 0.07f, alpha);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = old;
    }

    public static void Outline(Rect r, float t, Color c)
    {
        if (Event.current.type != EventType.Repaint) return;
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Texture2D.whiteTexture);
        GUI.color = old;
    }
}
