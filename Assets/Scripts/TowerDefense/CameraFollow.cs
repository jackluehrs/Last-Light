using UnityEngine;

/// <summary>
/// Smoothly keeps the camera on the player, without ever showing past the edge of the map.
/// Added to the Main Camera automatically by the GameManager.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Tooltip("How much of the world you can see: half the screen height, in tiles.")]
    public float zoom = 8f;
    [Tooltip("Lower = snappier, higher = floatier.")]
    public float smoothTime = 0.12f;

    public Transform target;

    Camera cam;
    Vector3 velocity;

    // Screen shake: "trauma" jumps up on big hits and fades away; the shake is trauma squared so small bumps stay subtle.
    static float trauma;
    Vector3 shakeOffset;

    /// <summary>Shake the screen (0.1 = a nudge, 0.5 = a big boom). Juice.Shake calls this.</summary>
    public static void AddShake(float amount) => trauma = Mathf.Clamp01(trauma + amount);

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
    }

    /// <summary>Jump straight to the target (no sliding), e.g. at the start of a game.</summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = Clamp(new Vector3(target.position.x, target.position.y, transform.position.z));
        velocity = Vector3.zero;
        shakeOffset = Vector3.zero;
    }

    void LateUpdate()
    {
        cam.orthographicSize = zoom;
        transform.position -= shakeOffset; // undo last frame's shake before following
        shakeOffset = Vector3.zero;
        if (target == null) return;

        // While the player is dead their object stays where they fell, so the camera waits there.
        Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);
        Vector3 next = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
        transform.position = Clamp(next);

        if (trauma > 0f && Time.deltaTime > 0f)
        {
            float t = trauma * trauma * 0.6f;
            float time = Time.unscaledTime * 25f;
            shakeOffset = new Vector3((Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f * t, (Mathf.PerlinNoise(0f, time) - 0.5f) * 2f * t, 0f);
            transform.position += shakeOffset;
            trauma = Mathf.Max(0f, trauma - Time.deltaTime * 1.6f);
        }
    }

    /// <summary>Keeps the camera's view inside the map rectangle.</summary>
    Vector3 Clamp(Vector3 pos)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Map == null) return pos;

        Rect b = gm.CurrentBounds; // the map, or the dungeon you're in
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        // If the map is smaller than the view, just center on it.
        pos.x = b.width <= halfW * 2f ? b.center.x : Mathf.Clamp(pos.x, b.xMin + halfW, b.xMax - halfW);
        pos.y = b.height <= halfH * 2f ? b.center.y : Mathf.Clamp(pos.y, b.yMin + halfH, b.yMax - halfH);
        return pos;
    }
}
