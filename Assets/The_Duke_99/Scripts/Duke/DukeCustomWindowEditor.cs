using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class DukeCustomWindowEditor : EditorWindow {
    [MenuItem("Tools/The Handsome Duke/Show Persistent Data Path Folder")]    
    public static void OpenPersistentDataPath() {
        Application.OpenURL("file:///" + Application.persistentDataPath);
    }

    [MenuItem("Tools/The Handsome Duke/Add separator")]
    public static void AddSeparator() {
        GameObject obj = new GameObject("---------------------------------------------");
        obj.transform.position = Vector3.zero;
        obj.transform.SetParent(null);
        AssetDatabase.SaveAssets();
    }
}
#endif