using UnityEngine;

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

    [System.Serializable]
    public class MathGraphModel : GraphModel
    {
        public MathGraphModel()
        {
            AssemblyQualifiedType = GetType();
        }

        public override IGraph Build(bool refresh = false)
        {
            if (refresh)
            {
                Root.Refresh();
                foreach (var c in Children)
                {
                    c.Refresh();
                }
            }

            var root = Root.Build(this);
            return new MathGraph(root);
        }

        public override NodeModel GenerateDefaultRootNode()
        {
            var root = new MathEvaluateNodeModel
            {
                Guid = NodeModel.CreateGuid(),
                NodeType = typeof(MathEvaluateNodeModel).AssemblyQualifiedName
            };
            return root;
        }
    }
}
