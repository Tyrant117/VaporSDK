using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Vapor.DataTables;

namespace VaporEditor.DataTables
{
    public static class DataTableMenu
    {
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int index)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceId);
            if (asset.GetType() != typeof(DataTableSo))
            {
                return false;
            }
            
            var path = AssetDatabase.GetAssetPath(instanceId);
            var guid = AssetDatabase.AssetPathToGUID(path);

            foreach (var w in Resources.FindObjectsOfTypeAll<DataTableEditorWindow>())
            {
                if (w.SelectedGuid != guid)
                {
                    continue;
                }

                w.Focus();
                return true;
            }

            var window = EditorWindow.CreateWindow<DataTableEditorWindow>();
            window.Initialize(guid);
            window.Focus();
            return true;
        }
    }
}
