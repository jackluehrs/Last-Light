using System;

/// <summary>The kinds of ore. Gold is money; the others come from the outer lands and unlock better things.</summary>
public enum OreType { Gold, Iron, SoulCrystal, Bloodstone, Emberite }

/// <summary>Monsters, bosses, towers and ores for the outer lands (same style as PixelArt.cs).</summary>
public static partial class PixelArt
{
    // =====================================================================
    //  BIOME ENEMIES
    // =====================================================================

    /// <summary>Dark Forest: a grey wolf (side view, facing right). Fast, hunts in packs.</summary>
    public static PixelCanvas Wolf() => PixelCanvas.FromArt(new[]
    {
        "............g.g...",
        "...........gGgGg..",
        "...........GGGGGGk",
        "g..........GYGGWW.",
        "Gg.gggggggGGGGWw..",
        ".GgGGGGGGGGGGGw...",
        "..GGGGGGGGGGGW....",
        "..GGGGGGGGGGW.....",
        "..wGGgwwwwGGw.....",
        "..GG.G....GG.G....",
        "..GG.G....GG.G....",
        "..dd.d....dd.d....",
    }, "G=8a8f9e g=5e6372 W=d4d7de w=b0b4bf k=1e1a26 Y=f2dc3a d=4a4e5a", glowChars: "Y");

    /// <summary>Dark Forest: a giant purple spider. Its bite slows you down.</summary>
    public static PixelCanvas Spider() => PixelCanvas.FromArt(new[]
    {
        "......pPPp......",
        ".....pPLLPp.....",
        ".k..pPPLPPPp..k.",
        "..k.pPPOPPPp.k..",
        "...kpPOOOPPpk...",
        "kk..kpPOPPpk..kk",
        "..kk.kpPPpk.kk..",
        "....kkRpRpkk....",
        "..kk.kpRpRk.kk..",
        "kk..k.kwkwk.k..kk",
        ".......w.w......",
    }, "p=4a2a5e P=6e3e8a L=9a6ab8 O=e86a2a k=5a3a70 R=ff3a3a w=e8e4d4", glowChars: "R");

    /// <summary>Bloodmoor: a bat. Very fast, very fragile.</summary>
    public static PixelCanvas Bat() => PixelCanvas.FromArt(new[]
    {
        ".....P....P.....",
        ".....PP..PP.....",
        "P....PRPPRP....P",
        "PP..mPPPPPPm..PP",
        "PmmmmmPwwPmmmmmP",
        ".PmmmmPPPPmmmmP.",
        "..Pm.mmPPmm.mP..",
        "...P..m..m..P...",
    }, "P=5a3a6e m=3e2650 R=ff3a3a w=f0f0e6", glowChars: "R");

    /// <summary>Bloodmoor: a vampire. Heals itself with every bite.</summary>
    public static PixelCanvas Vampire() => PixelCanvas.FromArt(new[]
    {
        "....hhhhhh......",
        "...hhhhhhhh.....",
        "...hhsssshh.....",
        "...hsEssEsh.....",
        "..CCsswwssCC....",
        ".CRRCkSSkCRRC...",
        "CRRRCkWWkCRRRC..",
        "CRRRkkWWkkRRRC..",
        "CRRkkkkkkkkRRC..",
        "CRRkkkkkkkkRRC..",
        "CRskkkkkkkksRC..",
        "CRRkkkkkkkkRRC..",
        "CRRkkkkkkkkRRC..",
        "CCRkkkkkkkkRCC..",
        ".CCkkkkkkkkCC...",
        "..C.kk..kk.C....",
        "....dd..dd......",
    }, "h=2a2238 s=dcd6e6 S=b4aac8 E=ff3030 w=f8f8f8 C=6e1a2e R=c8283c k=3a3048 W=e6e6ee d=2a2238", glowChars: "E");

    /// <summary>Crypt: a skeleton that throws bones from a distance.</summary>
    public static PixelCanvas Skeleton() => PixelCanvas.FromArt(new[]
    {
        "....wWWWWw......",
        "...wWWWWWWw.....",
        "...WkEWWEkW.....",
        "...WkkWWkkW.....",
        "....WWkkWW......",
        "....wkWkWw......",
        ".....wwww.......",
        "......WW.....W..",
        "..WWWWWWWWWWWW..",
        "..W.WkWWkW.W.W..",
        ".W..WWWWWW..WW..",
        ".W..WkWWkW..W...",
        ".W...WWWW.......",
        "WW..wWWWWw......",
        ".....W..W.......",
        ".....W..W.......",
        "....WW..WW......",
    }, "W=e8e4d4 w=b8b29c k=2a1e30 E=6affd0", glowChars: "E");

    /// <summary>Crypt: a ghost. Drifts through everything; towers have a hard time hitting it.</summary>
    public static PixelCanvas Ghost() => PixelCanvas.FromArt(new[]
    {
        "....GGGGGG....",
        "...GGGGGGGG...",
        "..GGGGGGGGGG..",
        ".GGGkkGGkkGGG.",
        ".GGGkkGGkkGGG.",
        ".GGGGGGGGGGGG.",
        ".GGGGGkkGGGGG.",
        "GGGGGGkkGGGGGG",
        "GgGGGGGGGGGGgG",
        ".GGGGGGGGGGGG.",
        ".GGGGGGGGGGGG.",
        ".gGGGGGGGGGGg.",
        ".gGGgGGGgGGGg.",
        "..gG.gGG.gGg..",
        "..g...gg...g..",
    }, "G=dcf0ff g=96c8e6 k=2a3a5a", glowChars: "Gg", outline: false);

    /// <summary>Ashlands: a little fire imp that throws fireballs.</summary>
    public static PixelCanvas Imp() => PixelCanvas.FromArt(new[]
    {
        ".h........h...",
        ".hh......hh...",
        "..hRRRRRRh....",
        "..RRRRRRRR....",
        "..RYYRRYYR....",
        "..RRRwwRRR....",
        "...rRRRRr.....",
        "m.RRRRRRRR.m..",
        "mmRrRRRRrRmm..",
        "m..RRRRRR..m..",
        "...rRRRRr.....",
        "...RR..RR.....",
        "..rr....rr....",
    }, "R=d23c2c r=962020 h=f0e0c0 Y=ffe03a w=f0f0e6 m=6e1a1a", glowChars: "Y");

    /// <summary>Ashlands: a slow blob of cooling magma.</summary>
    public static PixelCanvas MagmaSlime() => PixelCanvas.FromArt(new[]
    {
        ".....kkkkkk.....",
        "...kkKKKKKKkk...",
        "..kKKOKKKKKKKk..",
        ".kKKOOKKKKOKKKk.",
        ".KKKKYKKKKYKKKK.",
        "kKKKKYKKKKYKKKKk",
        "KKOKKKKKKKKKOKKK",
        "KKOOKKKOOKKKOKKK",
        "KKKOKKOOOOKKKKKK",
        "kKKKKKKOKKKKKKKk",
        ".kKKKKKKKKKKKKk.",
        "..kkkkkkkkkkkk..",
    }, "K=5a4646 k=3a2c2c O=ff7a1e Y=ffe066", glowChars: "OY");

    // =====================================================================
    //  BOSSES
    // =====================================================================

    /// <summary>Dark Forest boss: the Spider Queen, crowned, with a glowing hourglass mark.</summary>
    public static PixelCanvas SpiderQueen()
    {
        var c = new PixelCanvas(28, 22);
        Px leg = Px.Hex("3a2048"), legL = Px.Hex("6e3e8a");
        // Legs: 4 a side, each a bent line from the body out to a foot.
        for (int i = 0; i < 4; i++)
        {
            int sy = 8 + i * 2;
            foreach (int side in new[] { -1, 1 })
            {
                int x0 = 14 + side * 4, kx = 14 + side * (10 + i % 2), ky = sy + 4 - i, fx = 14 + side * (12 + (i == 1 || i == 2 ? 1 : 0)), fy = 1 + i * 3;
                c.Line(x0, sy, kx, ky, leg, 2);
                c.Line(kx, ky, fx, fy, leg);
                c.Set(kx, ky + 1, legL);
            }
        }
        // Abdomen (behind) and head (in front)
        c.Ellipse(14f, 14f, 8f, 6.5f, (x, y, dx, dy) =>
        {
            float l = -dx * 0.5f + dy * 0.8f;
            return l > 0.5f ? Px.Hex("9a6ab8") : l > -0.1f ? Px.Hex("6e3e8a") : Px.Hex("4a2a5e");
        });
        // Glowing hourglass on her back
        foreach (var (x, y) in new[] { (13, 16), (14, 16), (15, 16), (14, 15), (14, 14), (13, 13), (14, 13), (15, 13) })
            c.Set(x, y, Px.Hex("ff5a3a"), glow: true);
        c.Ellipse(14f, 6.5f, 5f, 3.8f, (x, y, dx, dy) => dy > 0.3f - dx * 0.3f ? Px.Hex("6e3e8a") : Px.Hex("4a2a5e"));
        // Six red eyes
        foreach (var (x, y) in new[] { (11, 7), (13, 8), (15, 8), (17, 7), (12, 6), (16, 6) })
            c.Set(x, y, Px.Hex("ff3a3a"), glow: true);
        // Fangs
        c.VLine(12, 1, 3, Px.Hex("e8e4d4")); c.VLine(16, 1, 3, Px.Hex("e8e4d4"));
        // Crown
        for (int x = 11; x <= 17; x++) c.Set(x, 10, Gold);
        foreach (int x in new[] { 11, 14, 17 }) { c.Set(x, 11, Gold); c.Set(x, 12, GoldL); }
        c.Set(14, 11, Px.Hex("c83c3c"));
        c.Outline();
        return c;
    }

    /// <summary>Bloodmoor boss: the Vampire Lord, with a crown and a cape like wings.</summary>
    public static PixelCanvas VampireLord() => PixelCanvas.FromArt(new[]
    {
        "........Y.YY.Y........",
        "........YYYYYY........",
        ".......hhhhhhhh.......",
        ".......hsEssEsh.......",
        ".......hssssssh.......",
        "C......CsswwssC......C",
        "CC....CRCkSSkCRC....CC",
        "CRC..CRRCkWWkCRRC..CRC",
        "CRRCCRRRkkWWkkRRRCCRRC",
        "CRRRRRRkkkYYkkkRRRRRRC",
        "CRRRRRRkkkkkkkkRRRRRRC",
        ".CRRRRskkkkkkkksRRRRC.",
        ".CRRRRRkkkkkkkkRRRRRC.",
        "..CRRRRkkkkkkkkRRRRC..",
        "..CRRRCkkkkkkkkCRRRC..",
        "...CRRCkkkkkkkkCRRC...",
        "...CRC.kkkkkkkk.CRC...",
        "....C...kkkkkk...C....",
        "........kk..kk........",
        "........dd..dd........",
    }, "h=2a2238 s=dcd6e6 S=b4aac8 E=ff3030 w=f8f8f8 C=6e1a2e R=c8283c k=3a3048 W=e6e6ee d=2a2238 Y=f0c850",
       glowChars: "E");

    /// <summary>Crypt boss: the Lich King, a crowned skeleton mage with a soul-fire staff.</summary>
    public static PixelCanvas Lich() => PixelCanvas.FromArt(new[]
    {
        "......Y.Y.Y.....gGg.",
        "......YYYYY....gGGGg",
        ".....wWWWWWw....gGg.",
        ".....WkEWEkW.....b..",
        ".....WWWkWWW.....b..",
        "......WkWkW......b..",
        "...pPPPwwwPPPp...b..",
        "..pPPLPPYPPLPPp..b..",
        ".pPPPLPPPPPLPPPpWb..",
        ".pPPLLPPPPPLLPPp.b..",
        ".pPPPLPPPPPLPPPp.b..",
        ".WPPPLPPPPPLPPPp.b..",
        "..pPPLPPPPPLPPp..b..",
        "..pPPLPPYPPLPPp..b..",
        "..pPPLPPPPPLPPp..b..",
        ".pPPPLPPPPPLPPPp.b..",
        ".pPPLPPPPPPPLPPp.b..",
        "pPPPLPPPPPPPLPPPpb..",
        "pyyyyyyyyyyyyyyypb..",
        ".g..g..g..g..g...b..",
    }, "P=5a2e8a p=3a1e5e L=7a4ab0 W=e8e4d4 w=b8b29c k=1e1a26 E=7affb0 Y=f0c850 y=b8902e G=b0ffd0 g=4adc8a b=6e5a3e",
       glowChars: "EGg");

    /// <summary>Ashlands boss: the Rock Giant, a walking mountain with lava in its cracks.</summary>
    public static PixelCanvas RockGiant()
    {
        var c = new PixelCanvas(26, 26);
        Px rL = Px.Hex("b08a62"), r = Px.Hex("8c6a48"), rD = Px.Hex("6a4e34"), rDD = Px.Hex("4a3424");
        Func<int, int, float, float, Px> rock = (x, y, dx, dy) =>
        {
            float l = -dx * 0.55f + dy * 0.75f + (Hash(x, y, 31) - 0.5f) * 0.5f;
            return l > 0.55f ? rL : l > 0f ? r : l > -0.55f ? rD : rDD;
        };
        c.Ellipse(9f, 3f, 3f, 3f, rock);   // legs
        c.Ellipse(17f, 3f, 3f, 3f, rock);
        c.Ellipse(3.5f, 10f, 3f, 6f, rock); // arms
        c.Ellipse(22.5f, 10f, 3f, 6f, rock);
        c.Ellipse(3.5f, 4f, 3.2f, 2.6f, rock); // fists
        c.Ellipse(22.5f, 4f, 3.2f, 2.6f, rock);
        c.Ellipse(13f, 11.5f, 8.5f, 7.5f, rock); // body
        c.Ellipse(13f, 20f, 4.5f, 3.6f, rock);   // head
        // Glowing eyes and lava cracks
        Px lava = Px.Hex("ff8a2a"), lavaL = Px.Hex("ffd060");
        c.Set(11, 20, lavaL, true); c.Set(15, 20, lavaL, true);
        void Crack(int x0, int y0, int x1, int y1) { c.Line(x0, y0, x1, y1, lava, 1, true); }
        Crack(9, 16, 10, 12); Crack(10, 12, 8, 8);
        Crack(16, 15, 15, 11); Crack(15, 11, 17, 7); Crack(17, 7, 16, 5);
        Crack(12, 9, 13, 6);
        Crack(3, 13, 4, 9); Crack(22, 12, 23, 8);
        c.Set(10, 12, lavaL, true); c.Set(15, 11, lavaL, true);
        // Moss and a little sapling growing on its shoulder (like the reference)
        foreach (var (x, y) in new[] { (5, 16), (6, 16), (6, 17), (19, 17), (20, 17), (20, 16), (12, 23), (13, 23) })
            c.Set(x, y, Hash(x, y, 2) > 0.5f ? Moss : MossD);
        c.VLine(21, 18, 22, WoodD);
        c.Fill(20, 22, 3, 2, Moss); c.Set(21, 24, Px.Hex("8cc850"));
        c.Outline();
        return c;
    }

    // =====================================================================
    //  TOWERS  (all 16 wide; tall ones stand on their tile like the Crystal Tower)
    // =====================================================================

    static void TowerBody(PixelCanvas c, int top, Px light, Px mid, Px dark, int seed)
    {
        for (int x = 2; x <= 13; x++) for (int y = 1; y <= 3; y++) c.Set(x, y, Brick(x, y, mid, dark, dark.Shade(0.8f), seed));
        for (int x = 3; x <= 12; x++)
            for (int y = 4; y <= top; y++)
            {
                Px b = Brick(x, y, light, mid, dark, seed + 1);
                if (x >= 11) b = b.Shade(0.8f);
                if (x == 3) b = Px.Lerp(b, light, 0.4f);
                c.Set(x, y, b);
            }
    }

    /// <summary>Cannon: a squat stone bastion with an iron cannon. Slow, but the shells explode.</summary>
    public static PixelCanvas Cannon()
    {
        var c = new PixelCanvas(16, 20) { PivotY = 8f / 20f };
        TowerBody(c, 10, StoneL, Stone, StoneM, 41);
        c.HLine(2, 13, 11, StoneL); c.HLine(2, 13, 10, StoneD);
        foreach (int mx in new[] { 2, 11 }) { c.Fill(mx, 12, 3, 2, Stone); c.HLine(mx, mx + 2, 13, StoneL); }
        // Wooden carriage and iron barrel
        c.Fill(5, 12, 6, 2, Wood); c.HLine(5, 10, 12, WoodD);
        Px iron = Px.Hex("4a4e5a"), ironL = Px.Hex("7a8090"), ironD = Px.Hex("2a2c34");
        for (int x = 5; x <= 14; x++) { c.Set(x, 14, iron); c.Set(x, 15, ironL); c.Set(x, 16, iron); }
        c.Fill(4, 14, 2, 3, ironD); c.VLine(14, 13, 17, ironD); c.Set(15, 15, Dark);
        c.HLine(7, 8, 17, ironD);
        // A little pile of cannonballs
        c.Set(4, 4, ironD); c.Set(5, 4, ironD); c.Set(4, 5, iron);
        c.Outline();
        return c;
    }

    /// <summary>Frost Spire: pale stone with an ice crystal cluster on top. Slows everything nearby.</summary>
    public static PixelCanvas FrostSpire()
    {
        var c = new PixelCanvas(16, 26) { PivotY = 8f / 26f };
        TowerBody(c, 14, Px.Hex("e6f0fa"), Px.Hex("b4c8dc"), Px.Hex("8aa0b8"), 51);
        c.HLine(2, 13, 15, Px.Hex("f4fbff")); c.HLine(2, 13, 14, Px.Hex("8aa0b8"));
        // snow on the ledge
        foreach (int x in new[] { 3, 4, 7, 8, 9, 12 }) c.Set(x, 16, Px.Hex("f4fbff"));
        Px ice = Px.Hex("8ae0ff"), iceL = Px.Hex("e6fbff"), iceD = Px.Hex("3aa0dc");
        void Shard(int x, int y0, int h)
        {
            for (int y = y0; y < y0 + h; y++) { c.Set(x, y, ice, true); c.Set(x + 1, y, iceD, true); }
            c.Set(x, y0 + h, iceL, true);
        }
        Shard(7, 16, 9); Shard(4, 16, 5); Shard(10, 16, 6);
        c.Set(7, 22, iceL, true); c.Set(7, 21, iceL, true);
        c.Fill(6, 9, 4, 3, Px.Hex("3aa0dc"), true); c.Set(7, 10, iceL, true);
        c.Outline();
        return c;
    }

    /// <summary>Storm Spire: dark stone wrapped in copper coils, with a crackling orb. Lightning jumps between enemies.</summary>
    public static PixelCanvas StormSpire()
    {
        var c = new PixelCanvas(16, 26) { PivotY = 8f / 26f };
        TowerBody(c, 15, Px.Hex("7a7a90"), Px.Hex("5a5a6e"), Px.Hex("3e3e50"), 61);
        Px copper = Px.Hex("d88a4a"), copperL = Px.Hex("ffc08a");
        foreach (int y in new[] { 6, 10, 14 }) { c.HLine(3, 12, y, copper); c.Set(4, y, copperL); }
        c.VLine(7, 16, 18, copper); c.VLine(8, 16, 18, Px.Hex("a8643a"));
        Px orb = Px.Hex("b07aff"), orbL = Px.Hex("f0e0ff"), orbD = Px.Hex("6a3ae0");
        c.Ellipse(7.5f, 21f, 3.2f, 3.2f, (x, y, dx, dy) => dx * dx + dy * dy < 0.25f ? orbL : dy > 0f && dx < 0f ? orb : orbD);
        for (int y = 18; y <= 24; y++) for (int x = 4; x <= 11; x++) if (!c.Get(x, y).IsClear && c.Get(x, y).B > 200) c.Set(x, y, c.Get(x, y), true);
        // sparks
        c.Set(2, 22, Px.Hex("fff07a"), true); c.Set(13, 20, Px.Hex("fff07a"), true); c.Set(12, 24, Px.Hex("fff07a"), true);
        c.Outline();
        return c;
    }

    /// <summary>Inferno: a black iron brazier on a stone base. Sets nearby enemies on fire.</summary>
    public static PixelCanvas Inferno()
    {
        var c = new PixelCanvas(16, 24) { PivotY = 8f / 24f };
        TowerBody(c, 11, Px.Hex("6a5a5a"), Px.Hex("4e4040"), Px.Hex("362c2c"), 71);
        Px iron = Px.Hex("2e2a30"), ironL = Px.Hex("5a545e");
        for (int y = 12; y <= 15; y++) { int inset = 15 - y; c.HLine(2 + inset / 2, 13 - inset / 2, y, y == 15 ? ironL : iron); }
        c.Set(2, 15, ironL); c.Set(13, 15, iron);
        // Flames
        Px f1 = Px.Hex("e8481e"), f2 = Px.Hex("ff9a30"), f3 = Px.Hex("fff2a8");
        int[] heights = { 3, 5, 7, 5, 8, 6, 4, 6, 3 };
        for (int i = 0; i < heights.Length; i++)
        {
            int x = 3 + i, h = heights[i];
            for (int y = 16; y < 16 + h; y++)
            {
                float t = (y - 16f) / h;
                c.Set(x, y, t < 0.35f ? f3 : t < 0.7f ? f2 : f1, true);
            }
        }
        c.Fill(6, 6, 4, 3, Px.Hex("ff7a1e"), true);
        c.Outline();
        return c;
    }

    // =====================================================================
    //  ORES
    // =====================================================================

    /// <summary>An ore node. Gold and Iron are nuggets in rock; the rest are glowing crystals.</summary>
    public static PixelCanvas OreNode(OreType type)
    {
        if (type == OreType.Gold) return Ore();
        var c = new PixelCanvas(16, 16);

        Px rL, r, rD, rDD;
        if (type == OreType.Emberite) { rL = Px.Hex("6a5e5e"); r = Px.Hex("4a4040"); rD = Px.Hex("362e2e"); rDD = Px.Hex("241e1e"); }
        else if (type == OreType.Bloodstone) { rL = Px.Hex("8a7a86"); r = Px.Hex("6a5a68"); rD = Px.Hex("4e4050"); rDD = Px.Hex("362a38"); }
        else if (type == OreType.SoulCrystal) { rL = Px.Hex("9aa4a4"); r = Px.Hex("737e80"); rD = Px.Hex("525c60"); rDD = Px.Hex("3a4246"); }
        else { rL = Px.Hex("b8aca0"); r = Px.Hex("948678"); rD = Px.Hex("6e6258"); rDD = Px.Hex("4e443c"); }

        Func<int, int, float, float, Px> rock = (x, y, dx, dy) =>
        {
            float l = -dx * 0.55f + dy * 0.8f + (Hash(x, y, 18 + (int)type) - 0.5f) * 0.35f;
            return l > 0.5f ? rL : l > 0f ? r : l > -0.55f ? rD : rDD;
        };
        c.Ellipse(10.5f, 5.5f, 4f, 3.8f, rock);
        c.Ellipse(5.5f, 5f, 4.5f, 4.2f, rock);
        c.Ellipse(8f, 3f, 4.5f, 2.5f, rock);

        if (type == OreType.Iron)
        {
            Px rust = Px.Hex("c8642a"), rustL = Px.Hex("e8945a"), silver = Px.Hex("e0e4ec");
            foreach (var (x, y) in new[] { (4, 6), (9, 4), (11, 7), (6, 2) })
            {
                c.Set(x, y, rust); c.Set(x + 1, y, Px.Hex("8a3e1a")); c.Set(x, y - 1, Px.Hex("8a3e1a"));
                c.Set(x, y + 1, rustL);
            }
            c.Set(5, 7, silver, true); c.Set(12, 8, silver, true);
            c.Outline();
            return c;
        }

        Px cL, cM, cD;
        switch (type)
        {
            case OreType.SoulCrystal: cL = Px.Hex("e6fffa"); cM = Px.Hex("6affd8"); cD = Px.Hex("2ab89a"); break;
            case OreType.Bloodstone: cL = Px.Hex("ffb0b8"); cM = Px.Hex("e8304a"); cD = Px.Hex("8a1a2e"); break;
            default: cL = Px.Hex("fff2a8"); cM = Px.Hex("ff8a2a"); cD = Px.Hex("c8401a"); break;
        }
        // Crystal shards sticking up out of the rock
        void Shard(int x, int y0, int h, bool wide)
        {
            for (int y = y0; y < y0 + h; y++)
            {
                c.Set(x, y, cM, true);
                if (wide) c.Set(x + 1, y, cD, true);
            }
            c.Set(x, y0 + h, cL, true);
            if (wide) c.Set(x + 1, y0 + h - 1, cM, true);
        }
        Shard(7, 6, 7, true); Shard(4, 6, 4, true); Shard(11, 6, 5, false); Shard(10, 4, 3, false);
        if (type == OreType.Emberite)
            foreach (var (x, y) in new[] { (3, 3), (4, 3), (12, 4), (8, 2), (9, 2) }) c.Set(x, y, cM, true); // glowing veins
        c.Outline();
        return c;
    }

    /// <summary>Small HUD icon for each resource.</summary>
    public static PixelCanvas OreIcon(OreType type)
    {
        if (type == OreType.Gold) return IconCoin();
        if (type == OreType.Iron) return PixelCanvas.FromArt(new[]
        {
            "..LLLLL.",
            ".LWWWWMk",
            "LMMMMMMk",
            "Mkkkkkk.",
        }, "L=e0e4ec W=ffffff M=a8aebc k=6a7080");
        string pal = type switch
        {
            OreType.SoulCrystal => "L=e6fffa M=6affd8 D=2ab89a",
            OreType.Bloodstone => "L=ffb0b8 M=e8304a D=8a1a2e",
            _ => "L=fff2a8 M=ff8a2a D=c8401a",
        };
        return PixelCanvas.FromArt(new[]
        {
            ".LMM.",
            "LLMMD",
            "MMMDD",
            ".MMD.",
            "..D..",
        }, pal);
    }

    public static string OreName(OreType type) => type switch
    {
        OreType.Iron => "Iron",
        OreType.SoulCrystal => "Soul Crystal",
        OreType.Bloodstone => "Bloodstone",
        OreType.Emberite => "Emberite",
        _ => "Gold",
    };

    /// <summary>Text colour for each resource, as hex.</summary>
    public static string OreColorHex(OreType type) => type switch
    {
        OreType.Iron => "c8ccd6",
        OreType.SoulCrystal => "6affd8",
        OreType.Bloodstone => "ff5a6a",
        OreType.Emberite => "ff9a3a",
        _ => "ffd75e",
    };

    // =====================================================================
    //  MORE PROJECTILES & ICONS
    // =====================================================================

    public static PixelCanvas Cannonball() => PixelCanvas.FromArt(new[]
    {
        ".kk.",
        "kLMk",
        "kMMk",
        ".kk.",
    }, "k=2a2c34 M=4a4e5a L=8a90a0");

    /// <summary>Enemy shots. kind: 0 bone, 1 web, 2 soul bolt, 3 boulder, 4 blood bolt.</summary>
    public static PixelCanvas EnemyShot(int kind) => kind switch
    {
        0 => PixelCanvas.FromArt(new[] { "W....W", "WWWWWW", "W....W" }, "W=e8e4d4"),
        1 => PixelCanvas.FromArt(new[] { "w.w.w", ".www.", "wwWww", ".www.", "w.w.w" }, "w=d0d0dc W=ffffff", "wW"),
        2 => PixelCanvas.FromArt(new[] { ".g.", "gGg", ".g." }, "g=4adc8a G=d0ffe0", "gG", outline: false),
        3 => PixelCanvas.FromArt(new[] { ".rr.", "rLrr", "rrrD", ".DD." }, "r=8c6a48 L=b08a62 D=4a3424"),
        _ => PixelCanvas.FromArt(new[] { ".r.", "rRr", ".r." }, "r=e8304a R=ffb0b8", "rR", outline: false),
    };

    public static PixelCanvas IconLock() => PixelCanvas.FromArt(new[]
    {
        ".kkk.",
        "k...k",
        "YYYYY",
        "YYkYY",
        "YYYYY",
    }, "k=8d9aab Y=d8aa3c");

    /// <summary>Crossed swords, for dungeons on the minimap.</summary>
    public static PixelCanvas IconSwords() => PixelCanvas.FromArt(new[]
    {
        "W...W",
        ".W.W.",
        "..W..",
        ".bWb.",
        "b...b",
    }, "W=eef1f6 b=d8aa3c");
}
