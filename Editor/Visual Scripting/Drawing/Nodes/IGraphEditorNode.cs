using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using NodeModel = Vapor.VisualScripting.NodeModel;

namespace VaporEditor.VisualScripting
{
    public interface IGraphEditorNode
    {
        Dictionary<string, Port> InPorts { get; }
        Dictionary<string, Port> OutPorts { get; }

        NodeModel GetNode();
        void RedrawPorts(EdgeConnectorListener edgeConnectorListener);

        void OnConnectedInputEdge(string portName);
        void OnDisconnectedInputEdge(string portName);
    }
}
