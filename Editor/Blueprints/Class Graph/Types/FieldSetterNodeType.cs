using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Vapor.Blueprints
{
    // public struct FieldSetterNodeType : INodeType
    // {
    //     public BlueprintNodeController CreateDesignNode(Vector2 position, List<(string, object)> parameters)
    //     {
    //         var fieldInfo = this.FindParam<FieldInfo>(parameters, INodeType.FIELD_INFO_PARAM);
    //         var node = new BlueprintNodeController(this)
    //         {
    //             Position = new Rect(position, Vector2.zero)
    //         };
    //         node.AddOrUpdateProperty(NodePropertyNames.FIELD_TYPE, fieldInfo.DeclaringType, true);
    //         node.AddOrUpdateProperty(NodePropertyNames.FIELD_NAME, fieldInfo.Name, true);
    //         UpdateDesignNode(node);
    //         return node;
    //     }
    //     public void UpdateDesignNode(BlueprintNodeController nodeController)
    //     {
    //         nodeController.TryGetProperty<Type>(NodePropertyNames.FIELD_TYPE, out var fieldType);
    //         nodeController.TryGetProperty<string>(NodePropertyNames.FIELD_NAME, out var fieldName);
    //
    //         var fieldInfo = fieldType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    //         nodeController.NodeName = UnityEditor.ObjectNames.NicifyVariableName(fieldInfo.Name);
    //         
    //         var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
    //             .WithDisplayName(string.Empty)
    //             .WithAllowMultipleWires();
    //         nodeController.InPorts.Add(PinNames.EXECUTE_IN, inSlot);
    //         var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
    //             .WithDisplayName(string.Empty);
    //         nodeController.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);
    //
    //         // In Pin
    //         var ownerPin = new BlueprintPin(PinNames.OWNER, PinDirection.In, fieldInfo.DeclaringType, true);
    //         nodeController.InPorts.Add(PinNames.OWNER, ownerPin);
    //
    //         var setterPin = new BlueprintPin(fieldInfo.Name, PinDirection.In, fieldInfo.FieldType, false);
    //         nodeController.InPorts.Add(fieldInfo.Name, setterPin);
    //     }
    //
    //     public BlueprintBaseNode Decompile(BlueprintDesignNodeDto dto)
    //     {
    //         return new BlueprintFieldSetterNode(dto);
    //     }
    // }
}