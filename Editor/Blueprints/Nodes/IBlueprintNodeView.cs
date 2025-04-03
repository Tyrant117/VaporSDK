using System.Collections.Generic;
using Vapor.Blueprints;

namespace VaporEditor.Blueprints
{
    public interface IBlueprintNodeView
    {
        BlueprintNodeController Controller { get; }
        BlueprintView View { get; }
        Dictionary<string, BlueprintPortView> InPorts { get; }
        Dictionary<string, BlueprintPortView> OutPorts { get; }
        
        void OnConnectedInputEdge(BlueprintWireReference wire, bool shouldModifyDataModel);
        void OnDisconnectedInputEdge(string portName);
        void InvalidateName();
        void InvalidateType();
    }
}
