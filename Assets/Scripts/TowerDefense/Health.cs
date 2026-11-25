using System;
using UnityEngine;

/// <summary>
/// Hit points plus a small health bar that floats above the object.
/// The bar only shows once the object has taken damage.
/// </summary>
public class Health : MonoBehaviour
{
    public float Max { get; private set; }
    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;

    /// <summary>Fires once when health reaches zero.</summary>
    public event Action Died;

    /// <summary>Fires every time damage is taken (with the amount).</summary>
    public event Action<float> Damaged;

    /// <summary>Incoming damage is multiplied by this (0.7 = take 30% less).</summary>
    public float DamageMultiplier { get; set; } = 1f;

    /// <summary>While true, no damage is taken at all (e.g. while dashing).</summary>
    public bool Invulnerable { get; set; }

    /// <summary>
    /// Asked when health hits zero. Return true to survive at 50% health instead of dying
    /// (used by the Phoenix skill).
    /// </summary>
    public Func<bool> TryCheatDeath { get; set; }

    GameObject bar;
    Transform fill;
    SpriteRenderer fillRenderer;
    float barWidth;

    public void Init(float maxHp, float width, float heightAbove)
    {
        Max = Current = maxHp;
        barWidth = width;

        bar = new GameObject("HealthBar");
        bar.transform.SetParent(transform, false);
        bar.transform.localPosition = new Vector3(-width / 2f, heightAbove, 0f);

        var bg = new GameObject("Background").AddComponent<SpriteRenderer>();
        bg.transform.SetParent(bar.transform, false);
        bg.sprite = SpriteFactory.Pixel(pivotLeft: true);
        bg.color = new Color(0.1f, 0.08f, 0.14f, 0.9f);
        bg.sortingOrder = 20;
        SpriteFactory.MakeUnlit(bg); // stays readable in the dark
        bg.transform.localScale = new Vector3(width + 0.12f, 0.2f, 1f);
        bg.transform.localPosition = new Vector3(-0.06f, 0f, 0f);

        fillRenderer = new GameObject("Fill").AddComponent<SpriteRenderer>();
        fill = fillRenderer.transform;
        fill.SetParent(bar.transform, false);
        fillRenderer.sprite = SpriteFactory.Pixel(pivotLeft: true);
        fillRenderer.sortingOrder = 21;
        SpriteFactory.MakeUnlit(fillRenderer);

        UpdateBar();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || Invulnerable) return;
        amount *= DamageMultiplier;
        if (amount <= 0f) return;
        Current = Mathf.Max(0f, Current - amount);
        if (IsDead && TryCheatDeath != null && TryCheatDeath()) Current = Max * 0.5f;
        UpdateBar();
        Damaged?.Invoke(amount);
        if (IsDead) Died?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        Current = Mathf.Min(Max, Current + amount);
        UpdateBar();
    }

    /// <summary>
    /// Change max health (for level-ups). With fillUp, heal to full; otherwise keep the same
    /// amount of missing health. Never brings a dead object back.
    /// </summary>
    public void SetMax(float newMax, bool fillUp)
    {
        float gained = newMax - Max;
        Max = newMax;
        if (!IsDead) Current = fillUp ? Max : Mathf.Clamp(Current + gained, 1f, Max);
        UpdateBar();
    }

    /// <summary>Back to full health (used when the player respawns).</summary>
    public void ResetToFull()
    {
        Current = Max;
        UpdateBar();
    }

    void UpdateBar()
    {
        if (bar == null) return;
        float t = Max > 0 ? Current / Max : 0f;
        bar.SetActive(t < 1f && t > 0f);
        fill.localScale = new Vector3(barWidth * t, 0.1f, 1f);
        fill.localPosition = new Vector3(0f, 0f, 0f);
        fillRenderer.color = Color.Lerp(new Color(0.88f, 0.28f, 0.28f), new Color(0.49f, 0.82f, 0.36f), t);
    }
}
