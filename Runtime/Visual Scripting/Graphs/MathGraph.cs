using Newtonsoft.Json;
using System;
using System.Linq;
using UnityEngine;

namespace Vapor.VisualScripting
{
    public class MathGraph : IEvaluateToValueGraph<double>
    {
        public uint Id { get; }

        public readonly MathReturnNode Root;

        public double Value { get; set; }

        public MathGraph(INode root)
        {
            Root = (MathReturnNode)root;
            Traverse(SetGraph);
        }

        public void Evaluate(IGraphOwner graphOwner)
        {
            Value = Root.GetValue(graphOwner, -1);
        }

        public void Traverse(Action<INode> callback)
        {
            Root.Traverse(callback);
        }

        private void SetGraph(INode node)
        {
            node.Graph = this;
        }
    }

    [Serializable]
    public class MathGraphModel : GraphModel
    {
        [Serializable]
        public class InspectorDrawer
        {

        }

        [JsonIgnore]
        public InspectorDrawer Inspector { get; set; }

        public override IGraph Build(bool refresh = false, string debugName = "")
        {
            DebugName = debugName;
            if (refresh)
            {
                foreach (var c in Nodes)
                {
                    c.Refresh();
                }
            }

            var @return = GetReturnNode();
            var root = @return.Build(this);

            // Create an instance of FunctionGraph2<MyStruct>
            return new MathGraph(root);
        }

        public MathReturnNodeModel GetReturnNode()
        {
            var @return = Nodes.OfType<MathReturnNodeModel>().FirstOrDefault();
            if (@return == null)
            {
                @return = GenerateDefaultReturnNode();
                Nodes.Add(@return);
            }
            return @return;
        }

        public virtual MathReturnNodeModel GenerateDefaultReturnNode()
        {
            var exit = new MathReturnNodeModel()
            {
                Guid = NodeModel.CreateGuid(),
                NodeType = typeof(MathReturnNodeModel).AssemblyQualifiedName,
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
