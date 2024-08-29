using System;

namespace Vapor.VisualScripting
{
    public class SubtractNode : INode, IReturnNode<double>
    {
        public uint Id { get; }

        public readonly IReturnNode<double> A;
        public readonly IReturnNode<double> B;

        private readonly string _aPort;
        private readonly string _bPort;

        public SubtractNode(string guid, NodePortTuple a, NodePortTuple b)
        {
            Id = guid.GetStableHashU32();
            A = (IReturnNode<double>)a.Node;
            B = (IReturnNode<double>)b.Node;
            _aPort = a.PortName;
            _bPort = b.PortName;
        }

        public double GetValue(IGraphOwner owner, string portName = "")
        {
            return A.GetValue(owner, _aPort) - B.GetValue(owner, _bPort);
        }
    }

    [SearchableNode("Math/Basic/Subtract", "Subtract", "math")]
    public class SubtractNodeModel : NodeModel
    {
        [PortIn("A", 0, true, typeof(double))]
        public NodeReference A;
        [PortIn("B", 1, true, typeof(double))]
        public NodeReference B;

        [PortOut("Out", 0, true, typeof(double))]
        public NodeReference Out;

        public override INode Build(GraphModel graph)
        {
            if (NodeRef != null)
            {
                return NodeRef;
            }

            NodeRef = new SubtractNode(Guid, new(graph.Get(A).Build(graph), A.PortName), new(graph.Get(B).Build(graph), B.PortName));
            return NodeRef;
        }
    }
}
