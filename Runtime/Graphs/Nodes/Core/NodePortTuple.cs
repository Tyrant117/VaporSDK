using UnityEngine;

namespace Vapor.Graphs
{
    public readonly struct NodePortTuple
    {
        public readonly INode Node;
        public readonly string PortName;

        public NodePortTuple(INode node, string portName)
        {
            Node = node;
            PortName = portName;
        }
    }
}
