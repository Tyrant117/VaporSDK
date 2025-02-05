using System.Collections.Generic;
using System.Linq;
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
        
        protected static string CreateTooltipForPin(BlueprintPin pin)
        {
            if (pin.IsExecutePin)
            {
                return "Execute";
            }
            else
            {
                return pin.Type.IsGenericType ? $"{pin.Type.Name.Split('`')[0]}<{string.Join(",", pin.Type.GetGenericArguments().Select(a => a.Name))}>" : pin.Type.Name;
            }

            // return pin.PinValueType switch
            // {
            //     PinValueType.Value => $"{pin.Type.Name} Pin",
            //     PinValueType.List => $"List<{pin.Type.GetGenericArguments()[0].Name}> Pin",
            //     PinValueType.Dictionary => $"Dictionary<{pin.Type.GetGenericArguments()[0].Name}, {pin.Type.GetGenericArguments()[1].Name}> Pin",
            //     _ => string.Empty
            // };
        }
    }
}
