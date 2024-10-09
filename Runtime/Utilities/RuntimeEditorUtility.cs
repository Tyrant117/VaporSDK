using UnityEngine;

namespace Vapor
{
    public static class RuntimeEditorUtility
    {
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void DirtyAndSave(Object obj)
        {
            UnityEditor.EditorUtility.SetDirty(obj);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(obj);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Ping(Object obj)
        {
            UnityEditor.EditorGUIUtility.PingObject(obj);
            UnityEditor.Selection.SetActiveObjectWithContext(obj, null);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void SaveAndRefresh()
        {
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
    }
}
