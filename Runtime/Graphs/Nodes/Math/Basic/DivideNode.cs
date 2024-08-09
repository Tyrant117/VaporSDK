using System;

namespace Vapor.Graphs
{
    public class DivideNode : INode, IReturnNode<double>
    {
        public uint Id { get; }

        public readonly IReturnNode<double> A;
        public readonly IReturnNode<double> B;

        private readonly int _aPort;
        private readonly int _bPort;

        public DivideNode(string guid, NodePortTuple a, NodePortTuple b)
        {
            Id = guid.GetStableHashU32();
            A = (IReturnNode<double>)a.Node;
            B = (IReturnNode<double>)b.Node;
            _aPort = a.Port;
            _bPort = b.Port;

        }

        public double GetValue(IGraphOwner owner, int portIndex = 0)
        {
            return A.GetValue(owner, _aPort) / B.GetValue(owner, _bPort);
        }
    }

    [SearchableNode("Math/Basic/Divide", "Divide", "math")]
    public class DivideNodeModel : NodeModel
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

            NodeRef = new DivideNode(Guid, new(graph.Get(A).Build(graph), A.PortIndex), new(graph.Get(B).Build(graph), B.PortIndex));
            return NodeRef;
        }
    }
}
