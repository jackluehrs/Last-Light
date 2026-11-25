using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The skill tree screen (press K or Tab; the game pauses while it's open)
/// and the Q / F ability slots in the bottom-right corner.
/// </summary>
public class SkillTreeUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }

    const float ColWidth = 210f, ColGap = 14f, NodeHeight = 74f, RowGap = 16f, HeaderHeight = 58f;
    static readonly HeroClass[] Columns = { HeroClass.Knight, HeroClass.Ranger, HeroClass.Pyromancer, HeroClass.None };

    public void SetOpen(bool open)
    {
        var gm = GameManager.Instance;
        if (open && (gm == null || gm.IsGameOver || gm.Player == null)) return;
        IsOpen = open;
        Time.timeScale = open ? 0f : 1f;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.kKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame) SetOpen(!IsOpen);
        else if (IsOpen && kb.escapeKey.wasPressedThisFrame) SetOpen(false);
    }

    void OnGUI()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Player == null) return;

        float s = Mathf.Max(1f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        GUI.depth = -10; // draw on top of the normal HUD
        float w = Screen.width / s, h = Screen.height / s;

        if (!gm.IsGameOver) DrawAbilitySlots(gm.Player, w, h);
        if (IsOpen) DrawTree(gm.Player, w, h);
    }

    // ---------- Skill tree window ----------

    void DrawTree(PlayerController p, float w, float h)
    {
        var skills = p.Skills;
        float treeW = Columns.Length * ColWidth + (Columns.Length - 1) * ColGap;
        float treeH = HeaderHeight + 5 * NodeHeight + 4 * RowGap;
        var window = new Rect((w - treeW) / 2f - 20f, (h - treeH) / 2f - 60f, treeW + 40f, treeH + 110f);

        Ui.Dim(w, h, 0.65f);
        Ui.Panel(window);

        // Title bar
        Ui.Text(new Rect(window.x + 20, window.y + 8, 300, 32), "Skill Tree", Ui.Style(24, bold: true), Ui.White, shadow: true);
        float cx = window.xMax - 20;
        string points = $"{skills.Points} skill point{(skills.Points == 1 ? "" : "s")}";
        cx -= Ui.ChipRight(cx, window.y + 14, points, skills.Points > 0 ? Ui.GoldText : Ui.Muted, 13) + 6;
        string cls = skills.Class == HeroClass.None ? "no class yet" : SkillTree.ClassName(skills.Class);
        cx -= Ui.ChipRight(cx, window.y + 14, cls, skills.Class == HeroClass.None ? Ui.Muted : SkillTree.ClassColor(skills.Class), 13) + 6;
        Ui.ChipRight(cx, window.y + 14, $"Hero Lv {p.Level}", Ui.White, 13);

        float left = window.x + 20f, top = window.y + 46f;
        for (int c = 0; c < Columns.Length; c++)
        {
            float x = left + c * (ColWidth + ColGap);
            DrawColumn(p, Columns[c], x, top);
        }

        string hint = skills.Class == HeroClass.None
            ? "Your first point in a class column picks your class (you can't change it later). Survival skills work for everyone."
            : "Click a skill to learn it. Class skills unlock top to bottom. Q and F use your active abilities.";
        Ui.Text(new Rect(window.x + 20, window.yMax - 52, window.width - 40, 22), hint, Ui.Style(12, TextAnchor.MiddleCenter, wrap: true), Ui.Muted);
        Ui.Text(new Rect(window.x + 20, window.yMax - 30, window.width - 40, 22),
                "<color=#ffd75e>K</color> / <color=#ffd75e>Tab</color> / <color=#ffd75e>Esc</color> to close", Ui.Style(12, TextAnchor.MiddleCenter), Ui.White);
    }

    void DrawColumn(PlayerController p, HeroClass cls, float x, float top)
    {
        var skills = p.Skills;
        Color col = SkillTree.ClassColor(cls);
        bool locked = cls != HeroClass.None && skills.Class != HeroClass.None && skills.Class != cls;
        float a = locked ? 0.35f : 1f;

        string blurb = cls switch
        {
            HeroClass.Knight => "Tough melee fighter",
            HeroClass.Ranger => "Fast, fights from range",
            HeroClass.Pyromancer => "Fire magic from your torch",
            _ => "For every class"
        };

        // Header: the class's look, its name and a one-line blurb
        var head = new Rect(x, top, ColWidth, HeaderHeight - 8);
        Ui.Inset(head, locked ? Ui.InsetBorder : col * 0.7f);
        Ui.Icon(new Rect(x + 6, top + 4, 40, 42), SpriteFactory.Hero(cls), a);
        Ui.Text(new Rect(x + 52, top + 5, ColWidth - 56, 22), SkillTree.ClassName(cls), Ui.Style(17, bold: true), new Color(col.r, col.g, col.b, a));
        Ui.Text(new Rect(x + 52, top + 26, ColWidth - 56, 18), blurb, Ui.Style(11), new Color(Ui.Muted.r, Ui.Muted.g, Ui.Muted.b, a));

        float y = top + HeaderHeight;
        bool first = true;
        foreach (var skill in SkillTree.Column(cls))
        {
            // Connector line from the node above (class columns are a chain)
            if (!first && cls != HeroClass.None)
            {
                bool lit = skills.Has(SkillTree.Column(cls).ToArrayAt(skill.Row - 1).Id);
                GUI.color = lit ? col : new Color(0.3f, 0.36f, 0.45f, locked ? 0.3f : 0.9f);
                GUI.DrawTexture(new Rect(x + ColWidth / 2f - 2f, y - RowGap, 4f, RowGap), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            DrawNode(p, skill, new Rect(x, y, ColWidth, NodeHeight), col, locked);
            y += NodeHeight + RowGap;
            first = false;
        }
    }

    void DrawNode(PlayerController p, SkillDef skill, Rect r, Color classColor, bool columnLocked)
    {
        var skills = p.Skills;
        int rank = skills.Rank(skill.Id);
        bool canLearn = skills.CanLearn(skill, p.Level, out string reason);
        bool maxed = rank >= skill.MaxRank;
        float a = columnLocked ? 0.4f : 1f;

        // Learned: tinted with the class colour. Learnable: pulsing gold border. Otherwise: dark.
        Color fill = rank > 0 ? Color.Lerp(Ui.InsetBg, classColor, 0.28f) : Ui.InsetBg;
        Color border = rank > 0 ? classColor
                     : canLearn ? Color.Lerp(Ui.GoldText * 0.6f, Ui.GoldText, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f))
                     : Ui.InsetBorder;
        fill.a *= a; border.a *= a;
        Ui.Card(r, fill, border);
        if (canLearn) Ui.Card(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), Color.clear, border);

        Color text = new Color(Ui.White.r, Ui.White.g, Ui.White.b, a);
        float nx = r.x + 8;
        if (skill.IsActive)
            nx += Ui.Chip(new Vector2(nx, r.y + 5), skill.Key, Ui.PanelBg, 11, new Color(Ui.GoldText.r, Ui.GoldText.g, Ui.GoldText.b, a)) + 5;
        Ui.Text(new Rect(nx, r.y + 4, r.xMax - nx - 40, 20), skill.Name, Ui.Style(14, bold: true), text);
        if (skill.MaxRank > 1 || rank > 0)
            Ui.Text(new Rect(r.xMax - 44, r.y + 4, 36, 20), $"{rank}/{skill.MaxRank}", Ui.Style(12, TextAnchor.MiddleRight),
                    rank > 0 ? new Color(classColor.r, classColor.g, classColor.b, a) : new Color(Ui.Muted.r, Ui.Muted.g, Ui.Muted.b, a));
        Ui.Text(new Rect(r.x + 8, r.y + 24, r.width - 16, 32), skill.Description, Ui.Style(10, TextAnchor.UpperLeft, wrap: true),
                new Color(0.78f, 0.82f, 0.88f, a));

        string status = maxed ? "Learned" : canLearn ? "Click to learn" : reason;
        Color statusColor = maxed ? Ui.Good : canLearn ? Ui.GoldText : Ui.Bad;
        statusColor.a = a;
        Ui.Text(new Rect(r.x + 8, r.yMax - 18, r.width - 16, 16), status, Ui.Style(10), statusColor);

        if (canLearn && GUI.Button(r, GUIContent.none, GUIStyle.none))
        {
            bool wasClassless = skills.Class == HeroClass.None;
            skills.Learn(skill, p.Level);
            if (wasClassless && skills.Class != HeroClass.None)
                GameManager.Instance.Announce($"You are now a {SkillTree.ClassName(skills.Class)}!");
        }
    }

    // ---------- Q / F ability slots ----------

    void DrawAbilitySlots(PlayerController p, float w, float h)
    {
        var abilities = p.GetComponent<HeroAbilities>();
        if (abilities == null) return;
        string[] keys = { "Q", "F" };
        const float size = 64f, gap = 8f, pad = 8f;
        float panelW = (size + gap) * keys.Length - gap + pad * 2;
        var panel = new Rect(w - panelW - 10f, h - size - pad * 2 - 10f, panelW, size + pad * 2);
        Ui.Panel(panel);

        for (int i = 0; i < keys.Length; i++)
        {
            var r = new Rect(panel.x + pad + i * (size + gap), panel.y + pad, size, size);
            var skill = abilities.AbilityOn(keys[i]);
            if (skill == null)
            {
                Ui.Inset(r);
                Ui.Text(new Rect(r.x, r.y + 10, r.width, 22), keys[i], Ui.Style(18, TextAnchor.MiddleCenter, bold: true), new Color(1, 1, 1, 0.25f));
                Ui.Text(new Rect(r.x, r.y + 34, r.width, 16), "locked", Ui.Style(10, TextAnchor.MiddleCenter), new Color(Ui.Muted.r, Ui.Muted.g, Ui.Muted.b, 0.6f));
                continue;
            }

            float frac = abilities.CooldownFraction(keys[i]);
            Color cc = SkillTree.ClassColor(skill.Class);
            Ui.Card(r, Color.Lerp(Ui.InsetBg, cc, 0.3f), frac > 0f ? Ui.InsetBorder : cc);
            if (frac > 0f && Event.current.type == EventType.Repaint)
            {
                // Dark overlay that shrinks as the cooldown finishes
                GUI.color = new Color(0, 0, 0, 0.6f);
                GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, r.width - 4, (r.height - 4) * frac), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            Ui.Chip(new Vector2(r.x + 3, r.y + 3), keys[i], Ui.PanelBg, 11, Ui.GoldText);
            float secs = abilities.CooldownSeconds(keys[i]);
            if (secs > 0f)
                Ui.Text(new Rect(r.x, r.y + 14, r.width, 24), Mathf.CeilToInt(secs).ToString(), Ui.Style(20, TextAnchor.MiddleCenter, bold: true), Ui.White, shadow: true);
            Ui.Text(new Rect(r.x + 2, r.yMax - 24, r.width - 4, 22), skill.Name, Ui.Style(10, TextAnchor.MiddleCenter, wrap: true), Ui.White);
        }

        if (p.Skills.Points > 0)
        {
            string msg = $"{p.Skills.Points} skill point{(p.Skills.Points > 1 ? "s" : "")}!  Press K";
            Ui.ChipRight(panel.xMax, panel.y - 26f, msg, Ui.GoldText, 12);
        }
    }
}

static class SkillColumnExtensions
{
    /// <summary>The skill at a given row in a column.</summary>
    public static SkillDef ToArrayAt(this System.Collections.Generic.IEnumerable<SkillDef> column, int row)
    {
        foreach (var s in column) if (s.Row == row) return s;
        return null;
    }
}
