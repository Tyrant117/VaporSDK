using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Inspector;
using Vapor.Keys;

namespace Vapor.VisualScripting
{
    [DatabaseKeyValuePair, KeyOptions(includeNone: false, category: "Graphs")]
    public class GraphSo : NamedKeySo, IDatabaseInitialize
    {
        public List<string> SearchIncludeFlags = new();

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
            Graph = model.Build(true);

            //_isLoaded = true;
            RuntimeDataStore<IGraph>.InitDatabase(RuntimeDatabase<GraphSo>.Count);
        }

        public void PostInitializedInDatabase()
        {
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
    }
}
