using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.Blueprints
{
    // public struct TemporaryDataSetterNodeType : INodeType
    // {
    //     public BlueprintNodeController CreateDesignNode(Vector2 position, List<(string, object)> parameters)
    //     {
    //         var graph = this.FindParam<BlueprintMethodGraph>(parameters, INodeType.GRAPH_PARAM);
    //         var name = this.FindParam<string>(parameters, INodeType.VARIABLE_NAME_PARAM);
    //         var node = new BlueprintNodeController(this)
    //         {
    //             Graph = graph,
    //             Position = new Rect(position, Vector2.zero)
    //         };
    //         node.AddOrUpdateProperty(NodePropertyNames.VARIABLE_NAME, name, true);
    //         UpdateDesignNode(node);
    //         return node;
    //     }
    //
    //     public void UpdateDesignNode(BlueprintNodeController nodeController)
    //     {
    //         nodeController.TryGetProperty<string>(NodePropertyNames.VARIABLE_NAME, out var tempFieldName);
    //         var tempData = nodeController.Graph.TemporaryVariables.FirstOrDefault(x => x.Name == tempFieldName);
    //         if (tempData == null)
    //         {
    //             Debug.LogError($"{tempFieldName} not found in graph, was the variable deleted?");
    //             nodeController.SetError($"{tempFieldName} not found in graph, was the variable deleted?");
    //             nodeController.NodeName = $"Set <b><i>{tempFieldName}</i></b>";
    //             return;
    //         }
    //         
    //         nodeController.NodeName = $"Set <b><i>{tempData.Name}</i></b>";
    //         
    //         var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
    //             .WithDisplayName(string.Empty)
    //             .WithAllowMultipleWires();
    //         nodeController.InPorts.Add(PinNames.EXECUTE_IN, inSlot);
    //
    //         var inData = new BlueprintPin(tempData.Name, PinDirection.In, tempData.Type, false)
    //             .WithDisplayName(string.Empty);
    //         nodeController.InPorts.Add(tempData.Name, inData);
    //
    //         var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
    //             .WithDisplayName(string.Empty);
    //         nodeController.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);
    //
    //         var os = new BlueprintPin(tempData.Name, PinDirection.Out, tempData.Type, false)
    //             .WithDisplayName(string.Empty)
    //             .WithAllowMultipleWires();
    //         nodeController.OutPorts.Add(tempData.Name, os);
    //     }
    //
    //     public BlueprintBaseNode Decompile(BlueprintDesignNodeDto dto)
    //     {
    //         return new BlueprintSetterNode(dto);
    //     }
    // }
}