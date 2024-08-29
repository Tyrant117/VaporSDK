using System;
using UnityEngine;

namespace Vapor.VisualScripting
{
    public class SquareRootNode : INode, IReturnNode<double>
    {
        public uint Id { get; }

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

            NodeRef = new SquareRootNode(Guid, new(graph.Get(A).Build(graph), A.PortName));
            return NodeRef;
        }
    }
}
