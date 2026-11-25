using System;

/// <summary>
/// All of the game's pixel art, drawn in code (no image files needed).
/// Characters are drawn as text pictures: each letter is a colour from the palette above it,
/// '.' is see-through, and the dark outline is added automatically.
/// Buildings, ground and effects are drawn with simple shapes.
///
/// Every method returns a fresh PixelCanvas; SpriteFactory turns them into Unity sprites (and caches them).
/// </summary>
public static partial class PixelArt
{
    // ---------- Shared colours ----------
    internal static readonly Px StoneL = Px.Hex("cfd2da"), Stone = Px.Hex("a3a7b2"), StoneM = Px.Hex("80848f"), StoneD = Px.Hex("5c5f6b"), StoneDD = Px.Hex("3e404b");
    internal static readonly Px WoodL = Px.Hex("b07a45"), Wood = Px.Hex("8a5a2e"), WoodD = Px.Hex("5e3b1e"), WoodDD = Px.Hex("3e2614");
    internal static readonly Px Gold = Px.Hex("f0c850"), GoldL = Px.Hex("fff1a8"), GoldD = Px.Hex("b8862a");
    internal static readonly Px Moss = Px.Hex("6ea03c"), MossD = Px.Hex("4c7a2a");
    internal static readonly Px Dark = Px.Hex("221a2c");

    /// <summary>Repeatable "random" number 0..1 for a pixel.</summary>
    internal static float Hash(int x, int y, int seed = 0)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * 1274126177;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xffff) / 65535f;
        }
    }

    // =====================================================================
    //  HEROES  (one look per class, facing right; SpriteRenderer.flipX faces them left)
    // =====================================================================

    /// <summary>No class yet: a young adventurer with a short sword.</summary>
    public static PixelCanvas Adventurer() => PixelCanvas.FromArt(new[]
    {
        "...hhhhhh.......",
        "..hHHHHHHh......",
        "..hhsssshh..M...",
        "..hsksskSh..M...",
        "...ssssSS...Mm..",
        "....SssS....Mm..",
        "..tTTTTTTt..Mm..",
        ".tTTTTTTTTt.Mm..",
        ".tTTTTTTTTt.Mm..",
        ".tTTTTTTTTtYYYY.",
        ".slllYlllltsb...",
        "..tTTTTTTt..Y...",
        "..tTTTTTTt......",
        "...pppppp.......",
        "...pp..pp.......",
        "..ddd..ddd......",
    }, "h=6b4423 H=94602f s=f0be96 S=c8906e k=2a1e30 T=3c78d2 t=28508c l=7a4a26 Y=f0c850 p=5a4632 d=3c2a20 M=dfe3ec m=8c94a5 b=7a4a26");

    /// <summary>Knight: plumed helmet, blue tabard, shield and sword.</summary>
    public static PixelCanvas Knight() => PixelCanvas.FromArt(new[]
    {
        ".....RRRr.......",
        "....mMMMMm......",
        "...mMMMMMMm.M...",
        "...mkkkkkkm.M...",
        "...mMMMMMMm.Mm..",
        "....nmmmmn..Mm..",
        "...nMBBBBMn.Mm..",
        "yYYyMBBBBMm.Mm..",
        "YbBYMBYYBMm.Mm..",
        "YbBYMBBBBMmYYYY.",
        "YbBYlllYllmsb...",
        ".yy.MBBBBM..Y...",
        "....mBBBBm......",
        "....nn..nn......",
        "....mn..mn......",
        "...nnn..nnn.....",
    }, "M=d5dbe6 m=969eb0 n=5f6678 k=1e1a26 B=3c64c8 b=283f8c Y=f0c850 y=b8902e R=d23c3c r=962828 l=7a4a26 s=f0be96");

    /// <summary>Ranger: green hood, longbow and a quiver of arrows.</summary>
    public static PixelCanvas Archer() => PixelCanvas.FromArt(new[]
    {
        ".......g........",
        "......gGg.......",
        "..b..gGGGg......",
        ".bw..gGGGGg..R..",
        "b.w.gGsssSg..l..",
        "b.w.gsksskg..l..",
        "b.w..gssssg..l..",
        "b.w.gGGGGGGGgl..",
        "b.wgGLGRGGLGgl..",
        "bssgGLGGRGLGgs..",
        "b.wglllYllllg...",
        "b.w.gGGGGGGGg...",
        ".bw..gGg.gGg....",
        ".bw..gGg.gGg....",
        "..b..ddd.ddd....",
    }, "G=4ea64a g=2f6e34 L=7cc85e s=d8a47c S=a87650 k=2a1e30 b=8a5a2e w=e8e8e8 l=7a4a26 R=c83c3c d=3c2a20 Y=f0c850");

    /// <summary>Pyromancer: purple wizard with a staff topped by a fire orb.</summary>
    public static PixelCanvas Wizard() => PixelCanvas.FromArt(new[]
    {
        ".......p....oOo.",
        "......pPp..oOOOo",
        "......PYP...oOo.",
        ".....pPPPp...b..",
        ".....PPLPP...b..",
        "..pppppppppp.b..",
        "....sksskS...b..",
        "....swwwwS...b..",
        "...PwwwwwwPPsb..",
        "..pPPwwwwPP..b..",
        "..pPPgwwgPPP.b..",
        "..pPPPggPPPP.b..",
        "..pYYYYYYYYp.b..",
        ".pPPPLPPPPPPpb..",
        ".pPPYPPPPYPPpb..",
        ".pPYYYPPYYYPpb..",
        ".pPPYPPPPYPPpb..",
        ".yyyyyyyyyyyyb..",
        "...dd....dd..b..",
    }, "P=6a3fb5 p=45297a L=8d62d6 Y=f0c850 y=b8902e w=f2f2f2 g=c8c8d2 s=f0be96 S=c8906e k=2a1e30 b=8a5a2e o=ff8a30 O=ffe08c d=3c2832",
       glowChars: "oO");

    // =====================================================================
    //  ENEMIES
    // =====================================================================

    static readonly string[] GoblinRows =
    {
        "g..............g",
        "gG............Gg",
        ".gGG..gGGg..GGg.",
        "..gGGgGGGGgGGg..",
        "....GYYGGYYG....",
        "....GYkGGkYG....",
        "....GGGeeGGG....",
        "....GmwmmwmG....",
        ".....gGGGGg.....",
        "....llGGGGll....",
        "...GllGGGGllGM..",
        "...GllllllllGM..",
        "...gbbbybbbbgb..",
        "....llllllll....",
        ".....GG..GG.....",
        ".....GG..GG.....",
        "....ggg..ggg....",
    };

    /// <summary>Grunt: a green goblin with a knife. Its yellow eyes glow in the dark.</summary>
    public static PixelCanvas Goblin() => PixelCanvas.FromArt(GoblinRows,
        "G=7cb840 g=4c8a2c e=a6d86a Y=f2dc3a k=1e1a26 w=f0f0e6 m=4a1e22 l=8a5a32 b=5a3a20 y=d8aa3c M=c8ccd6", glowChars: "Y");

    /// <summary>Runner: a scrawny yellow-green goblin scout in a red vest.</summary>
    public static PixelCanvas GoblinScout() => PixelCanvas.FromArt(GoblinRows,
        "G=b4c84a g=7a8e2a e=dcea7a Y=ff8a3a k=1e1a26 w=f0f0e6 m=4a1e22 l=b43c32 b=6e2020 y=d8aa3c M=c8ccd6", glowChars: "Y");

    /// <summary>Brute: a mossy stone golem with a glowing cracked core.</summary>
    public static PixelCanvas Golem() => PixelCanvas.FromArt(new[]
    {
        ".......vLLLLv.......",
        ".......LMMMMm.......",
        ".......McMMcm.......",
        ".......MMMMMm.......",
        "...vLLLmmmmmmLLv....",
        "..LLMMMLMMMMLMMMMm..",
        ".LMMMMmMMMMMMmMMMMm.",
        ".MMMmMMMLMMLMMMmMMm.",
        ".MMm.mMMMccMMMm.mMm.",
        ".MMm.mMMcCCcMMm.mMm.",
        ".MMm.mMMMccMMMm.mMm.",
        ".MMm.mMMMMMMMMm.mMm.",
        "LMMMm.nmmmmmmn..LMMm",
        "MMMMm.MMMm.MMMm.MMMm",
        "mmmm..MMMm.MMMm.mmmm",
        "......MMMm.MMMm.....",
        ".....LMMMm.LMMMm....",
        ".....mmmmm.mmmmm....",
    }, "M=a3a8b2 m=767b86 n=50555f L=cdd1d9 c=64dcff C=e6ffff v=6ea03c", glowChars: "cC");

    // =====================================================================
    //  BUILDINGS
    // =====================================================================

    /// <summary>Stone with a brick pattern (bricks 4 wide, 3 tall), lit from the top-left.</summary>
    internal static Px Brick(int x, int y, Px light, Px mid, Px dark, int seed)
    {
        int row = y / 3;
        int bx = x + (row % 2) * 2;
        if (y % 3 == 0) return dark;           // mortar row
        if (bx % 4 == 0) return dark;          // mortar column
        float h = Hash(bx / 4, row, seed);
        Px c = h < 0.3f ? mid : h > 0.85f ? light : Px.Lerp(mid, light, 0.5f);
        if (y % 3 == 2) c = Px.Lerp(c, light, 0.35f); // top edge of each brick catches light
        return c;
    }

    /// <summary>A cone roof made of shingles, light on the left, dark on the right.</summary>
    internal static void Roof(PixelCanvas c, int left, int right, int bottom, int top, Px light, Px mid, Px dark)
    {
        float cx = (left + right) / 2f;
        for (int y = bottom; y <= top; y++)
        {
            float t = (y - bottom) / (float)(top - bottom + 1);
            float half = (right - left + 1) / 2f * (1f - t);
            for (int x = left; x <= right; x++)
            {
                if (Math.Abs(x + 0.5f - (cx + 0.5f)) > half) continue;
                Px col = x + 0.5f < cx + 0.5f - half * 0.25f ? light : x + 0.5f > cx + 0.5f + half * 0.45f ? dark : mid;
                if ((y - bottom) % 2 == 0 && (x + y) % 2 == 0) col = col.Shade(0.85f); // shingle texture
                c.Set(x, y, col);
            }
        }
        c.HLine(left, right, bottom, dark.Shade(0.8f)); // eave shadow
    }

    /// <summary>The hub: a little castle with two towers, a keep and a gate. 32x32 (2x2 tiles).</summary>
    public static PixelCanvas Hub()
    {
        var c = new PixelCanvas(32, 32);
        Px roofL = Px.Hex("6e96e6"), roof = Px.Hex("3c64c8"), roofD = Px.Hex("283f8c");

        // Keep (behind everything)
        for (int x = 11; x <= 20; x++) for (int y = 12; y <= 22; y++) c.Set(x, y, Brick(x, y, StoneL, Stone, StoneM, 3));
        Roof(c, 10, 21, 23, 29, roofL, roof, roofD);
        c.VLine(15, 29, 31, WoodD);
        c.Fill(16, 29, 4, 2, Px.Hex("d23c3c"));
        c.Set(16, 30, Px.Hex("ff6a5a"));
        c.Fill(15, 18, 2, 3, Px.Hex("ffc864"), glow: true);
        c.HLine(15, 16, 21, Dark);

        // Side towers
        foreach (int tx in new[] { 1, 23 })
        {
            for (int x = tx; x < tx + 8; x++)
                for (int y = 1; y <= 19; y++)
                {
                    Px b = Brick(x, y, StoneL, Stone, StoneM, tx);
                    if (x == tx + 7) b = b.Shade(0.8f);           // shaded right side
                    if (x == tx) b = Px.Lerp(b, StoneL, 0.3f);
                    c.Set(x, y, b);
                }
            Roof(c, tx - 1, tx + 8, 20, 28, roofL, roof, roofD);
            c.Fill(tx + 3, 11, 2, 3, Px.Hex("ffc864"), glow: true); // window
            c.HLine(tx + 3, tx + 4, 14, Dark);
        }

        // Curtain wall with battlements
        for (int x = 9; x <= 22; x++)
        {
            for (int y = 1; y <= 12; y++) c.Set(x, y, Brick(x, y, StoneL, Stone, StoneM, 7));
            c.Set(x, 13, StoneL);
            if ((x - 9) % 4 < 2) { c.Set(x, 14, Stone); c.Set(x, 15, StoneL); }
        }

        // Gate: wooden doors under a stone arch
        for (int x = 13; x <= 18; x++)
            for (int y = 1; y <= 8; y++)
            {
                if (y == 8 && (x == 13 || x == 18)) continue;
                Px w = (x == 15 || x == 16) ? WoodD : (x % 2 == 0 ? Wood : WoodL);
                if (y == 3 || y == 6) w = StoneDD;     // iron bands
                c.Set(x, y, w);
            }
        c.HLine(14, 17, 9, StoneD);
        c.Set(13, 8, StoneD); c.Set(18, 8, StoneD);
        c.Set(15, 4, Gold); c.Set(16, 4, Gold);           // door rings

        // Banners on the wall
        foreach (int bx in new[] { 10, 20 })
        {
            c.Fill(bx, 5, 2, 6, Px.Hex("3c64c8"));
            c.Set(bx, 4, Px.Hex("283f8c")); c.Set(bx + 1, 4, Px.Hex("283f8c"));
            c.Set(bx, 8, Gold); c.Set(bx + 1, 8, Gold);
        }

        // Moss along the bottom
        for (int x = 1; x <= 30; x++)
            if (Hash(x, 0, 11) > 0.55f) c.Set(x, 1, Hash(x, 1, 11) > 0.5f ? Moss : MossD);

        c.Outline();
        return c;
    }

    /// <summary>Defense tower: a stone turret with a glowing crystal on top. 16x26, base sits on its tile.</summary>
    public static PixelCanvas Tower()
    {
        var c = new PixelCanvas(16, 26) { PivotY = 8f / 26f };
        // Base
        for (int x = 2; x <= 13; x++) for (int y = 1; y <= 3; y++) c.Set(x, y, Brick(x, y, Stone, StoneM, StoneD, 5));
        // Body
        for (int x = 3; x <= 12; x++)
            for (int y = 4; y <= 15; y++)
            {
                Px b = Brick(x, y, StoneL, Stone, StoneM, 9);
                if (x >= 11) b = b.Shade(0.8f);
                if (x == 3) b = Px.Lerp(b, StoneL, 0.4f);
                c.Set(x, y, b);
            }
        // Arrow slit
        c.Fill(7, 8, 2, 4, Dark);
        c.Set(7, 11, StoneDD);
        // Platform and battlements
        c.HLine(2, 13, 16, StoneL);
        c.HLine(2, 13, 15, StoneD);
        foreach (int mx in new[] { 2, 6, 10 })
        {
            c.Fill(mx, 17, mx == 6 ? 4 : 4, 2, Stone);
            c.HLine(mx, mx + 3, 18, StoneL);
        }
        // Crystal on top
        Px cr = Px.Hex("64dcff"), crL = Px.Hex("e6ffff"), crD = Px.Hex("2a8ac8");
        int[] widths = { 2, 4, 4, 4, 2, 2 };
        for (int i = 0; i < widths.Length; i++)
        {
            int y = 19 + i, w = widths[i], x0 = 8 - w / 2;
            for (int x = x0; x < x0 + w; x++) c.Set(x, y, x < 8 ? cr : crD, glow: true);
        }
        c.Set(7, 22, crL, glow: true); c.Set(7, 23, crL, glow: true);
        c.Outline();
        return c;
    }

    /// <summary>Miner: a timber-framed mine shaft in a rocky mound, with a cart of gold.</summary>
    public static PixelCanvas Miner()
    {
        var c = new PixelCanvas(16, 16);
        // Rocky mound
        c.Ellipse(7.5f, 4f, 7f, 7.5f, (x, y, dx, dy) =>
        {
            if (y < 1) return Px.Clear;
            float l = -dx * 0.6f + dy * 0.8f + (Hash(x, y, 4) - 0.5f) * 0.5f;
            return l > 0.45f ? StoneL : l > -0.1f ? Stone : l > -0.6f ? StoneM : StoneD;
        });
        // Dark entrance
        c.Fill(5, 1, 6, 7, Dark);
        c.HLine(6, 9, 8, Dark);
        // Timber frame
        c.VLine(4, 1, 9, Wood); c.VLine(11, 1, 9, WoodD);
        c.HLine(3, 12, 10, WoodL); c.HLine(3, 12, 9, WoodD);
        // Lantern
        c.Set(10, 7, WoodDD); c.Fill(10, 5, 1, 2, Px.Hex("ffc864"), glow: true);
        // Cart full of gold
        c.Fill(1, 1, 5, 3, WoodD);
        c.HLine(1, 5, 3, Wood);
        c.Set(2, 0, StoneDD); c.Set(4, 0, StoneDD);
        c.HLine(1, 5, 4, Gold); c.Set(2, 5, Gold); c.Set(3, 5, GoldL); c.Set(4, 5, Gold);
        c.Set(2, 4, GoldL);
        c.Outline();
        return c;
    }

    /// <summary>Gold ore: grey boulders with shiny gold nuggets (the bright bits glow so you can find them).</summary>
    public static PixelCanvas Ore()
    {
        var c = new PixelCanvas(16, 16);
        Func<int, int, float, float, Px> rock = (x, y, dx, dy) =>
        {
            float l = -dx * 0.55f + dy * 0.8f + (Hash(x, y, 8) - 0.5f) * 0.35f;
            return l > 0.5f ? StoneL : l > 0f ? Stone : l > -0.55f ? StoneM : StoneD;
        };
        c.Ellipse(10.5f, 6f, 4f, 4f, rock);
        c.Ellipse(5.5f, 5.5f, 4.5f, 4.5f, rock);
        c.Ellipse(8f, 3f, 4.5f, 2.5f, rock);
        foreach (var (gx, gy) in new[] { (4, 6), (9, 4), (11, 8), (6, 2) })
        {
            c.Set(gx, gy, Gold); c.Set(gx + 1, gy, GoldD); c.Set(gx, gy - 1, GoldD);
            c.Set(gx, gy + 1, GoldL, glow: true);
        }
        c.Outline();
        return c;
    }

    // =====================================================================
    //  GROUND
    // =====================================================================

    static readonly Px[] GrassLight = { Px.Hex("6cb04a"), Px.Hex("62a444"), Px.Hex("7cc056"), Px.Hex("538f3a") };
    static readonly Px[] GrassDark = { Px.Hex("5a9a40"), Px.Hex("528e3a"), Px.Hex("68a84a"), Px.Hex("467c32") };

    /// <summary>
    /// A grass tile. tone 0 = light, 1 = dark. decor: 0 plain, 1 tufts, 2 flowers, 3 pebbles.
    /// </summary>
    public static PixelCanvas Grass(int tone, int decor, int seed)
    {
        var c = new PixelCanvas(16, 16);
        var pal = tone == 0 ? GrassLight : GrassDark;
        var rng = new Random(seed * 31 + tone * 7 + decor);
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
            {
                float n = Hash(x, y, seed);
                c.Set(x, y, n < 0.55f ? pal[0] : n < 0.85f ? pal[1] : n < 0.94f ? pal[3] : pal[2]);
            }
        // Little blades of grass everywhere
        for (int i = 0; i < 5; i++)
        {
            int x = rng.Next(1, 15), y = rng.Next(0, 14);
            c.Set(x, y, pal[3]); c.Set(x, y + 1, pal[2]);
        }
        switch (decor)
        {
            case 1: // tufts
                for (int i = 0; i < 2; i++)
                {
                    int x = rng.Next(2, 13), y = rng.Next(1, 12);
                    c.Set(x, y, pal[3]); c.Set(x + 1, y, pal[3]); c.Set(x + 2, y, pal[3]);
                    c.Set(x, y + 1, pal[2]); c.Set(x + 2, y + 1, pal[2]); c.Set(x + 1, y + 2, pal[2]);
                    c.Set(x - 1, y + 2, pal[2]); c.Set(x + 3, y + 2, pal[2]);
                }
                break;
            case 2: // flowers
                Px[] petals = { Px.Hex("f4f4f4"), Px.Hex("f2d64a"), Px.Hex("c87ae6"), Px.Hex("ff8a8a") };
                for (int i = 0; i < 3; i++)
                {
                    int x = rng.Next(2, 14), y = rng.Next(2, 14);
                    Px p = petals[rng.Next(petals.Length)];
                    c.Set(x - 1, y, p); c.Set(x + 1, y, p); c.Set(x, y + 1, p); c.Set(x, y - 1, pal[3]);
                    c.Set(x, y, Px.Hex("f2c040"));
                }
                break;
            case 3: // pebbles
                for (int i = 0; i < 2; i++)
                {
                    int x = rng.Next(2, 13), y = rng.Next(2, 13);
                    c.Set(x, y, StoneM); c.Set(x + 1, y, StoneD); c.Set(x, y + 1, StoneL); c.Set(x + 1, y + 1, Stone);
                    c.Set(x, y - 1, pal[3]); c.Set(x + 1, y - 1, pal[3]);
                }
                break;
        }
        return c;
    }

    /// <summary>A tree for the forest border. variant 0 = round oak, 1 = pine.</summary>
    public static PixelCanvas Tree(int variant)
    {
        var c = new PixelCanvas(16, 16);
        Px leafL = Px.Hex("5aa848"), leaf = Px.Hex("3a8a3c"), leafD = Px.Hex("27663a"), leafDD = Px.Hex("1b4a2e");

        if (variant == 0)
        {
            c.Fill(7, 1, 2, 4, Wood); c.VLine(8, 1, 4, WoodD); c.Set(6, 1, WoodD); c.Set(9, 1, WoodD);
            Func<int, int, float, float, Px> shade = (x, y, dx, dy) =>
            {
                float l = -dx * 0.5f + dy * 0.85f + (Hash(x, y, 2) - 0.5f) * 0.45f;
                return l > 0.55f ? leafL : l > 0f ? leaf : l > -0.6f ? leafD : leafDD;
            };
            c.Ellipse(7.5f, 7f, 6f, 3.4f, shade);
            c.Ellipse(5f, 10f, 3.6f, 3.4f, shade);
            c.Ellipse(10f, 10.5f, 3.6f, 3.2f, shade);
            c.Ellipse(7.5f, 12f, 3.5f, 2.6f, shade);
            // a couple of red apples
            c.Set(4, 8, Px.Hex("d84a3a")); c.Set(11, 9, Px.Hex("d84a3a"));
        }
        else
        {
            c.Fill(7, 1, 2, 2, Wood); c.Set(8, 1, WoodD);
            int[] widths = { 13, 11, 9, 11, 9, 7, 7, 5, 3, 1 };
            for (int i = 0; i < widths.Length; i++)
            {
                int y = 3 + i, w = widths[i], x0 = 8 - (w + 1) / 2;
                for (int x = x0; x < x0 + w; x++)
                {
                    float rel = (x - x0) / (float)Math.Max(1, w - 1);
                    Px p = rel < 0.3f ? leafL : rel < 0.7f ? leaf : leafD;
                    if (y == 3 || y == 6 || y == 9) p = leafDD.Shade(1.2f); // bottom edge of each layer
                    c.Set(x, y, p);
                }
            }
        }
        c.Outline(Px.Hex("142a1e"));
        // soft shadow on the ground
        for (int x = 3; x <= 12; x++)
            if (c.Get(x, 0).IsClear) c.Set(x, 0, new Px(10, 30, 15, 90));
        return c;
    }

    // =====================================================================
    //  EFFECTS & PROJECTILES
    // =====================================================================

    public static PixelCanvas Torch()
    {
        var c = PixelCanvas.FromArt(new[]
        {
            "..o..",
            ".oOo.",
            ".OYO.",
            "oOYOo",
            ".oOo.",
            "..b..",
            "..B..",
            "..b..",
            "..b..",
            "..B..",
        }, "o=f06a1e O=ffa83a Y=fff0a0 b=8a5a2e B=5e3b1e", glowChars: "oOY");
        c.PivotY = 0.25f;
        return c;
    }

    /// <summary>White sword-swing arc pointing right.</summary>
    public static PixelCanvas Slash()
    {
        var c = new PixelCanvas(20, 28);
        for (int x = 0; x < 20; x++)
            for (int y = 0; y < 28; y++)
            {
                float d = (float)Math.Sqrt(x * x + (y - 13.5) * (y - 13.5));
                float inner = (float)Math.Sqrt((x + 6) * (x + 6) + (y - 13.5) * (y - 13.5));
                if (d < 14f && inner > 17.5f)
                {
                    float edge = 14f - d;
                    c.Set(x, y, edge < 1.5f ? new Px(255, 255, 255, 240) : edge < 3f ? new Px(200, 230, 255, 200) : new Px(150, 200, 255, 110));
                }
            }
        return c;
    }

    public static PixelCanvas Arrow() => PixelCanvas.FromArt(new[]
    {
        "ww.......M..",
        ".wbbbbbbbMMM",
        "ww.......M..",
    }, "w=f0f0f0 b=a8743a M=dfe3ec");

    /// <summary>Tower shot: a little glowing crystal shard.</summary>
    public static PixelCanvas Bolt() => PixelCanvas.FromArt(new[]
    {
        ".c.",
        "cWc",
        ".c.",
    }, "c=64dcff W=ffffff", glowChars: "cW", outline: false);

    public static PixelCanvas Fireball() => PixelCanvas.FromArt(new[]
    {
        "..oo..",
        ".oOOo.",
        "oOYYOo",
        "oOYYOo",
        ".oOOo.",
        "..oo..",
    }, "o=e8481e O=ff9a30 Y=fff2a8", glowChars: "oOY");

    /// <summary>Gold diamond showing a building's level.</summary>
    public static PixelCanvas LevelPip() => PixelCanvas.FromArt(new[]
    {
        ".Y.",
        "YWY",
        ".Y.",
    }, "Y=f0c850 W=fff6c8", glowChars: "YW");

    // =====================================================================
    //  HUD ICONS
    // =====================================================================

    public static PixelCanvas IconCoin() => PixelCanvas.FromArt(new[]
    {
        "..ooo..",
        ".oYYYo.",
        "oYWYYyo",
        "oYYYYyo",
        "oYYYyyo",
        ".oyyyo.",
        "..ooo..",
    }, "o=b8862a Y=f0c850 W=fff6c8 y=d8a83a");

    public static PixelCanvas IconHeart() => PixelCanvas.FromArt(new[]
    {
        ".RR.RR.",
        "RWRRRRr",
        "RRRRRRr",
        ".RRRRr.",
        "..RRr..",
        "...r...",
    }, "R=e04848 r=a82c3a W=ffb0b0");

    public static PixelCanvas IconSkull() => PixelCanvas.FromArt(new[]
    {
        ".WWWWW.",
        "WWWWWWg",
        "WkkWkkg",
        "WkkWkkg",
        ".WWkWg.",
        ".W.W.g.",
    }, "W=eeeef2 g=b4b4c0 k=2a1e30");

    public static PixelCanvas IconHourglass() => PixelCanvas.FromArt(new[]
    {
        "bbbbb",
        ".sYs.",
        "..Y..",
        ".s.s.",
        ".YYY.",
        "bbbbb",
    }, "b=8a5a2e s=cfe6f2 Y=f0c850");

    public static PixelCanvas IconStar() => PixelCanvas.FromArt(new[]
    {
        "...Y...",
        "..YWY..",
        "YYYYYYY",
        ".YYYYy.",
        ".YY.Yy.",
        "Yy...yY",
    }, "Y=5ec8ff W=d8f4ff y=3a8ad0");
}
