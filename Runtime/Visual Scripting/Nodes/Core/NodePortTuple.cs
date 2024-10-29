using UnityEngine;

namespace Vapor.VisualScripting
{
    public readonly struct NodePortTuple
    {
        public readonly INode Node;
        public readonly string PortName;
        public readonly int Index;

        public NodePortTuple(INode node, string portName, int index)
        {
            Node = node;
            PortName = portName;
            Index = index;
        }
    }
}
