using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Inspector;
using Vapor.Keys;
using Vapor.NewtonsoftConverters;

namespace Vapor.VisualScripting
{
    [DatabaseKeyValuePair, KeyOptions(includeNone: false, category: "Graphs")]
    public class GraphSo : NamedKeySo, IDatabaseInitialize
    {
        public List<string> SearchIncludeFlags = new();
        public bool AllowImpureNodes;
        [ValueDropdown("GraphType", ValueDropdownAttribute.FilterType.Category)]
        public KeyDropdownValue GraphType;

        [Title("Graph")]
        [ReadOnly]
        public string ModelType;
        [Button]
#pragma warning disable IDE0051 // Remove unused private members
        private void ToggleEdit() { EditEnabled = !EditEnabled; }
#pragma warning restore IDE0051 // Remove unused private members
        [EnableIf("@EditEnabled"), TextArea(50, 100), HideLabel]
        public string ModelJson;

        [SerializeField, HideInInspector]
        protected bool EditEnabled;

        //[NonSerialized]
        //private bool _isLoaded;
        public IGraph Graph { get; set; }

        public GraphModel GetGraphModel()
        {
            var serializerSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new FieldsOnlyContractResolver(),
                Converters = new List<JsonConverter> { new RectConverter() },
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
                Error = (sender, args) =>
                {
                    args.ErrorContext.Handled = true;
                }
            };
            
            return (GraphModel)JsonConvert.DeserializeObject(ModelJson, Type.GetType(ModelType), serializerSettings);
        }

        public void InitializedInDatabase()
        {
            //Debug.Log($"Graph Initialized: {_isLoaded}");
            //if (_isLoaded)
            //{
            //    return;
            //}

            var model = GetGraphModel();
            Graph = model.Build((ushort)Key, true, name);

            //_isLoaded = true;
            RuntimeDataStore<IGraph>.InitDatabase(RuntimeDatabase<GraphSo>.Count);
        }

        public void PostInitializedInDatabase()
        {
            Debug.Log("Post Initialized Graph: " + Key);
            RuntimeDataStore<IGraph>.Add(Key, Graph);
        }

        //public IGraph GetGraphDependency()
        //{
        //    if (_isLoaded)
        //    {
        //        return Graph;
        //    }

        //    var model = GetGraphModel();
        //    Graph = model.Build(true);

        //    _isLoaded = true;
        //    return Graph;
        //}

        public override void GenerateAdditionalKeys()
        {
#if UNITY_EDITOR
            var allGraphs = RuntimeAssetDatabaseUtility.FindAssetsByType<GraphSo>();
            Dictionary<KeyDropdownValue, List<string>> guidMap = new();
            foreach (var graph in allGraphs)
            {
                if (!guidMap.TryGetValue(graph.GraphType, out var list))
                {
                    list = new List<string>();
                    guidMap.Add(graph.GraphType, list);
                }
                list.Add(UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(graph)));
            }

            foreach (var kvp in guidMap)
            {
                if(kvp.Key != KeyDropdownValue.None)
                {
                    var groupName = kvp.Key.DisplayName.Replace(" ", string.Empty);
                    KeyGenerator.GenerateKeys<GraphSo>(kvp.Value, $"Graph{groupName}Keys", true);
                }
            }
#endif
        }
    }
}
