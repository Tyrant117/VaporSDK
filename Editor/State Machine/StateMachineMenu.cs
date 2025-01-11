using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Vapor.StateMachines;

namespace VaporEditor.StateMachines
{
    public static class StateMachineMenu
    {
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int index)
        {
            var path = AssetDatabase.GetAssetPath(instanceId);
            var guid = AssetDatabase.AssetPathToGUID(path);

            foreach (var w in Resources.FindObjectsOfTypeAll<StateMachineEditorWindow>())
            {
                if (w.SelectedGuid == guid)
                {
                    w.Focus();
                    return true;
                }
            }

            var asset = EditorUtility.InstanceIDToObject(instanceId);
            if (asset.GetType() != typeof(StateMachineSo))
            {
                return false;
            }

            var window = (StateMachineEditorWindow)EditorWindow.GetWindow(typeof(StateMachineEditorWindow));
            window.Initialize(guid);
            window.Focus();
            return true;
        }
    }
}
