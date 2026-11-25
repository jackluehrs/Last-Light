using UnityEngine;

/// <summary>The on-screen HUD (drawn with Unity's immediate-mode GUI, styled by Ui.cs).</summary>
public partial class GameManager
{
    public void ShowPopup(Vector3 worldPos, string text, Color color) =>
        popups.Add(new Popup { pos = worldPos, text = text, color = color });

    public void Announce(string message)
    {
        announcement = message;
        announcementTimer = 4.5f;
    }

    float UiScale => Mathf.Max(1f, Screen.height / 720f);
    float ScreenW => Screen.width / UiScale;
    float ScreenH => Screen.height / UiScale;
    Rect BuildBarRect => new Rect(ScreenW / 2f - 339f, ScreenH - 104f, 678f, 96f);
    Rect StatsRect => new Rect(10f, 10f, 250f, 214f);
    Rect MinimapRect
    {
        get
        {
            float w = 184f, h = Map != null ? w * Map.height / Map.width : 126f;
            return new Rect(ScreenW - w - 10f, 10f, w, h + 26f);
        }
    }

    /// <summary>Is this mouse position (Input System screen coords) on top of a HUD panel?</summary>
    public bool IsScreenPointOverHud(Vector2 screen)
    {
        var gui = new Vector2(screen.x, Screen.height - screen.y) / UiScale;
        return BuildBarRect.Contains(gui) || StatsRect.Contains(gui) || MinimapRect.Contains(gui);
    }

    void OnGUI()
    {
        if (Map == null || Hub == null) return;

        float s = UiScale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float w = ScreenW, h = ScreenH;

        // Floating "+5" popups
        var cam = Camera.main;
        if (cam != null)
        {
            var popupStyle = Ui.Style(15, TextAnchor.MiddleCenter, bold: true);
            foreach (var p in popups)
            {
                Vector3 sp = cam.WorldToScreenPoint(p.pos);
                var r = new Rect(sp.x / s - 90f, (Screen.height - sp.y) / s - 12f, 180f, 24f);
                Ui.Text(r, p.text, popupStyle, new Color(p.color.r, p.color.g, p.color.b, 1f - p.age), shadow: true);
            }
        }

        DrawStats();
        DrawMinimap();
        DrawWorldPrompts(cam, s);
        if (Dungeons.Current == null) DrawBuildBar(); else DrawDungeonBar();
        DrawRecall();

        // Boss health bar across the top
        float topY = 12f;
        var boss = NearbyBoss();
        if (boss != null && !IsGameOver)
        {
            var r = new Rect(w / 2f - 230f, 10f, 460f, 46f);
            Ui.Panel(r);
            Ui.Text(new Rect(r.x + 12, r.y + 4, r.width - 24, 18), boss.Def.Name, Ui.Style(15, TextAnchor.MiddleCenter, bold: true), Ui.Bad, shadow: true);
            Ui.Bar(new Rect(r.x + 12, r.y + 26, r.width - 24, 12), boss.Health.Current / boss.Health.Max, Ui.HpRed);
            topY = r.yMax + 6f;
        }

        // Announcements: a card at the top of the screen
        if (announcementTimer > 0f && !IsGameOver)
        {
            var style = Ui.Style(15, TextAnchor.MiddleCenter, wrap: true);
            float boxW = Mathf.Min(620f, Ui.Measure(announcement, style).x + 36f);
            float boxH = style.CalcHeight(new GUIContent(announcement), boxW - 24f) + 16f;
            var r = new Rect(w / 2f - boxW / 2f, topY, boxW, boxH);
            float a = Mathf.Clamp01(announcementTimer);
            GUI.color = new Color(1f, 1f, 1f, a);
            Ui.Panel(r);
            Ui.Text(new Rect(r.x + 12, r.y, r.width - 24, r.height), announcement, style, new Color(Ui.White.r, Ui.White.g, Ui.White.b, a));
            GUI.color = Color.white;
        }

        // Game over
        if (IsGameOver)
        {
            Ui.Dim(w, h, 0.7f);
            var r = new Rect(w / 2f - 280f, h / 2f - 100f, 560f, 200f);
            Ui.Panel(r);
            Ui.Inset(new Rect(r.x + 12, r.y + 12, r.width - 24, 70f), Ui.Hex("5a2a32"));
            Ui.Text(new Rect(r.x, r.y + 12, r.width, 70f), "The Hub has fallen!", Ui.Style(32, TextAnchor.MiddleCenter, bold: true), Ui.Bad, shadow: true);
            int cleared = Dungeons.All.FindAll(d => d.Cleared).Count;
            Ui.Text(new Rect(r.x + 20, r.y + 92, r.width - 40, 50f),
                $"You survived {FormatTime(ElapsedTime)}, reached wave {spawner.WaveNumber}, defeated {Kills} enemies and cleared {cleared} of 4 dungeons.",
                Ui.Style(15, TextAnchor.MiddleCenter, wrap: true), Ui.White);
            string again = "Press R to play again";
            float cw = Ui.Measure(again, Ui.Style(13, TextAnchor.MiddleCenter)).x + 12f;
            Ui.Chip(new Vector2(r.center.x - cw / 2f, r.yMax - 42f), again, Ui.GoldText, 13);
        }
    }

    // ---------- Stats panel (top-left) ----------

    void DrawStats()
    {
        var r = StatsRect;
        Ui.Panel(r);
        float x = r.x + 12, y = r.y + 10, rowW = r.width - 24;

        // Gold
        Ui.Icon(new Rect(x, y + 2, 16, 16), SpriteFactory.IconCoin());
        Ui.Text(new Rect(x + 22, y, 80, 20), "Gold", Ui.Style(14), Ui.Muted);
        Ui.Text(new Rect(x, y - 2, rowW, 24), Gold.ToString(), Ui.Style(22, TextAnchor.MiddleRight, bold: true), Ui.GoldText, shadow: true);
        y += 26;

        // The four special ores
        var inset = new Rect(x - 4, y, rowW + 8, 24);
        Ui.Inset(inset);
        float cellW = inset.width / 4f;
        for (int i = 1; i <= 4; i++)
        {
            var t = (OreType)i;
            float cx = inset.x + (i - 1) * cellW + 6;
            Ui.Icon(new Rect(cx, y + 5, 13, 13), SpriteFactory.OreIcon(t));
            Ui.Text(new Rect(cx + 17, y + 2, cellW - 20, 20), Amount(t).ToString(), Ui.Style(13, bold: true),
                    Amount(t) > 0 ? OreColor(t) : Ui.Muted);
        }
        y += 32;

        // Hub and hero health
        StatRow(new Rect(x, y, rowW, 20), SpriteFactory.Hub(), $"Hub {Hub.Level}", Hub.Health.Current, Hub.Health.Max, null);
        y += 24;
        if (Player != null)
            StatRow(new Rect(x, y, rowW, 20), SpriteFactory.IconHeart(), "You", Player.Health.Current, Player.Health.Max,
                    Player.IsAlive ? null : $"respawn in {Mathf.CeilToInt(Player.RespawnTimer)}s");
        y += 28;

        // Level, class and XP
        if (Player != null)
        {
            Ui.Icon(new Rect(x, y + 2, 16, 16), SpriteFactory.IconStar());
            Ui.Text(new Rect(x + 22, y, 100, 20), $"Lv {Player.Level}", Ui.Style(15, bold: true), Ui.White);
            var cls = Player.Skills.Class;
            string clsName = cls == HeroClass.None ? "no class" : SkillTree.ClassName(cls);
            Color clsColor = cls == HeroClass.None ? Ui.Muted : SkillTree.ClassColor(cls);
            Ui.ChipRight(r.xMax - 12, y + 1, clsName, clsColor, 11);
            y += 22;
            Ui.Bar(new Rect(x, y, rowW, 10), Player.Xp / Player.XpToNext, Ui.XpBlue);
            y += 12;
            Ui.Text(new Rect(x, y, rowW, 16), $"{Mathf.FloorToInt(Player.Xp)} / {Mathf.CeilToInt(Player.XpToNext)} XP",
                    Ui.Style(11, TextAnchor.MiddleRight), Ui.Muted);
            y += 18;
        }

        // Wave timer, time and kills
        Ui.Icon(new Rect(x, y + 1, 14, 16), SpriteFactory.IconHourglass());
        string wave = spawner.NextWaveIsBoss ? $"BOSS wave in {Mathf.CeilToInt(spawner.TimeToNextWave)}s"
                                            : $"Wave {spawner.WaveNumber + 1} in {Mathf.CeilToInt(spawner.TimeToNextWave)}s";
        Ui.Text(new Rect(x + 22, y, 160, 18), wave, Ui.Style(13), spawner.NextWaveIsBoss ? Ui.Bad : Ui.White);
        string killText = Kills.ToString();
        float kw = Ui.Measure(killText, Ui.Style(13)).x;
        Ui.Text(new Rect(r.xMax - 12 - kw, y, kw, 18), killText, Ui.Style(13), Ui.White);
        Ui.Icon(new Rect(r.xMax - 12 - kw - 20, y + 1, 15, 15), SpriteFactory.IconSkull());
        y += 18;
        Ui.Text(new Rect(x + 22, y, 200, 16), $"Time {FormatTime(ElapsedTime)}", Ui.Style(11), Ui.Muted);
    }

    /// <summary>Icon, name, health bar and "45/100".</summary>
    void StatRow(Rect r, Sprite icon, string name, float current, float max, string replaceBarWith)
    {
        Ui.Icon(new Rect(r.x, r.y + 2, 16, 16), icon);
        Ui.Text(new Rect(r.x + 22, r.y, 44, r.height), name, Ui.Style(14), Ui.Muted);
        var barRect = new Rect(r.x + 66, r.y + 5, r.width - 66 - 62, 11);
        if (replaceBarWith != null)
        {
            Ui.Text(new Rect(barRect.x, r.y, r.width - 66, r.height), replaceBarWith, Ui.Style(13), Ui.Bad);
            return;
        }
        float t = max > 0 ? current / max : 0f;
        Ui.Bar(barRect, t, Color.Lerp(Ui.HpRed, Ui.HpGreen, t));
        Ui.Text(new Rect(barRect.xMax, r.y, 62, r.height), $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}",
                Ui.Style(12, TextAnchor.MiddleRight), Ui.White);
    }

    // ---------- Minimap (top-right) ----------

    Texture2D minimap;

    static Color BiomeMapColor(Biome b) => b switch
    {
        Biome.DarkForest => Ui.Hex("2e5a3e"),
        Biome.Bloodmoor => Ui.Hex("6e2e44"),
        Biome.Crypt => Ui.Hex("5a625c"),
        Biome.Ashlands => Ui.Hex("54403a"),
        _ => Ui.Hex("5a9a40"),
    };

    Color[] minimapColors;     // the full map's colours
    int minimapFogVersion = -1;   // which FogOfWar.Version the texture was last drawn for

    bool FogOn => Lighting != null && Lighting.fogOfWar;

    void BuildMinimap()
    {
        if (minimap == null || minimap.width != Map.width || minimap.height != Map.height)
            minimap = new Texture2D(Map.width, Map.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        minimapColors = new Color[Map.width * Map.height];
        for (int x = 0; x < Map.width; x++)
            for (int y = 0; y < Map.height; y++)
            {
                var c = new Vector2Int(x, y);
                Color col = BiomeMapColor(Map.BiomeAt(c));
                if (Map.IsRoad(c)) col = Ui.Hex("b08a5a");
                else if (Map.IsTree(c)) col *= 0.55f;
                col.a = 1f;
                minimapColors[x + y * Map.width] = col;
            }
        minimapFogVersion = -1;
        RefreshMinimapFog();
    }

    /// <summary>Fog of war: places you've never lit up stay black on the minimap.</summary>
    void RefreshMinimapFog()
    {
        if (minimap == null || minimapColors == null) return;
        int version = FogOn ? FogOfWar.Version : -2;
        if (version == minimapFogVersion) return;
        minimapFogVersion = version;
        var unexplored = new Color(0.03f, 0.03f, 0.05f, 1f);
        var pixels = new Color[minimapColors.Length];
        for (int x = 0; x < Map.width; x++)
            for (int y = 0; y < Map.height; y++)
            {
                int i = x + y * Map.width;
                pixels[i] = !FogOn || FogOfWar.IsExplored(x, y) ? minimapColors[i] : unexplored;
            }
        minimap.SetPixels(pixels);
        minimap.Apply();
    }

    void DrawMinimap()
    {
        if (minimap == null) return;
        RefreshMinimapFog();
        var r = MinimapRect;
        Ui.Panel(r);
        var mapRect = new Rect(r.x + 6, r.y + 6, r.width - 12, r.height - 32);
        GUI.DrawTexture(mapRect, minimap);

        Vector2 ToMap(Vector3 world)
        {
            Rect b = Map.WorldBounds;
            return new Vector2(mapRect.x + (world.x - b.xMin) / b.width * mapRect.width,
                               mapRect.yMax - (world.y - b.yMin) / b.height * mapRect.height);
        }
        void Dot(Vector3 world, float size, Color c)
        {
            Vector2 p = ToMap(world);
            if (!mapRect.Contains(p)) return;
            GUI.color = c;
            GUI.DrawTexture(new Rect(p.x - size / 2f, p.y - size / 2f, size, size), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        foreach (var b in Buildings) if (b != null && b != Hub) Dot(b.transform.position, 2f, Ui.Hex("7ec8ff"));
        foreach (var e in Enemies)
            if (e != null && e.Mode != EnemyMode.Dungeon && e.Mode != EnemyMode.Guard
                && (!FogOn || FogOfWar.IsLit(e.transform.position))) // can't track what you can't see
                Dot(e.transform.position, e.IsBoss ? 5f : 2f, e.IsBoss ? Ui.Hex("ff3a3a") : Ui.Hex("ff7a7a"));
        Dot(Hub.transform.position, 5f, Ui.GoldText);
        foreach (var d in Dungeons.All)
        {
            if (FogOn && !FogOfWar.IsExplored(Map, d.EntrancePos)) continue; // undiscovered
            Vector2 p = ToMap(d.EntrancePos);
            Ui.Icon(new Rect(p.x - 5, p.y - 5, 10, 10), SpriteFactory.IconSwords(), d.Cleared ? 0.35f : 1f);
        }
        if (Player != null && Dungeons.Current == null && Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.55f)
            Dot(Player.transform.position, 4f, Color.white);

        // Artifacts you're carrying (bottom-left of the minimap)
        int ai = 0;
        foreach (var opens in Locks.HeldArtifacts)
            Ui.Icon(new Rect(r.x + 8 + ai++ * 18, r.yMax - 22, 14, 14), SpriteFactory.Artifact(opens));

        string label = Dungeons.Current != null ? "In " + Dungeons.Current.Name : PixelArt.BiomeName(PlayerBiome);
        label = char.ToUpper(label[0]) + label.Substring(1);
        Ui.Text(new Rect(r.x + 8, r.yMax - 24, r.width - 16, 20), label, Ui.Style(12, TextAnchor.MiddleCenter), Ui.White);
    }

    // ---------- Build bar (bottom middle) ----------

    void DrawBuildBar()
    {
        var bar = BuildBarRect;
        Ui.Panel(bar);
        for (int i = 0; i < BuildingDef.Hotbar.Length; i++)
            HotbarSlot(new Rect(bar.x + 12 + i * 110, bar.y + 8, 104, 62), BuildingDef.Hotbar[i], (i + 1).ToString());

        string hint = builder.Selected == BuildType.None
            ? "<b>1-6</b> build   <b>Space</b> attack / mine   <b>Q/F</b> abilities   <b>E</b> upgrade / enter   <b>K</b> skills   <b>Right-click</b> sell"
            : builder.HoverReason ?? BuildingDef.Get(builder.Selected).Description;
        bool warning = builder.Selected != BuildType.None && builder.HoverReason != null;
        Ui.Text(new Rect(bar.x + 12, bar.y + 73, bar.width - 24, 20), hint, Ui.Style(12, TextAnchor.MiddleCenter),
                warning ? Ui.Bad : Ui.Muted);
    }

    /// <summary>A hotbar slot: picture, key, name and cost. Gold border when selected; locked until the Hub is upgraded.</summary>
    void HotbarSlot(Rect r, BuildType type, string key)
    {
        var def = BuildingDef.Get(type);
        bool selected = builder.Selected == type;
        bool unlocked = HasUnlocked(type);
        bool canAfford = unlocked && CanAfford(def.Cost, def.Ore, def.OreCost);

        Ui.Inset(r, selected ? Ui.GoldText : Ui.InsetBorder);
        if (selected) Ui.Card(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), Color.clear, Ui.GoldText);

        Ui.Icon(new Rect(r.x + 4, r.y + 4, 34, 38), SpriteFactory.ForBuilding(type), canAfford ? 1f : 0.4f);
        Ui.Chip(new Vector2(r.x + 3, r.y + 3), key, selected ? Ui.PanelBg : Ui.White, 10, selected ? Ui.GoldText : (Color?)null);
        Ui.Text(new Rect(r.x + 2, r.yMax - 19, r.width - 4, 16), def.Name, Ui.Style(11, TextAnchor.MiddleCenter, bold: true),
                unlocked ? (canAfford ? Ui.White : Ui.Muted) : new Color(Ui.Muted.r, Ui.Muted.g, Ui.Muted.b, 0.6f));

        float cx = r.x + 44;
        if (!unlocked)
        {
            Ui.Icon(new Rect(cx + 6, r.y + 8, 14, 14), SpriteFactory.IconLock());
            Ui.Text(new Rect(cx, r.y + 24, 58, 16), $"Hub Lv{def.HubLevel}", Ui.Style(11), Ui.Muted);
            return;
        }
        Ui.Icon(new Rect(cx, r.y + 8, 12, 12), SpriteFactory.IconCoin());
        Ui.Text(new Rect(cx + 16, r.y + 5, 44, 18), def.Cost.ToString(), Ui.Style(13), Gold >= def.Cost ? Ui.GoldText : Ui.Bad);
        if (def.OreCost > 0)
        {
            Ui.Icon(new Rect(cx, r.y + 26, 12, 12), SpriteFactory.OreIcon(def.Ore));
            Ui.Text(new Rect(cx + 16, r.y + 23, 44, 18), def.OreCost.ToString(), Ui.Style(13),
                    Amount(def.Ore) >= def.OreCost ? OreColor(def.Ore) : Ui.Bad);
        }
    }

    /// <summary>Replaces the build bar while you're in a dungeon.</summary>
    void DrawDungeonBar()
    {
        var d = Dungeons.Current;
        var bar = new Rect(ScreenW / 2f - 250f, ScreenH - 70f, 500f, 60f);
        Ui.Panel(bar);
        string title = char.ToUpper(d.Name[0]) + d.Name.Substring(1);
        Ui.Text(new Rect(bar.x, bar.y + 8, bar.width, 22), title, Ui.Style(17, TextAnchor.MiddleCenter, bold: true), Ui.White);
        string hint = d.Cleared ? "Cleared!  Take the portal or the stairs home." : "Defeat the boss at the far end.  E at the stairs to leave.";
        if (Hub.Level >= HubLevels.RecallLevel) hint += "  H to recall.";
        Ui.Text(new Rect(bar.x + 10, bar.y + 32, bar.width - 20, 20), hint, Ui.Style(12, TextAnchor.MiddleCenter), Ui.Muted);
    }

    void DrawRecall()
    {
        if (Player == null || Player.RecallProgress < 0f) return;
        var r = new Rect(ScreenW / 2f - 110f, ScreenH - (Dungeons.Current != null ? 120f : 150f), 220f, 40f);
        Ui.Panel(r);
        Ui.Text(new Rect(r.x, r.y + 3, r.width, 18), "Recalling home...", Ui.Style(13, TextAnchor.MiddleCenter), Ui.White);
        Ui.Bar(new Rect(r.x + 12, r.y + 23, r.width - 24, 9), Player.RecallProgress, Ui.GoldText);
    }

    // ---------- Prompts above things in the world ----------

    void DrawWorldPrompts(Camera cam, float s)
    {
        if (cam == null || IsGameOver || Player == null) return;

        // Dungeon entrance
        var d = Dungeons.NearbyEntrance;
        if (d != null)
        {
            string boss = EnemyDefs.Get(EnemyDefs.BossOf(d.Biome)).Name;
            if (d.Cleared) Prompt(cam, s, d.EntrancePos + Vector3.up * 1.4f, $"{Cap(d.Name)} <color=#8be07a>(cleared)</color>", null);
            else
            {
                var next = AreaLocks.NextAfter(d.Biome);
                string reward = $"gold, {PixelArt.OreName(d.Ore)}, skill point" + (next != null ? $", {PixelArt.ArtifactName(next.Value)}" : "");
                Prompt(cam, s, d.EntrancePos + Vector3.up * 1.4f, $"<color=#ffd75e>[E]</color> Enter {d.Name}",
                       $"Boss: {boss}   Reward: {reward}");
            }
            return;
        }

        // Seal gate to a locked land
        if (Locks.NearbyGate != null && Dungeons.Current == null)
        {
            var land = Locks.NearbyGate.Value;
            string landName = Cap(PixelArt.BiomeName(land));
            string col = "#" + PixelArt.SealColorHex(land);
            if (Locks.HasArtifact(land))
                Prompt(cam, s, Locks.NearbyGatePos + Vector3.up * 2.2f, $"<color=#ffd75e>[E]</color> Break the seal on {landName}",
                       $"Uses the <color={col}>{PixelArt.ArtifactName(land)}</color>");
            else
            {
                var prev = AreaLocks.PreviousOf(land);
                string where = prev != null ? $"Beat {EnemyDefs.Get(EnemyDefs.BossOf(prev.Value)).Name} in {PixelArt.BiomeName(prev.Value)} to get it" : "";
                Prompt(cam, s, Locks.NearbyGatePos + Vector3.up * 2.2f,
                       $"{landName} is sealed. Needs the <color={col}>{PixelArt.ArtifactName(land)}</color>", where);
            }
            return;
        }
        if (Dungeons.Current != null)
        {
            if (Dungeons.NearExit)
                Prompt(cam, s, Player.transform.position + Vector3.up * 1.2f, "<color=#ffd75e>[E]</color> Leave the dungeon", null);
            return;
        }

        // Building upgrade
        var b = BuildingNearPlayer();
        if (b == null) return;
        var def = BuildingDef.Get(b.Type);
        string line1, line2;
        if (b.IsMaxLevel)
        {
            line1 = $"{def.Name}  Lv {b.Level} <color=#8d9aab>(max)</color>";
            line2 = null;
        }
        else
        {
            int cost = UpgradeCostFor(b);
            string costText = $"<color={(Gold >= cost ? "#ffd75e" : "#ff7a7a")}>{cost}g</color>";
            if (b.Type == BuildType.Hub)
            {
                var step = HubLevels.To(b.Level + 1);
                string oreCol = Amount(step.Ore) >= step.OreAmount ? "#" + PixelArt.OreColorHex(step.Ore) : "#ff7a7a";
                costText += $" + <color={oreCol}>{step.OreAmount} {PixelArt.OreName(step.Ore)}</color>";
                line2 = "Unlocks: " + string.Join(", ", step.Unlocks);
            }
            else
            {
                line2 = def.UpgradeText + ", full repair";
                var miner = b.GetComponent<Miner>();
                if (miner != null) line2 = $"Now: {miner.AmountPerTrip(this)} {PixelArt.OreName(miner.ore)} per trip. " + line2;
            }
            line1 = $"<color=#ffd75e>[E]</color> Upgrade {def.Name} to Lv {b.Level + 1}  {costText}";
        }
        float top = b.IsTall ? 1.7f : b.Radius + 0.6f;
        Prompt(cam, s, b.transform.position + Vector3.up * top, line1, line2);
    }

    void Prompt(Camera cam, float s, Vector3 world, string line1, string line2)
    {
        Vector3 sp = cam.WorldToScreenPoint(world);
        var st1 = Ui.Style(13, TextAnchor.MiddleCenter);
        var st2 = Ui.Style(11, TextAnchor.MiddleCenter);
        float width = Mathf.Max(Ui.Measure(line1, st1).x, line2 != null ? Ui.Measure(line2, st2).x : 0f) + 28f;
        float boxH = line2 == null ? 26f : 44f;
        var r = new Rect(sp.x / s - width / 2f, (Screen.height - sp.y) / s - boxH, width, boxH);
        Ui.Panel(r);
        Ui.Text(new Rect(r.x, r.y + 4, r.width, 18), line1, st1, Ui.White);
        if (line2 != null) Ui.Text(new Rect(r.x, r.y + 23, r.width, 16), line2, st2, Ui.Muted);
    }

    static string Cap(string s) => char.ToUpper(s[0]) + s.Substring(1);

    static string FormatTime(float t) => $"{(int)(t / 60)}:{(int)(t % 60):00}";
}
