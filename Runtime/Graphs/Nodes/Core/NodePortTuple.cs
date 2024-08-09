using UnityEngine;

namespace Vapor.Graphs
{
    public readonly struct NodePortTuple
    {
        public readonly INode Node;
        public readonly int Port;

        public NodePortTuple(INode node, int port)
        {
            Node = node;
            Port = port;
        }
    }
}
