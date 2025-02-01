using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Vapor.Blueprints;
using Vapor.VisualScripting;
using VaporEditor.VisualScripting;

namespace VaporEditor.Blueprints
{
    public static class BlueprintGraphMenu
    {
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int index)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceId);
            if (asset.GetType() != typeof(BlueprintGraphSo))
            {
                return false;
            }
            
            var path = AssetDatabase.GetAssetPath(instanceId);
            var guid = AssetDatabase.AssetPathToGUID(path);

            foreach (var w in Resources.FindObjectsOfTypeAll<BlueprintEditorWindow>())
            {
                if (w.SelectedGuid == guid)
                {
                    w.Focus();
                    return true;
                }
            }

            var window = EditorWindow.CreateWindow<BlueprintEditorWindow>();
            window.Initialize(guid);
            window.Focus();
            return true;
        }

        [MenuItem("Assets/Create/Vapor/Blueprints/Function Graph", priority = VaporConfig.AssetMenuPriority, secondaryPriority = 500)]
        private static void CreateFunctionGraph()
        {
            var path = ScriptableObjectUtility.Create<BlueprintGraphSo>(processAsset: (x) =>
            {
                var so = (BlueprintGraphSo)x;
            });
        }
    }
}
