using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor.VisualScripting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Vapor.Inspector;
using Vapor.NewtonsoftConverters;

namespace VaporEditor.VisualScripting
{
    /// <summary>
    /// The backing object for the graph.
    /// All changes made to the graph should be reflected here and then drawn to the proper view.
    /// </summary>
    public class GraphObject : ScriptableObject, ISourceDataStore<GraphModel>
    {
        public Type GraphType;
        public GraphModel Graph;
        public GraphModel State => Graph;

        private JsonSerializerSettings _serializerSettings;
        private List<NodeModel> _nodesToAdd = new();
        private List<NodeModel> _nodesToRemove = new();

        private List<EdgeConnection> _edgesToAdd = new();
        private List<EdgeConnection> _edgesToRemove = new();

        public event Action<GraphModel, ISourceDataAction> Subscribe;
        public Action<NodeModel> RequireRedraw;

        public void Setup(string json, Type modelType)
        {
            _serializerSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new FieldsOnlyContractResolver(),
                Converters = new List<JsonConverter> { new RectConverter() },
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
                Error = (sender, args) =>
                {
                    Debug.Log(args.ErrorContext.Path);
                    args.ErrorContext.Handled = true;
                }
            };
            GraphType = modelType;            
            Graph = (GraphModel)JsonConvert.DeserializeObject(json, GraphType, _serializerSettings);
        }

        public void Validate()
        {
            Graph.Nodes ??= new List<NodeModel>();
            if (Graph is FunctionGraphModel functionGraph)
            {
                functionGraph.GetEntryNode();
                functionGraph.GetReturnNode();
                functionGraph.RedrawEntryNode = OnRequireRedraw;
                functionGraph.RedrawReturnNode = OnRequireRedraw;
            }
            if (Graph is MathGraphModel mathGraph)
            {
                mathGraph.GetReturnNode();
            }
        }

        public string Serialize()
        {
            List<NodeModel> links = new();
            links.AddRange(Graph.Nodes);
            foreach (var l in links)
            {
                l.BuildSlots();
            }

            foreach (var l in links)
            {
                l.LinkNodeData(links, null);
            }

            return JsonConvert.SerializeObject(Graph, _serializerSettings);
        }

        public void Dispatch(ISourceDataAction changeAction)
        {

        }

        private void OnRequireRedraw(NodeModel node)
        {
            RequireRedraw?.Invoke(node);
        }

        #region Nodes
        public void AddNode(NodeModel node)
        {
            _nodesToAdd.Add(node);
        }
        #endregion

        #region Edges 
        public void Connect()
        {

        }
        #endregion
    }
}
