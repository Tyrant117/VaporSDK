using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vapor.Blueprints
{
    // public struct ReturnNodeType : INodeType
    // {
    //     public BlueprintNodeController CreateDesignNode(Vector2 position, List<(string, object)> parameters)
    //     {
    //         var graph = this.FindParam<BlueprintMethodGraph>(parameters, INodeType.GRAPH_PARAM);
    //         var node = new BlueprintNodeController(this)
    //         {
    //             Graph = graph,
    //             NodeName = "Return",
    //             Position = new Rect(position, Vector2.zero)
    //         };
    //         UpdateDesignNode(node);
    //         return node;
    //     }
    //
    //     public void UpdateDesignNode(BlueprintNodeController nodeController)
    //     {
    //         nodeController.NodeName = "Return";
    //         var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
    //             .WithDisplayName("")
    //             .WithAllowMultipleWires();
    //         nodeController.InPorts.Add(PinNames.EXECUTE_IN, inSlot);
    //
    //         foreach (var parameter in nodeController.Graph.OutputArguments)
    //         {
    //             var slot = new BlueprintPin(parameter.Name, PinDirection.In, parameter.Type, false);
    //             nodeController.InPorts.Add(parameter.Name, slot);
    //         }
    //     }
    //
    //     // public BlueprintCompiledNodeDto Compile(BlueprintDesignNode node)
    //     // {
    //     //     var dto = new BlueprintCompiledNodeDto
    //     //     {
    //     //         NodeType = node.Type,
    //     //         Guid = node.Guid,
    //     //         InputWires = node.InputWires,
    //     //         InputPinValues = new Dictionary<string, (Type, object)>(node.InPorts.Count),
    //     //         OutputPinNames = new List<string>(node.OutPorts.Count),
    //     //         Properties = new Dictionary<string, object>(),
    //     //     };
    //     //     
    //     //     foreach (var inPort in node.InPorts.Values.Where(inPort => inPort.HasInlineValue))
    //     //     {
    //     //         dto.InputPinValues[inPort.PortName] = (inPort.Type, inPort.GetContent());
    //     //     }
    //     //     
    //     //     return dto;
    //     // }
    //
    //     public BlueprintBaseNode Decompile(BlueprintDesignNodeDto dto)
    //     {
    //         return new BlueprintReturnNode(dto);
    //     }
    // }
}