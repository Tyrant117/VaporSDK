using UnityEngine;

namespace Vapor
{
    public static class RuntimeEditorUtility
    {
        //[System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void DirtyAndSave(Object obj)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(obj);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(obj);
#endif
        }

        //[System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Ping(Object obj)
        {
#if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.PingObject(obj);
            UnityEditor.Selection.SetActiveObjectWithContext(obj, null);
#endif
        }

        //[System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void SaveAndRefresh()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}
