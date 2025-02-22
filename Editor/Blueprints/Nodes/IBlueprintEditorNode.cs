using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using Vapor.Blueprints;

namespace VaporEditor.Blueprints
{
    public interface IBlueprintEditorNode
    {
        BlueprintNodeDataModel Model { get; }
        Dictionary<string, BlueprintEditorPort> InPorts { get; }
        Dictionary<string, BlueprintEditorPort> OutPorts { get; }
        BlueprintView View { get; }
        
        void OnConnectedInputEdge(string portName);
        void OnDisconnectedInputEdge(string portName);
    }
}
