using UnityEditor;
using UnityEngine;

public static class TowerDefenseMenu
{
    /// <summary>
    /// Adds a Game Manager to the scene so you can tweak its settings in the Inspector.
    /// (Without this, one is created automatically when you press Play, using default values.)
    /// </summary>
    [MenuItem("Tools/Create Tower Defense Game")]
    static void Create()
    {
        if (Object.FindAnyObjectByType<MapGenerator>() == null)
        {
            var map = new GameObject("Map");
            Undo.RegisterCreatedObjectUndo(map, "Create Map");
            map.AddComponent<MapGenerator>();
        }

        var existing = Object.FindAnyObjectByType<GameManager>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        var go = new GameObject("Game Manager");
        Undo.RegisterCreatedObjectUndo(go, "Create Tower Defense Game");
        go.AddComponent<GameManager>();
        Selection.activeGameObject = go;
    }
}
