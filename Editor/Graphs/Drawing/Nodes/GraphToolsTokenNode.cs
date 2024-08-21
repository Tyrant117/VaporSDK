using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

namespace VaporEditor.Graphs
{
    public class GraphToolsTokenNode<U> : TokenNode, IGraphEditorNode where U : Vapor.Graphs.NodeModel
    {
        private U _node;
        public U Node { get => _node; protected set => _node = value; }

        public Dictionary<string, Port> InPorts { get; set; } = new();
        public Dictionary<string, Port> OutPorts { get; set; } = new();

        public GraphEditorView View { get; protected set; }

        public GraphToolsTokenNode(Port input, Port output) : base(input, output)
        {
        }

        public Vapor.Graphs.NodeModel GetNode() => Node;

        public void OnConnectedInputEdge(string portName)
        {

        }

        public void OnDisconnectedInputEdge(string portName)
        {

        }
    }
}