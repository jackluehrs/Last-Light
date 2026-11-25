using System;

/// <summary>The lands of the map. The Meadow is in the middle around the Hub; the others surround it.</summary>
public enum Biome { Meadow, DarkForest, Bloodmoor, Crypt, Ashlands }

/// <summary>Ground, obstacles and dungeon pieces for every biome (same style as PixelArt.cs).</summary>
public static partial class PixelArt
{
    public static string BiomeName(Biome b) => b switch
    {
        Biome.DarkForest => "the Dark Forest",
        Biome.Bloodmoor => "the Bloodmoor",
        Biome.Crypt => "the Crypt Lands",
        Biome.Ashlands => "the Ashlands",
        _ => "the Meadow",
    };

    // ---------- Ground ----------

    /// <summary>Four ground tones per biome: base, second, highlight, shadow.</summary>
    static Px[] GroundPalette(Biome b, int tone)
    {
        string[] hex = b switch
        {
            Biome.DarkForest => tone == 0 ? new[] { "2f5a3e", "2a5238", "3e6e4a", "1f4030" } : new[] { "2a5038", "254832", "386444", "1a3828" },
            Biome.Bloodmoor => tone == 0 ? new[] { "5a2e40", "52283a", "6e3a4e", "3e1c2c" } : new[] { "4e2838", "472232", "623446", "36182a" },
            Biome.Crypt => tone == 0 ? new[] { "4e5450", "474c48", "5c625c", "383c3a" } : new[] { "464c48", "404540", "545a54", "323634" },
            Biome.Ashlands => tone == 0 ? new[] { "4a3e3c", "433836", "564a46", "322a2a" } : new[] { "403634", "3a302e", "4c403e", "2a2222" },
            _ => tone == 0 ? new[] { "6cb04a", "62a444", "7cc056", "538f3a" } : new[] { "5a9a40", "528e3a", "68a84a", "467c32" },
        };
        return Array.ConvertAll(hex, Px.Hex);
    }

    /// <summary>
    /// A ground tile. decor 0 = plain; 1..3 = the biome's decorations
    /// (Meadow: tufts/flowers/pebbles, Forest: ferns/glowing mushrooms/roots, Bloodmoor: dry grass/roses/bones,
    /// Crypt: flagstones/skull/candles, Ashlands: cracks/lava vein/embers). Some decorations glow.
    /// </summary>
    public static PixelCanvas Ground(Biome b, int tone, int decor, int seed)
    {
        if (b == Biome.Meadow) return Grass(tone, decor, seed);

        var c = new PixelCanvas(16, 16);
        var pal = GroundPalette(b, tone);
        var rng = new Random(seed * 31 + tone * 7 + decor + (int)b * 1000);
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
            {
                float n = Hash(x, y, seed + (int)b * 77);
                c.Set(x, y, n < 0.55f ? pal[0] : n < 0.85f ? pal[1] : n < 0.94f ? pal[3] : pal[2]);
            }
        for (int i = 0; i < 4; i++)
        {
            int x = rng.Next(1, 15), y = rng.Next(0, 14);
            c.Set(x, y, pal[3]); c.Set(x, y + 1, pal[2]);
        }

        int R(int lo, int hi) => rng.Next(lo, hi);
        switch (b)
        {
            case Biome.DarkForest:
                if (decor == 1) // ferns
                    for (int i = 0; i < 2; i++)
                    {
                        int x = R(3, 12), y = R(2, 11);
                        Px f = Px.Hex("4a8a4e"), fd = Px.Hex("2e6a3a");
                        c.Line(x, y, x - 3, y + 3, f); c.Line(x, y, x + 3, y + 3, f); c.Line(x, y, x, y + 4, fd);
                    }
                else if (decor == 2) // glowing mushrooms
                    for (int i = 0; i < 3; i++)
                    {
                        int x = R(2, 13), y = R(2, 12);
                        Px cap = i % 2 == 0 ? Px.Hex("6ae0ff") : Px.Hex("c87aff");
                        c.Set(x, y, Px.Hex("d8d0c0")); c.Set(x, y + 1, cap, true); c.Set(x - 1, y + 1, cap, true); c.Set(x + 1, y + 1, cap, true);
                        c.Set(x, y + 2, Px.Lerp(cap, Px.Hex("ffffff"), 0.5f), true);
                    }
                else if (decor == 3) // roots
                {
                    int y = R(4, 11);
                    c.Line(0, y, 7, y + 2, WoodD); c.Line(7, y + 2, 15, y + 1, WoodD); c.Line(4, y + 1, 9, y + 3, Wood);
                }
                break;

            case Biome.Bloodmoor:
                if (decor == 1) // dry grass
                    for (int i = 0; i < 3; i++)
                    {
                        int x = R(2, 13), y = R(1, 12);
                        Px g = Px.Hex("8a5a4a");
                        c.Set(x, y, g); c.Set(x - 1, y + 1, g); c.Set(x + 1, y + 1, g); c.Set(x, y + 2, Px.Hex("a87a5a"));
                    }
                else if (decor == 2) // blood roses (glow faintly)
                    for (int i = 0; i < 3; i++)
                    {
                        int x = R(2, 13), y = R(2, 12);
                        c.Set(x, y, Px.Hex("2e5a2e")); c.Set(x, y - 1, Px.Hex("2e5a2e"));
                        c.Set(x, y + 1, Px.Hex("e8304a"), true); c.Set(x - 1, y + 1, Px.Hex("a81a30"), true);
                        c.Set(x + 1, y + 1, Px.Hex("a81a30"), true); c.Set(x, y + 2, Px.Hex("ff6a7a"), true);
                    }
                else if (decor == 3) // scattered bones
                {
                    int x = R(3, 10), y = R(3, 12);
                    Px bone = Px.Hex("d8d0bc");
                    c.HLine(x, x + 4, y, bone); c.Set(x - 1, y + 1, bone); c.Set(x - 1, y - 1, bone); c.Set(x + 5, y + 1, bone); c.Set(x + 5, y - 1, bone);
                }
                break;

            case Biome.Crypt:
                if (decor == 1) // old flagstones
                {
                    Px s = Px.Hex("6a706a"), sl = Px.Hex("7c827a"), sd = Px.Hex("3e423e");
                    for (int x = 1; x < 15; x++)
                        for (int y = 1; y < 15; y++)
                        {
                            if (Hash(x / 5, y / 5, seed) < 0.35f) continue;
                            bool seam = x % 5 == 0 || y % 5 == 0;
                            c.Set(x, y, seam ? sd : (y % 5 == 4 ? sl : s));
                        }
                }
                else if (decor == 2) // skull
                {
                    int x = R(4, 10), y = R(4, 10);
                    Px w = Px.Hex("e0dac8"), k = Px.Hex("2a2430");
                    c.Fill(x, y, 4, 3, w); c.HLine(x + 1, x + 2, y + 3, w); c.Set(x, y + 1, k); c.Set(x + 3, y + 1, k);
                    c.Set(x + 1, y - 1, w); c.Set(x + 2, y - 1, w);
                }
                else if (decor == 3) // ghost candles
                    for (int i = 0; i < 2; i++)
                    {
                        int x = R(3, 13), y = R(2, 11);
                        c.VLine(x, y, y + 2, Px.Hex("d8d0bc"));
                        c.Set(x, y + 3, Px.Hex("8affb0"), true); c.Set(x, y + 4, Px.Hex("d8ffe6"), true);
                    }
                break;

            case Biome.Ashlands:
                if (decor == 1) // dark cracks
                {
                    int x = R(2, 6), y = R(3, 12);
                    c.Line(x, y, x + 5, y + 2, pal[3]); c.Line(x + 5, y + 2, x + 10, y - 1, pal[3]);
                }
                else if (decor == 2) // glowing lava vein
                {
                    int x = R(1, 5), y = R(4, 11);
                    c.Line(x, y, x + 5, y + 3, Px.Hex("ff7a1e"), 1, true); c.Line(x + 5, y + 3, x + 11, y + 1, Px.Hex("ff7a1e"), 1, true);
                    c.Set(x + 5, y + 3, Px.Hex("ffd060"), true);
                }
                else if (decor == 3) // embers and charred bits
                    for (int i = 0; i < 4; i++)
                    {
                        int x = R(1, 15), y = R(1, 15);
                        c.Set(x, y, i % 2 == 0 ? Px.Hex("ffb040") : Px.Hex("1e1818"), i % 2 == 0);
                    }
                break;
        }
        return c;
    }

    /// <summary>The road that leads from the Hub out to each dungeon.</summary>
    public static PixelCanvas PathTile(Biome b, int seed)
    {
        var c = new PixelCanvas(16, 16);
        bool cobbles = b == Biome.Crypt || b == Biome.Ashlands;
        Px a, m, d;
        switch (b)
        {
            case Biome.Crypt: a = Px.Hex("7a7e78"); m = Px.Hex("666a64"); d = Px.Hex("44483f"); break;
            case Biome.Ashlands: a = Px.Hex("5e5250"); m = Px.Hex("4c4240"); d = Px.Hex("2e2626"); break;
            case Biome.Bloodmoor: a = Px.Hex("6e4a40"); m = Px.Hex("5e3e36"); d = Px.Hex("44282a"); break;
            case Biome.DarkForest: a = Px.Hex("6a5236"); m = Px.Hex("5a442c"); d = Px.Hex("3e2e1e"); break;
            default: a = Px.Hex("b08a5a"); m = Px.Hex("9a774c"); d = Px.Hex("7a5c3a"); break;
        }
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
            {
                float n = Hash(x, y, seed + 5);
                Px p = n < 0.6f ? m : n < 0.85f ? a : d;
                if (cobbles)
                {
                    int bx = (x + (y / 4 % 2) * 2) % 4, by = y % 4;
                    p = bx == 0 || by == 0 ? d : (by == 3 ? a : m);
                }
                c.Set(x, y, p);
            }
        return c;
    }

    // ---------- Obstacles (block the hero; enemies walk through) ----------

    /// <summary>
    /// Something that blocks the way in a biome.
    /// Meadow: oak, pine. Dark Forest: dark pine, gnarled tree, watcher tree (glowing eyes).
    /// Bloodmoor: dead tree, crimson tree. Crypt: gravestone, broken pillar, dead tree.
    /// Ashlands: basalt rocks, lava pool, charred tree.
    /// </summary>
    public static PixelCanvas Obstacle(Biome b, int variant)
    {
        switch (b)
        {
            case Biome.DarkForest:
                if (variant == 0) return Pine(Px.Hex("3a7060"), Px.Hex("265448"), Px.Hex("1a3e36"), Px.Hex("10261f"));
                return Oak(Px.Hex("4a6e58"), Px.Hex("2e5244"), Px.Hex("1f3e34"), Px.Hex("142a24"), variant == 2);
            case Biome.Bloodmoor:
                if (variant == 0) return DeadTree(Px.Hex("5a4a4e"), Px.Hex("3e3036"));
                return Oak(Px.Hex("c8405a"), Px.Hex("962a40"), Px.Hex("6e1a2e"), Px.Hex("4a1020"), false);
            case Biome.Crypt:
                if (variant == 0) return Gravestone();
                if (variant == 1) return BrokenPillar();
                return DeadTree(Px.Hex("6a6a66"), Px.Hex("44443e"));
            case Biome.Ashlands:
                if (variant == 0) return Basalt();
                if (variant == 1) return LavaPool();
                return DeadTree(Px.Hex("2e2626"), Px.Hex("1e1818"), embers: true);
            default:
                return Tree(variant % 2);
        }
    }

    static PixelCanvas Pine(Px l, Px m, Px d, Px dd)
    {
        var c = new PixelCanvas(16, 16);
        c.Fill(7, 1, 2, 2, WoodD);
        int[] widths = { 13, 11, 9, 11, 9, 7, 7, 5, 3, 1 };
        for (int i = 0; i < widths.Length; i++)
        {
            int y = 3 + i, w = widths[i], x0 = 8 - (w + 1) / 2;
            for (int x = x0; x < x0 + w; x++)
            {
                float rel = (x - x0) / (float)Math.Max(1, w - 1);
                Px p = rel < 0.3f ? l : rel < 0.7f ? m : d;
                if (y == 3 || y == 6 || y == 9) p = dd;
                c.Set(x, y, p);
            }
        }
        c.Outline(Px.Hex("0c1612"));
        return c;
    }

    static PixelCanvas Oak(Px l, Px m, Px d, Px dd, bool eyes)
    {
        var c = new PixelCanvas(16, 16);
        c.Fill(7, 1, 2, 4, WoodD); c.VLine(8, 1, 4, WoodDD); c.Set(6, 1, WoodDD); c.Set(9, 1, WoodDD);
        Func<int, int, float, float, Px> shade = (x, y, dx, dy) =>
        {
            float lt = -dx * 0.5f + dy * 0.85f + (Hash(x, y, 9) - 0.5f) * 0.45f;
            return lt > 0.55f ? l : lt > 0f ? m : lt > -0.6f ? d : dd;
        };
        c.Ellipse(7.5f, 7f, 6f, 3.4f, shade);
        c.Ellipse(5f, 10f, 3.6f, 3.4f, shade);
        c.Ellipse(10f, 10.5f, 3.6f, 3.2f, shade);
        c.Ellipse(7.5f, 12f, 3.5f, 2.6f, shade);
        if (eyes) { c.Set(6, 8, Px.Hex("f2dc3a"), true); c.Set(9, 8, Px.Hex("f2dc3a"), true); }
        c.Outline(Px.Hex("0c1612"));
        return c;
    }

    static PixelCanvas DeadTree(Px bark, Px barkD, bool embers = false)
    {
        var c = new PixelCanvas(16, 16);
        c.Fill(7, 1, 2, 7, bark); c.VLine(8, 1, 7, barkD);
        c.Line(7, 7, 3, 12, bark); c.Line(3, 12, 2, 14, barkD); c.Line(5, 10, 6, 13, barkD);
        c.Line(8, 8, 12, 11, bark); c.Line(12, 11, 14, 14, barkD); c.Line(11, 10, 11, 14, bark);
        c.Line(8, 8, 8, 13, barkD);
        c.Set(6, 1, barkD); c.Set(9, 1, barkD);
        if (embers) { c.Set(8, 5, Px.Hex("ff7a1e"), true); c.Set(4, 11, Px.Hex("ff7a1e"), true); c.Set(12, 12, Px.Hex("ffb040"), true); }
        c.Outline(Px.Hex("120e14"));
        return c;
    }

    static PixelCanvas Gravestone()
    {
        var c = new PixelCanvas(16, 16);
        Px s = Px.Hex("8a908a"), sl = Px.Hex("b0b6ae"), sd = Px.Hex("5c625c");
        c.Fill(4, 1, 8, 9, s);
        c.Ellipse(7.5f, 10f, 4f, 3f, (x, y, dx, dy) => dx < -0.3f ? sl : dx > 0.4f ? sd : s);
        c.VLine(4, 1, 10, sl); c.VLine(11, 1, 10, sd);
        c.VLine(7, 4, 10, sd); c.HLine(5, 10, 8, sd); // carved cross
        c.HLine(3, 12, 0, Px.Hex("3e2e24")); c.Set(5, 1, Moss); c.Set(6, 1, MossD); c.Set(10, 2, Moss);
        c.Outline();
        return c;
    }

    static PixelCanvas BrokenPillar()
    {
        var c = new PixelCanvas(16, 16);
        Px s = Px.Hex("9a9e96"), sl = Px.Hex("c0c4ba"), sd = Px.Hex("6a6e66");
        c.Fill(3, 1, 10, 2, sd); c.HLine(3, 12, 2, s);
        for (int y = 3; y <= 11; y++)
            for (int x = 5; x <= 10; x++)
            {
                if (y > 9 && x > 7 + (y - 9)) continue; // broken off top
                c.Set(x, y, x == 5 ? sl : x == 10 ? sd : ((x - 5) % 2 == 0 ? s : Px.Lerp(s, sd, 0.4f)));
            }
        c.Set(12, 3, sd); c.Set(13, 3, s); // rubble
        c.Set(6, 4, Moss); c.Set(7, 5, MossD);
        c.Outline();
        return c;
    }

    static PixelCanvas Basalt()
    {
        var c = new PixelCanvas(16, 16);
        Func<int, int, float, float, Px> rock = (x, y, dx, dy) =>
        {
            float l = -dx * 0.55f + dy * 0.8f + (Hash(x, y, 44) - 0.5f) * 0.4f;
            return l > 0.5f ? Px.Hex("5e5454") : l > 0f ? Px.Hex("443c3c") : l > -0.55f ? Px.Hex("302a2a") : Px.Hex("201c1c");
        };
        c.Ellipse(6f, 6f, 5f, 5f, rock);
        c.Ellipse(11f, 5f, 4f, 4.5f, rock);
        c.Ellipse(8f, 10f, 3.5f, 3f, rock);
        c.Line(5, 3, 8, 7, Px.Hex("ff7a1e"), 1, true);
        c.Outline();
        return c;
    }

    static PixelCanvas LavaPool()
    {
        var c = new PixelCanvas(16, 16);
        c.Ellipse(7.5f, 7.5f, 7.5f, 7f, (x, y, dx, dy) => Px.Hex("2a2222"));
        c.Ellipse(7.5f, 7.5f, 6f, 5.5f, (x, y, dx, dy) =>
        {
            float n = Hash(x, y, 77);
            return dx * dx + dy * dy < 0.25f && n > 0.4f ? Px.Hex("ffd060") : n > 0.75f ? Px.Hex("e8481e") : Px.Hex("ff8a2a");
        });
        for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++)
        {
            var p = c.Get(x, y);
            if (!p.IsClear && p.R > 200) c.Set(x, y, p, true);
        }
        return c;
    }

    // =====================================================================
    //  DUNGEONS
    // =====================================================================

    static Px[] DungeonStone(Biome b) => Array.ConvertAll(b switch
    {
        Biome.DarkForest => new[] { "6a7a5a", "52604a", "3e4a38", "26302a" }, // mossy earth-stone
        Biome.Bloodmoor => new[] { "7a5a66", "5e4450", "46323c", "2a1c24" }, // castle stone
        Biome.Crypt => new[] { "8a8e8a", "6c706c", "50544f", "30332f" },     // tomb stone
        Biome.Ashlands => new[] { "5e4e4a", "483c38", "352c2a", "1e1818" },  // basalt
        _ => new[] { "a3a7b2", "80848f", "5c5f6b", "3e404b" },
    }, Px.Hex);

    /// <summary>Dungeon floor. variant 0/1 plain, 2 = biome detail (moss, carpet, bones, lava crack).</summary>
    public static PixelCanvas DungeonFloor(Biome b, int variant)
    {
        var c = new PixelCanvas(16, 16);
        var s = DungeonStone(b);
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
            {
                bool seam = x % 8 == 0 || y % 8 == 0;
                float n = Hash(x, y, 90 + variant);
                Px p = seam ? s[3] : n < 0.7f ? s[2] : n < 0.92f ? s[1] : s[3];
                if (!seam && (x % 8 == 1 || y % 8 == 7)) p = Px.Lerp(p, s[1], 0.5f);
                c.Set(x, y, p);
            }
        if (variant == 2)
        {
            switch (b)
            {
                case Biome.Bloodmoor: // red carpet stripe
                    for (int x = 0; x < 16; x++) for (int y = 3; y <= 12; y++)
                        c.Set(x, y, y == 3 || y == 12 ? Gold : (y == 4 || y == 11 ? Px.Hex("6e1a2e") : Px.Hex("962a40")));
                    break;
                case Biome.Ashlands:
                    c.Line(2, 4, 8, 9, Px.Hex("ff7a1e"), 1, true); c.Line(8, 9, 14, 6, Px.Hex("ff7a1e"), 1, true);
                    break;
                case Biome.Crypt:
                    c.HLine(5, 9, 6, Px.Hex("d8d0bc")); c.Set(4, 7, Px.Hex("d8d0bc")); c.Set(10, 5, Px.Hex("d8d0bc"));
                    break;
                default:
                    foreach (var (x, y) in new[] { (3, 3), (4, 3), (4, 4), (11, 10), (12, 10), (12, 11), (7, 13) }) c.Set(x, y, Moss);
                    break;
            }
        }
        return c;
    }

    /// <summary>Dungeon wall block (fills its tile so walls join up).</summary>
    public static PixelCanvas DungeonWall(Biome b)
    {
        var c = new PixelCanvas(16, 16);
        var s = DungeonStone(b);
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
            {
                Px p;
                if (y >= 12) p = y == 15 ? s[0] : Px.Lerp(s[0], s[1], Hash(x, y, 5) > 0.6f ? 0.6f : 0.2f);
                else if (y == 11) p = s[3];
                else p = Brick(x, y, s[0], s[1], s[3], 23 + (int)b);
                if (y == 0) p = s[3];
                c.Set(x, y, p);
            }
        if (b == Biome.DarkForest)
            foreach (var (x, y) in new[] { (2, 1), (3, 1), (3, 2), (12, 1), (6, 12), (7, 12), (13, 13) }) c.Set(x, y, Moss);
        return c;
    }

    /// <summary>A torch hanging on a dungeon wall.</summary>
    public static PixelCanvas WallTorch() => PixelCanvas.FromArt(new[]
    {
        ".o.",
        "oYo",
        "oOo",
        "kbk",
        ".b.",
    }, "o=f06a1e O=ffa83a Y=fff0a0 b=8a5a2e k=3e404b", glowChars: "oOY");

    /// <summary>Stairs leading back up to the surface (the dungeon's exit).</summary>
    public static PixelCanvas StairsUp()
    {
        var c = new PixelCanvas(16, 16);
        for (int i = 0; i < 5; i++)
        {
            int y = 1 + i * 3;
            Px top = Px.Lerp(StoneL, StoneM, i * 0.15f);
            c.Fill(2 + i, y, 12 - i * 2, 2, StoneM.Shade(1f - i * 0.08f));
            c.HLine(2 + i, 13 - i, y + 2, top);
        }
        c.Set(8, 15, Px.Hex("fff0c0"), true); c.Set(7, 14, Px.Hex("ffe08a"), true); c.Set(8, 14, Px.Hex("ffe08a"), true);
        c.Outline();
        return c;
    }

    /// <summary>A swirling portal home (appears when the boss falls).</summary>
    public static PixelCanvas Portal()
    {
        var c = new PixelCanvas(16, 22);
        c.Ellipse(7.5f, 11f, 6.5f, 9.5f, (x, y, dx, dy) =>
        {
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            float swirl = (float)Math.Sin(Math.Atan2(dy, dx) * 3 + d * 9);
            if (d > 0.82f) return Px.Hex("6a3ae0");
            return swirl > 0.3f ? Px.Hex("e0c8ff") : swirl > -0.4f ? Px.Hex("b07aff") : Px.Hex("7a4ae8");
        });
        for (int i = 0; i < c.Data.Length; i++) if (!c.Data[i].IsClear) c.Glow[i] = true;
        c.Outline(Px.Hex("2a1a4a"));
        return c;
    }

    /// <summary>A dungeon entrance on the overworld (2x2 tiles), themed for its biome.</summary>
    public static PixelCanvas DungeonEntrance(Biome b)
    {
        var c = new PixelCanvas(32, 32);
        var s = DungeonStone(b);

        // A mound / building shape around a dark doorway with stairs going down.
        switch (b)
        {
            case Biome.Crypt: // a mausoleum with a pointed roof and pillars
                for (int x = 3; x <= 28; x++) for (int y = 1; y <= 18; y++) c.Set(x, y, Brick(x, y, s[0], s[1], s[2], 3));
                for (int y = 19; y <= 27; y++)
                {
                    int half = 14 - (y - 19) * 14 / 9;
                    for (int x = 16 - half; x <= 15 + half; x++) c.Set(x, y, x < 16 ? s[0] : s[1]);
                }
                c.HLine(2, 29, 19, s[3]);
                foreach (int px in new[] { 5, 24 }) for (int y = 1; y <= 18; y++) { c.Set(px, y, s[0]); c.Set(px + 1, y, s[0]); c.Set(px + 2, y, s[1]); }
                c.Set(15, 24, Px.Hex("8affb0"), true); c.Set(16, 24, Px.Hex("8affb0"), true); // soul lamp
                break;
            case Biome.Bloodmoor: // a gothic gatehouse with red banners
                for (int x = 2; x <= 29; x++) for (int y = 1; y <= 20; y++) c.Set(x, y, Brick(x, y, s[0], s[1], s[2], 4));
                for (int x = 2; x <= 29; x++) if ((x - 2) % 4 < 2) { c.Set(x, 21, s[1]); c.Set(x, 22, s[0]); }
                Roof(c, 0, 7, 21, 30, Px.Hex("6e3a4e"), Px.Hex("4e2838"), Px.Hex("36182a"));
                Roof(c, 24, 31, 21, 30, Px.Hex("6e3a4e"), Px.Hex("4e2838"), Px.Hex("36182a"));
                foreach (int bx in new[] { 6, 24 }) { c.Fill(bx, 8, 2, 9, Px.Hex("c8283c")); c.Set(bx, 7, Px.Hex("6e1a2e")); c.Set(bx + 1, 7, Px.Hex("6e1a2e")); c.Set(bx, 14, Gold); c.Set(bx + 1, 14, Gold); }
                c.Fill(4, 17, 2, 2, Px.Hex("ff5050"), true); c.Fill(26, 17, 2, 2, Px.Hex("ff5050"), true); // red windows
                break;
            case Biome.Ashlands: // a volcanic cave
            default:              // a mossy hill cave (Dark Forest)
                c.Ellipse(15.5f, 8f, 15f, 15f, (x, y, dx, dy) =>
                {
                    if (y < 1) return Px.Clear;
                    float l = -dx * 0.5f + dy * 0.8f + (Hash(x, y, 12) - 0.5f) * 0.5f;
                    return l > 0.45f ? s[0] : l > -0.1f ? s[1] : l > -0.6f ? s[2] : s[3];
                });
                if (b == Biome.Ashlands)
                {
                    c.Line(4, 6, 9, 14, Px.Hex("ff7a1e"), 1, true); c.Line(26, 5, 22, 15, Px.Hex("ff7a1e"), 1, true);
                    c.Fill(14, 21, 4, 2, Px.Hex("ff8a2a"), true); // glowing vent on top
                }
                else
                {
                    for (int x = 2; x < 30; x++) for (int y = 14; y < 24; y++)
                        if (!c.Get(x, y).IsClear && Hash(x, y, 3) > 0.55f) c.Set(x, y, Hash(x, y, 4) > 0.5f ? Moss : MossD);
                    c.Line(8, 3, 11, 15, WoodD); c.Line(23, 3, 20, 14, WoodD); // roots
                }
                break;
        }

        // Doorway: dark arch with steps going down into the dark
        for (int x = 11; x <= 20; x++)
            for (int y = 1; y <= 14; y++)
            {
                float dx = (x - 15.5f) / 5f, dy = (y - 10f) / 5f;
                if (y > 10 && dx * dx + dy * dy > 1f) continue;
                c.Set(x, y, Dark);
            }
        for (int i = 0; i < 4; i++) c.HLine(12 + i / 2, 19 - i / 2, 1 + i * 2, Px.Lerp(s[2], Dark, i * 0.25f));
        // Stone frame around the arch
        for (int x = 10; x <= 21; x++)
            for (int y = 1; y <= 16; y++)
            {
                float dx = (x - 15.5f) / 6f, dy = (y - 10f) / 6f;
                bool inOuter = y <= 10 ? (x == 10 || x == 21) : dx * dx + dy * dy <= 1f;
                bool inInner = y <= 10 ? false : ((x - 15.5f) / 5f) * ((x - 15.5f) / 5f) + ((y - 10f) / 5f) * ((y - 10f) / 5f) <= 1f;
                if (inOuter && !inInner && c.Get(x, y).R != Dark.R) c.Set(x, y, y > 13 ? s[0] : s[1]);
            }
        // Torches on either side
        foreach (int tx in new[] { 8, 23 })
        {
            c.VLine(tx, 8, 10, WoodD);
            c.Set(tx, 11, Px.Hex("ffa83a"), true); c.Set(tx, 12, Px.Hex("fff0a0"), true);
        }
        c.Outline();
        return c;
    }
}
