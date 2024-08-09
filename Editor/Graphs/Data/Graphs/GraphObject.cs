using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Graphs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Vapor.Inspector;

namespace VaporEditor.Graphs
{
    public class GraphObject : ScriptableObject, ISourceDataStore<GraphModel>
    {
        public Type GraphType;
        public GraphModel Graph;
        public GraphModel State => Graph;

        private JsonSerializerSettings _serializerSettings;

        public event Action<GraphModel, ISourceDataAction> Subscribe;


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
                    args.ErrorContext.Handled = true;
                }
            };
            GraphType = modelType;            
            Graph = (GraphModel)JsonConvert.DeserializeObject(json, GraphType, _serializerSettings);
        }

        public void Validate()
        {
            Graph.Children ??= new List<NodeModel>();
            if (Graph.Root == null)
            {
                Graph.Root = Graph.GenerateDefaultRootNode();
            }
            //else
            //{
            //    var json = JsonConvert.SerializeObject(Graph.Root, _serializerSettings);
            //    var clonedRoot = (NodeModel)JsonConvert.DeserializeObject(json, Graph.Root.ToNodeType(), _serializerSettings);
            //    Graph.Root = clonedRoot;
            //}

            //Graph.Children ??= new List<NodeModel>();
            //List<NodeModel> clonedChildren = new(Graph.Children.Count);
            //foreach (var c in Graph.Children)
            //{
            //    var json = JsonConvert.SerializeObject(c, _serializerSettings);
            //    var clone = (NodeModel)JsonConvert.DeserializeObject(json, c.ToNodeType(), _serializerSettings);
            //    clonedChildren.Add(clone);
            //}
            //Graph.Children = clonedChildren;
        }

        public string Serialize()
        {
            List<NodeModel> links = new()
            {
                Graph.Root
            };
            links.AddRange(Graph.Children);

            foreach (var l in links)
            {
                l.LinkNodeData(links, null);
            }

            return JsonConvert.SerializeObject(Graph, _serializerSettings);
        }

        public void Dispatch(ISourceDataAction changeAction)
        {

        }
    }
}
