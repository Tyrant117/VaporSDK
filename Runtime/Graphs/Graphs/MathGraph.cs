using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Inspector;

namespace Vapor.Graphs
{
    public class MathGraph : IGraph, IEvaluatorNode<double, IExternalValueSource>
    {
        public uint Id { get; }

        public readonly IEvaluatorNode<double, IExternalValueSource> Root;

        public MathGraph(INode root)
        {
            Root = (IEvaluatorNode<double, IExternalValueSource>)root;
        }        

        public double Evaluate(GraphModel graph, IExternalValueSource arg)
        {
            return Root.Evaluate(graph, arg);
        }
    }

    [Serializable]
    public class MathGraphModel : GraphModel
    {
        [Serializable]
        public class InspectorDrawer
        {
            public List<string> Input;
            public List<string> Output;
        }

        [JsonIgnore]
        public InspectorDrawer Inspector;
        [JsonIgnore]
        public FieldInfo[] InspectorFields;

        public MathGraphModel()
        {
            AssemblyQualifiedType = GetType();
        }

        public override IGraph Build(bool refresh = false)
        {
            if (refresh)
            {
                Entry.Refresh();
                foreach (var c in Children)
                {
                    c.Refresh();
                }
            }

            var root = Entry.Build(this);
            return new MathGraph(root);
        }

        public override NodeModel GenerateDefaultEntryNode()
        {
            var exit = new FunctionEntryNodeModel()
            {
                Guid = NodeModel.CreateGuid(),
                NodeType = typeof(FunctionEntryNodeModel).AssemblyQualifiedName,
                Position = new Rect(-100, 0, 0, 0)
            };
            return exit;
        }

        public override NodeModel GenerateDefaultExitNode()
        {
            var exit = new FunctionReturnNodeModel()
            {
                Guid = NodeModel.CreateGuid(),
                NodeType = typeof(FunctionReturnNodeModel).AssemblyQualifiedName,
                Position = new Rect(100, 0, 0, 0)
            };
            //var root = new MathEvaluateNodeModel
            //{
            //    Guid = NodeModel.CreateGuid(),
            //    NodeType = typeof(MathEvaluateNodeModel).AssemblyQualifiedName
            //};
            return exit;
        }

        public override object ElementToDraw(out FieldInfo[] fields)
        {
            if (Inspector != null)
            {
                fields = InspectorFields;
                return Inspector;
            }

            Inspector = new InspectorDrawer();
            InspectorFields = typeof(InspectorDrawer).GetFields(BindingFlags.Public | BindingFlags.Instance);
            fields = InspectorFields;
            return Inspector;
        }
    }
}
