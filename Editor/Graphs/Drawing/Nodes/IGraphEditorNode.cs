using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using NodeModel = Vapor.Graphs.NodeModel;

namespace VaporEditor.Graphs
{
    public interface IGraphEditorNode
    {
        Dictionary<string, Port> InPorts { get; }
        Dictionary<string, Port> OutPorts { get; }

        NodeModel GetNode();

        void OnConnectedInputEdge(string portName);
        void OnDisconnectedInputEdge(string portName);
    }
}
