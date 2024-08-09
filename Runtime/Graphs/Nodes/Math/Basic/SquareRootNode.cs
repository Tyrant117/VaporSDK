using System;
using UnityEngine;

namespace Vapor.Graphs
{
    public class SquareRootNode : INode, IReturnNode<double>
    {
        public uint Id { get; }

        public readonly IReturnNode<double> A;

        private readonly int _aPort;

        public SquareRootNode(string guid, NodePortTuple a)
        {
            Id = guid.GetStableHashU32();
            A = (IReturnNode<double>)a.Node;
            _aPort = a.Port;
        }

        public double GetValue(IGraphOwner owner, int portIndex = 0)
        {
            return Math.Sqrt(A.GetValue(owner, _aPort));
        }
    }

    [SearchableNode("Math/Basic/Square Root", "Square Root", "math")]
    public class SquareRootNodeModel : NodeModel
    {
        [PortIn("A", 0, true, typeof(double))]
        public NodeReference A;

        [PortOut("Out", 0, true, typeof(double))]
        public NodeReference Out;

        public override INode Build(GraphModel graph)
        {
            if (NodeRef != null)
            {
                return NodeRef;
            }

            NodeRef = new SquareRootNode(Guid, new(graph.Get(A).Build(graph), A.PortIndex));
            return NodeRef;
        }
    }
}
