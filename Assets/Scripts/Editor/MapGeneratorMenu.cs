using UnityEditor;
using UnityEngine;

public static class MapGeneratorMenu
{
    [MenuItem("Tools/Create Forest Map")]
    static void CreateMap()
    {
        var existing = Object.FindAnyObjectByType<MapGenerator>();
        if (existing != null)
        {
            existing.Generate();
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        var go = new GameObject("Map");
        Undo.RegisterCreatedObjectUndo(go, "Create Forest Map");
        go.AddComponent<MapGenerator>();
        Selection.activeGameObject = go;
    }
}
