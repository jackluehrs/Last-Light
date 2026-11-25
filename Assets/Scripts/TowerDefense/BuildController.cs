using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player building controls.
///   1-6  pick Crystal Tower / Miner / Cannon / Frost Spire / Storm Spire / Inferno (press again to put it away)
///   Left click place (stays selected so you can place several)
///   Right click / Esc  cancel. With nothing selected, right click a building to sell it for 50%.
/// </summary>
public class BuildController : MonoBehaviour
{
    public BuildType Selected { get; private set; } = BuildType.None;
    public string HoverReason { get; private set; }

    SpriteRenderer ghost;
    SpriteRenderer rangeRing;

    void Start()
    {
        ghost = new GameObject("Build Ghost").AddComponent<SpriteRenderer>();
        ghost.transform.SetParent(transform, false);
        ghost.sortingOrder = 30;
        SpriteFactory.MakeUnlit(ghost); // visible even in the dark

        rangeRing = new GameObject("Range Ring").AddComponent<SpriteRenderer>();
        rangeRing.transform.SetParent(transform, false);
        rangeRing.sprite = SpriteFactory.Ring();
        rangeRing.sortingOrder = 29;
        SpriteFactory.MakeUnlit(rangeRing);

        HideGhost();
    }

    public void Select(BuildType type)
    {
        Selected = (Selected == type) ? BuildType.None : type; // pressing again toggles off
    }

    void Update()
    {
        var gm = GameManager.Instance;
        var mouse = Mouse.current;
        var kb = Keyboard.current;
        if (gm == null || gm.IsGameOver || gm.IsMenuOpen || mouse == null || Camera.main == null || gm.Dungeons.Current != null)
        {
            HideGhost();
            return;
        }

        if (kb != null)
        {
            var digits = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key, kb.digit6Key };
            var pads = new[] { kb.numpad1Key, kb.numpad2Key, kb.numpad3Key, kb.numpad4Key, kb.numpad5Key, kb.numpad6Key };
            for (int i = 0; i < BuildingDef.Hotbar.Length; i++)
                if (digits[i].wasPressedThisFrame || pads[i].wasPressedThisFrame) Select(BuildingDef.Hotbar[i]);
            if (kb.escapeKey.wasPressedThisFrame) Selected = BuildType.None;
        }

        Vector2 screen = mouse.position.ReadValue();
        bool overHud = gm.IsScreenPointOverHud(screen);
        Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -Camera.main.transform.position.z));
        Vector2Int cell = gm.Map.WorldToCell(world);

        // Right click: cancel build mode, or sell the building under the mouse.
        if (mouse.rightButton.wasPressedThisFrame && !overHud)
        {
            if (Selected != BuildType.None) Selected = BuildType.None;
            else gm.TrySell(cell);
        }

        if (Selected == BuildType.None || overHud)
        {
            HideGhost();
            ShowRangeOfHoveredTower(gm, cell, overHud);
            return;
        }

        // Show a see-through preview, green if valid, red if not.
        bool ok = gm.CanPlace(Selected, cell, out string reason);
        HoverReason = reason;
        ghost.gameObject.SetActive(true);
        ghost.sprite = SpriteFactory.ForBuilding(Selected);
        ghost.transform.position = gm.Map.CellToWorld(cell);
        ghost.color = ok ? new Color(0.6f, 1f, 0.6f, 0.7f) : new Color(1f, 0.4f, 0.4f, 0.6f);

        var def = BuildingDef.Get(Selected);
        if (def.IsTower)
            ShowRing(ghost.transform.position, def.Range, ok);
        else
            rangeRing.gameObject.SetActive(false);

        if (mouse.leftButton.wasPressedThisFrame && ok)
            gm.Build(Selected, cell);
    }

    void ShowRangeOfHoveredTower(GameManager gm, Vector2Int cell, bool overHud)
    {
        var b = overHud ? null : gm.GetBuildingAt(cell);
        var tower = b != null ? b.GetComponent<Tower>() : null;
        if (tower != null) ShowRing(b.transform.position, tower.range, true);
        else rangeRing.gameObject.SetActive(false);
    }

    void ShowRing(Vector3 pos, float range, bool ok)
    {
        rangeRing.gameObject.SetActive(true);
        rangeRing.transform.position = pos;
        rangeRing.transform.localScale = Vector3.one * range * 2f;
        rangeRing.color = ok ? new Color(1f, 1f, 1f, 0.8f) : new Color(1f, 0.4f, 0.4f, 0.6f);
    }

    void HideGhost()
    {
        HoverReason = null;
        if (ghost != null) ghost.gameObject.SetActive(false);
        if (rangeRing != null) rangeRing.gameObject.SetActive(false);
    }
}
