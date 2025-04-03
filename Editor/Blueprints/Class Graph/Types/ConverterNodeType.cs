using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.Blueprints
{
    // public struct ConverterNodeType : INodeType
    // {
    //     public BlueprintNodeController CreateDesignNode(Vector2 position, List<(string, object)> parameters)
    //     {
    //         var methodInfo = this.FindParam<MethodInfo>(parameters, INodeType.METHOD_INFO_PARAM);
    //         Assert.IsTrue(methodInfo.DeclaringType != null, $"{methodInfo.Name} DeclaringType is null");
    //         var node = new BlueprintNodeController(this)
    //         {
    //             NodeName = string.Empty,
    //             Position = new Rect(position, Vector2.zero)
    //         };
    //         node.AddOrUpdateProperty(NodePropertyNames.K_METHOD_DECLARING_TYPE, methodInfo.DeclaringType, true);
    //         node.AddOrUpdateProperty(NodePropertyNames.K_METHOD_NAME, methodInfo.Name, true);
    //         node.AddOrUpdateProperty(NodePropertyNames.K_METHOD_PARAMETER_TYPES, methodInfo.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName).ToArray(), true);
    //         UpdateDesignNode(node);
    //         return node;
    //     }
    //
    //     public void UpdateDesignNode(BlueprintNodeController nodeController)
    //     {
    //         nodeController.TryGetProperty<Type>(NodePropertyNames.K_METHOD_DECLARING_TYPE, out var methodAssemblyType);
    //         nodeController.TryGetProperty<string>(NodePropertyNames.K_METHOD_NAME, out var methodName);
    //         nodeController.TryGetProperty<string[]>(NodePropertyNames.K_METHOD_PARAMETER_TYPES, out var methodParameterTypes);
    //         
    //         var methodInfo = RuntimeReflectionUtility.GetMethodInfo(methodAssemblyType, methodName, methodParameterTypes);
    //         nodeController.AddOrUpdateProperty(NodePropertyNames.K_METHOD_INFO, methodInfo, false);
    //         
    //         var atr = methodInfo.GetCustomAttribute<BlueprintPinConverterAttribute>();
    //
    //         var slot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, atr.SourceType, false)
    //             .WithDisplayName(string.Empty);
    //         nodeController.InPorts.Add(PinNames.EXECUTE_IN, slot);
    //
    //         var outSlot = new BlueprintPin(PinNames.RETURN, PinDirection.Out, atr.TargetType, false)
    //             .WithDisplayName(string.Empty)
    //             .WithAllowMultipleWires();
    //         nodeController.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);
    //     }
    //
    //     public BlueprintBaseNode Decompile(BlueprintDesignNodeDto dto)
    //     {
    //         return new BlueprintConverterNode(dto);
    //     }
    // }
}