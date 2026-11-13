using UnityEditor;
using UnityEngine;

public static class HoverThrusterMenu
{
    // One-time setup: create two prefabs named exactly "ThrusterModel" and "HoverVFX".
    // After that, every floating object needs only one click.

    [MenuItem("GameObject/Hover/Add Thruster Rig", false, 30)]
    static void AddRig() { AddTo(false); }

    [MenuItem("GameObject/Hover/Add Thruster Rig (Jump Pad model)", false, 31)]
    static void AddRigJumpPad() { AddTo(true); }

    [MenuItem("GameObject/Hover/Add Thruster Rig", true)]
    [MenuItem("GameObject/Hover/Add Thruster Rig (Jump Pad model)", true)]
    static bool Validate() { return Selection.gameObjects.Length > 0; }

    static void AddTo(bool jumpPad)
    {
        foreach (var go in Selection.gameObjects)
        {
            var rig = go.GetComponent<HoverThrusterRig>();
            if (rig == null) rig = Undo.AddComponent<HoverThrusterRig>(go);

            if (rig.thrusterModel == null && !jumpPad) rig.thrusterModel = FindPrefab("ThrusterModel");
            if (rig.vfxPrefab == null) rig.vfxPrefab = FindPrefab("HoverVFX");
            if (jumpPad) rig.ApplyJumpPadPreset();

            rig.Rebuild();
            EditorUtility.SetDirty(rig);
        }
    }

    static GameObject FindPrefab(string prefabName)
    {
        foreach (var guid in AssetDatabase.FindAssets(prefabName + " t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == prefabName)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        Debug.LogWarning("Prefab not found: " + prefabName);
        return null;
    }
}
