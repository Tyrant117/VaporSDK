using System;
using Vapor.Inspector;

namespace Vapor.VisualScripting
{
    public class SquareRootNode : IReturnNode<double>
    {
        public uint Id { get; }
        public IGraph Graph { get; set; }

        public readonly IReturnNode<double> A;

        private readonly string _aPort;
        private readonly int _aPortIndex;

        public SquareRootNode(string guid, NodePortTuple a)
        {
            Id = guid.GetStableHashU32();
            A = (IReturnNode<double>)a.Node;
            _aPort = a.PortName;
            _aPortIndex = a.Index;
        }

        public object GetBoxedValue(IGraphOwner owner, int portIndex)
        {
            return GetValue(owner, portIndex);
        }

        public double GetValue(IGraphOwner owner, int portIndex)
        {
            return Math.Sqrt(A.GetValue(owner, _aPortIndex));
        }

        public void Traverse(Action<INode> callback)
        {
            A.Traverse(callback);
            callback(this);
        }
    }

    [Serializable, SearchableNode("Math/Basic/Square Root", "Square Root", "math")]
    public class SquareRootNodeModel : NodeModel
    {
        private const string k_A = "a";
        private const string k_Result = "result";

        protected override void BuildAdditionalSlots()
        {
            InSlots.TryAdd(k_A, new PortSlot(k_A, "A", PortDirection.In, typeof(double))
                .WithContent<double>(0));

            OutSlots.TryAdd(k_Result, new PortSlot(k_Result, "Result", PortDirection.Out, typeof(double))
                .SetAllowMultiple()
                .SetIsOptional());
        }

        public override INode Build(GraphModel graph)
        {
            if (NodeRef != null)
            {
                return NodeRef;
            }

            var sa = InSlots[k_A];

            NodePortTuple a = sa.Reference.Guid.EmptyOrNull() ? new(new DoubleNode(Guid, (double)sa.Content), string.Empty, 0) : new(graph.Get(sa.Reference).Build(graph), sa.Reference.PortName, 0);
            NodeRef = new SquareRootNode(Guid, a);
            return NodeRef;
        }
    }
}
