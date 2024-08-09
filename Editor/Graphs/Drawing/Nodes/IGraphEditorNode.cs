using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using NodeModel = Vapor.Graphs.NodeModel;

namespace VaporEditor.Graphs
{
    public interface IGraphEditorNode
    {
        List<Port> InPorts { get; }
        List<Port> OutPorts { get; }

        NodeModel GetNode();

        void OnConnectedInputEdge(int index);
        void OnDisconnectedInputEdge(int index);
    }
}
