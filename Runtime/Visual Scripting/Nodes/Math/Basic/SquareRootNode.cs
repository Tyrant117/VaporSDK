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

        public SquareRootNode(string guid, NodePortTuple a)
        {
            Id = guid.GetStableHashU32();
            A = (IReturnNode<double>)a.Node;
            _aPort = a.PortName;
        }

        public double GetValue(IGraphOwner owner, string portName = "")
        {
            return Math.Sqrt(A.GetValue(owner, _aPort));
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

        public override void BuildSlots()
        {
            base.BuildSlots();
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

            NodePortTuple a = sa.Reference.Guid.EmptyOrNull() ? new(new DoubleNode(Guid, (double)sa.Content), string.Empty) : new(graph.Get(sa.Reference).Build(graph), sa.Reference.PortName);
            NodeRef = new SquareRootNode(Guid, a);
            return NodeRef;
        }
    }
}
