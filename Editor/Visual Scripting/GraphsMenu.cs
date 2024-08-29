using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Vapor.VisualScripting;

namespace VaporEditor.VisualScripting
{
    public static class GraphsMenu
    {
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int index)
        {
            var path = AssetDatabase.GetAssetPath(instanceId);
            var guid = AssetDatabase.AssetPathToGUID(path);

            foreach (var w in Resources.FindObjectsOfTypeAll<GraphEditorWindow>())
            {
                if (w.SelectedGuid == guid)
                {
                    w.Focus();
                    return true;
                }
            }

            var asset = EditorUtility.InstanceIDToObject(instanceId);
            if (asset.GetType() != typeof(GraphSo))
            {
                return false;
            }

            var window = EditorWindow.CreateWindow<GraphEditorWindow>(typeof(GraphEditorWindow), typeof(SceneView));
            window.Initialize(guid);
            window.Focus();
            return true;
        }

        [MenuItem("Assets/Create/Vapor/Graphs/Create Math Graph", priority = VaporConfig.AssetMenuPriority, secondaryPriority = 500)]
        private static void CreateMathGraph()
        {
            FunctionGraphModel graph = new();
            var json = JsonConvert.SerializeObject(graph, new JsonSerializerSettings()
            { 
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new FieldsOnlyContractResolver(),
                Converters = new List<JsonConverter> { new RectConverter() },
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented
            });
            var path = ScriptableObjectUtility.Create<GraphSo>(processAsset: (x) =>
            {
                var so = (GraphSo)x;
                so.ModelType = typeof(FunctionGraphModel).AssemblyQualifiedName;
                so.SearchIncludeFlags.Add("math");
                so.ModelJson = json;
            });

            //ProjectWindowUtil.CreateAssetWithContent("MathGraph.graph", json, Resources.Load<Texture2D>("chart-line"));
        }

    }
}
