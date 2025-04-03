using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Vapor.Blueprints
{
//     public struct FieldGetterNodeType : INodeType
//     {
//         public BlueprintNodeController CreateDesignNode(Vector2 position, List<(string, object)> parameters)
//         {
//             var fieldInfo = this.FindParam<FieldInfo>(parameters, INodeType.FIELD_INFO_PARAM);
//             var node = new BlueprintNodeController(this)
//             {
//                 Position = new Rect(position, Vector2.zero)
//             };
//             node.AddOrUpdateProperty(NodePropertyNames.FIELD_TYPE, fieldInfo.DeclaringType, true);
//             node.AddOrUpdateProperty(NodePropertyNames.FIELD_NAME, fieldInfo.Name, true);
//             UpdateDesignNode(node);
//             return node;
//         }
//         public void UpdateDesignNode(BlueprintNodeController nodeController)
//         {
// #if UNITY_EDITOR
//             nodeController.TryGetProperty<Type>(NodePropertyNames.FIELD_TYPE, out var fieldType);
//             nodeController.TryGetProperty<string>(NodePropertyNames.FIELD_NAME, out var fieldName);
//
//             var fieldInfo = fieldType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
//             nodeController.NodeName = ObjectNames.NicifyVariableName(fieldInfo.Name);
//
//             // In Pin
//             var ownerPin = new BlueprintPin(PinNames.OWNER, PinDirection.In, fieldInfo.DeclaringType, true);
//             nodeController.InPorts.Add(PinNames.OWNER, ownerPin);
//
//             // Out Pin
//             var returnPin = new BlueprintPin(PinNames.RETURN, PinDirection.Out, fieldInfo.FieldType, false)
//                 .WithAllowMultipleWires();
//             nodeController.OutPorts.Add(PinNames.RETURN, returnPin);
// #endif
//         }
//
//         public BlueprintBaseNode Decompile(BlueprintDesignNodeDto dto)
//         {
//             return new BlueprintFieldGetterNode(dto);
//         }
//     }
}