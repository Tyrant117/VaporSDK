using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using Vapor.Blueprints;

namespace VaporEditor.Blueprints
{
    public interface IBlueprintEditorNode
    {
        BlueprintNodeDataModel Node { get; }
        Dictionary<string, Port> InPorts { get; }
        Dictionary<string, Port> OutPorts { get; }
        BlueprintEditorView View { get; }

        void RedrawPorts(EdgeConnectorListener edgeConnectorListener);

        void OnConnectedInputEdge(string portName);
        void OnDisconnectedInputEdge(string portName);
    }
}
