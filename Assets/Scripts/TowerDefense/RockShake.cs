using UnityEngine;

/// <summary>Makes an ore rock jiggle briefly when the hero mines it.</summary>
public class RockShake : MonoBehaviour
{
    Vector3 home;
    float timer;
    bool homeSaved;

    public void Shake()
    {
        if (!homeSaved) { home = transform.position; homeSaved = true; }
        timer = 0.15f;
    }

    void Update()
    {
        if (timer <= 0f) return;
        timer -= Time.deltaTime;
        transform.position = timer > 0f
            ? home + (Vector3)(Random.insideUnitCircle * 0.06f)
            : home;
    }
}
