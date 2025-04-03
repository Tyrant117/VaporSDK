using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.Blueprints
{
//     public struct GraphNodeType : INodeType
//     {
//         public BlueprintNodeController CreateDesignNode(Vector2 position, List<(string, object)> parameters)
//         {
//             var assetGuid = this.FindParam<string>(parameters, INodeType.NAME_DATA_PARAM);
//             var node = new BlueprintNodeController(this)
//             {
//                 Position = new Rect(position, Vector2.zero)
//             };
//             node.AddOrUpdateProperty(INodeType.NAME_DATA_PARAM, assetGuid, true);
//             UpdateDesignNode(node);
//             return node;
//         }
//
//         public void UpdateDesignNode(BlueprintNodeController nodeController)
//         {
// #if UNITY_EDITOR
//             nodeController.TryGetProperty<string>(INodeType.NAME_DATA_PARAM, out var assetGuid);
//             var path = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
//             var found = UnityEditor.AssetDatabase.LoadAssetAtPath<BlueprintGraphSo>(path);
//             Assert.IsTrue(found, $"Graph With Guid [{assetGuid}] Not Found");
//             nodeController.AddOrUpdateProperty(INodeType.KEY_DATA_PARAM, assetGuid, false);
//
//             nodeController.NodeName = UnityEditor.ObjectNames.NicifyVariableName(found.DisplayName);
//
//             // Execute Pins
//             var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
//                 .WithDisplayName(string.Empty)
//                 .WithAllowMultipleWires();
//             nodeController.InPorts.Add(PinNames.EXECUTE_IN, inSlot);
//             var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
//                 .WithDisplayName(string.Empty);
//             nodeController.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);
//
//             // Value Pins
//
//             // Input
//             // foreach (var inputParameter in found.DesignGraph.Current.InputArguments)
//             // {
//             //     string portName = inputParameter.Name;
//             //     string displayName = inputParameter.Name;
//             //     
//             //     displayName = UnityEditor.ObjectNames.NicifyVariableName(inputParameter.Name);
//             //
//             //     var slot = new BlueprintPin(portName, PinDirection.In, inputParameter.Type, false)
//             //         .WithDisplayName(displayName);
//             //     node.InPorts.Add(portName, slot);
//             // }
//
//             // Output
//             // foreach (var outputParameter in found.DesignGraph.Current.OutputArguments)
//             // {
//             //     string portName = outputParameter.Name;
//             //     string displayName = outputParameter.Name;
//             //     
//             //     displayName = UnityEditor.ObjectNames.NicifyVariableName(outputParameter.Name);
//             //     
//             //     var type = outputParameter.Type;
//             //     if (type.IsByRef)
//             //     {
//             //         type = type.GetElementType();
//             //     }
//             //
//             //     var slot = new BlueprintPin(portName, PinDirection.Out, type, false)
//             //         .WithDisplayName(displayName)
//             //         .WithAllowMultipleWires();
//             //     node.OutPorts.Add(portName, slot);
//             // }
// #endif
//         }
//
//         public BlueprintBaseNode Decompile(BlueprintDesignNodeDto dto)
//         {
//             return new BlueprintGraphNode(dto);
//         }
//     }
}