using System.Collections.Generic;
using Vapor.Blueprints;

namespace VaporEditor.Blueprints
{
    public interface IBlueprintNodeView
    {
        NodeModelBase Controller { get; }
        BlueprintView View { get; }
        Dictionary<string, BlueprintPortView> InPorts { get; }
        Dictionary<string, BlueprintPortView> OutPorts { get; }
        
        void InvalidateName();
        void InvalidateType();
    }
}
