using System;
using Vapor.Inspector;

namespace Vapor.Graphs
{
    public class AddNode : INode, IReturnNode<double>
    {
        public uint Id { get; }

        public readonly IReturnNode<double> A;
        public readonly IReturnNode<double> B;

        private readonly int _aPort;
        private readonly int _bPort;

        public AddNode(string guid, NodePortTuple a, NodePortTuple b)
        {
            Id = guid.GetStableHashU32();
            A = (IReturnNode<double>)a.Node;
            _aPort = a.Port;
            B = (IReturnNode<double>)b.Node;
            _bPort = b.Port;
        }

        public double GetValue(IGraphOwner owner, int portIndex)
        {
            return A.GetValue(owner, _aPort) + B.GetValue(owner, _bPort);
        }
    }

    [Serializable, SearchableNode("Math/Basic/Add", "Add", "math")]
    public class AddNodeData : NodeModel
    {
        private const string k_A = "a";
        private const string k_B = "b";
        private const string k_Result = "result";

        [PortIn("A", 0, false, typeof(double))]
        public NodeReference A;
        [PortContent(0)]
        public double AVal;
        [PortIn("B", 1, false, typeof(double))]
        public NodeReference B;
        [PortContent(1)]
        public double BVal;

        public override void BuildSlots()
        {
            base.BuildSlots();
            InSlots.Add(new PortSlot(k_A, "A", PortDirection.In, typeof(double))
                .WithContent(typeof(double), 0));
            InSlots.Add(new PortSlot(k_B, "B", PortDirection.In, typeof(double))
                .WithContent(typeof(double), 0));

            OutSlots.Add(new PortSlot(k_Result, "Result", PortDirection.Out, typeof(double))
                .CanAllowMultiple()
                .IsOptional());
        }

        public override INode Build(GraphModel graph)
        {
            if (NodeRef != null)
            {
                return NodeRef;
            }

            NodePortTuple a = A.Guid.EmptyOrNull() ? new(new DoubleNode(Guid, AVal), 0) : new(graph.Get(A).Build(graph), A.PortIndex);
            NodePortTuple b = B.Guid.EmptyOrNull() ? new(new DoubleNode(Guid, BVal), 0) : new(graph.Get(B).Build(graph), B.PortIndex);
            NodeRef = new AddNode(Guid, a, b);
            return NodeRef;
        }
    }
}
