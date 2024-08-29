using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Inspector;

namespace Vapor.VisualScripting
{
    public class FunctionGraph : IGraph, IEvaluatorNode<double, IExternalValueSource>
    {
        public uint Id { get; }

        public readonly IEvaluatorNode<double, IExternalValueSource> Root;

        public FunctionGraph(INode root)
        {
            Root = (IEvaluatorNode<double, IExternalValueSource>)root;
        }        

        public double Evaluate(GraphModel graph, IExternalValueSource arg)
        {
            return Root.Evaluate(graph, arg);
        }
    }

    [Serializable]
    public class FunctionGraphModel : GraphModel
    {
        [Serializable]
        public class InspectorDrawer
        {
            [TypeSelector("@GetTypes")]
            public List<string> Input = new();
            [TypeSelector("@GetTypes")]
            public List<string> Output = new();

            public static IEnumerable<Type> GetTypes()
            {
                return new List<Type>()
                {
                    typeof(int),
                    typeof(double),
                    typeof(Vector2),
                    typeof(Vector3)
                };                
            }
        }

        [JsonIgnore]
        public InspectorDrawer Inspector { get; set; }

        public FunctionGraphModel()
        {
            AssemblyQualifiedType = GetType();
        }

        public override IGraph Build(bool refresh = false)
        {
            if (refresh)
            {
                foreach (var c in Nodes)
                {
                    c.Refresh();
                }
            }

            var entry = GetEntryNode();
            var exit = GetReturnNode();
            var root = entry.Build(this);
            return new FunctionGraph(root);
        }

        public NodeModel GetEntryNode()
        {
            var entry = Nodes.OfType<FunctionEntryNodeModel>().FirstOrDefault();
            if (entry == null)
            {
                entry = GenerateDefaultEntryNode();
                Nodes.Add(entry);
            }
            return entry;
        }

        public NodeModel GetReturnNode()
        {
            var @return = Nodes.OfType<FunctionReturnNodeModel>().FirstOrDefault();
            if (@return == null)
            {
                @return = GenerateDefaultReturnNode();
                Nodes.Add(@return);
            }
            return @return;
        }

        public virtual FunctionEntryNodeModel GenerateDefaultEntryNode()
        {
            var entry = new FunctionEntryNodeModel()
            {
                Guid = NodeModel.CreateGuid(),
                NodeType = typeof(FunctionEntryNodeModel).AssemblyQualifiedName,
                Position = new Rect(-100, 0, 0, 0)
            };
            return entry;
        }

        public virtual FunctionReturnNodeModel GenerateDefaultReturnNode()
        {
            var exit = new FunctionReturnNodeModel()
            {
                Guid = NodeModel.CreateGuid(),
                NodeType = typeof(FunctionReturnNodeModel).AssemblyQualifiedName,
                Position = new Rect(100, 0, 0, 0)
            };
            return exit;
        }

        public override object GraphSettingsInspector()
        {
            if (Inspector != null)
            {
                return Inspector;
            }
            Inspector = new InspectorDrawer();

            return Inspector;
        }
    }
}
