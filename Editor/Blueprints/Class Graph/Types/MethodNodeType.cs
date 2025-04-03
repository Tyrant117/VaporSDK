using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
//     public struct MethodNodeType : INodeType
//     {
//         public BlueprintNodeController CreateDesignNode(Vector2 position, List<(string, object)> parameters)
//         {
//             var methodInfo = this.FindParam<MethodInfo>(parameters, INodeType.METHOD_INFO_PARAM);
//             var node = new BlueprintNodeController(this)
//             {
//                 Position = new Rect(position, Vector2.zero)
//             };
//             node.AddOrUpdateProperty(NodePropertyNames.K_METHOD_DECLARING_TYPE, methodInfo.DeclaringType, true);
//             node.AddOrUpdateProperty(NodePropertyNames.K_METHOD_NAME, methodInfo.Name, true);
//             node.AddOrUpdateProperty(NodePropertyNames.K_METHOD_PARAMETER_TYPES, methodInfo.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName).ToArray(), true);
//             UpdateDesignNode(node);
//             return node;
//         }
//         public void UpdateDesignNode(BlueprintNodeController nodeController)
//         {
// #if UNITY_EDITOR
//             nodeController.TryGetProperty<Type>(NodePropertyNames.K_METHOD_DECLARING_TYPE, out var methodAssemblyType);
//             nodeController.TryGetProperty<string>(NodePropertyNames.K_METHOD_NAME, out var methodName);
//             nodeController.TryGetProperty<string[]>(NodePropertyNames.K_METHOD_PARAMETER_TYPES, out var methodParameterTypes);
//             
//             var methodInfo = RuntimeReflectionUtility.GetMethodInfo(methodAssemblyType, methodName, methodParameterTypes);
//             nodeController.AddOrUpdateProperty(NodePropertyNames.K_METHOD_INFO, methodInfo, false);
//             var paramInfos = methodInfo.GetParameters();
//             bool hasOutParameter = paramInfos.Any(p => p.IsOut);
//             var callableAttribute = methodInfo.GetCustomAttribute<BlueprintCallableAttribute>();
//             
//             var nodeName = methodInfo.IsSpecialName ? methodInfo.Name.ToTitleCase() : methodInfo.Name;
//             nodeController.NodeName = callableAttribute == null || callableAttribute.NodeName.EmptyOrNull() ? UnityEditor.ObjectNames.NicifyVariableName(nodeName) : callableAttribute.NodeName;
//             
//             if (methodInfo.ReturnType == typeof(void) || hasOutParameter)
//             {
//                 var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
//                     .WithDisplayName(string.Empty)
//                     .WithAllowMultipleWires();
//                 nodeController.InPorts.Add(PinNames.EXECUTE_IN, inSlot);
//                 var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
//                     .WithDisplayName(string.Empty);
//                 nodeController.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);
//             }
//
//             if (!methodInfo.IsStatic)
//             {
//                 var slot = new BlueprintPin(PinNames.OWNER, PinDirection.In, methodInfo.DeclaringType, false);
//                 nodeController.InPorts.Add(PinNames.OWNER, slot);
//             }
//
//             if (methodInfo.ReturnType != typeof(void))
//             {
//                 var retParam = methodInfo.ReturnParameter;
//                 if (retParam is { IsRetval: true })
//                 {
//                     // Out Ports
//                     var slot = new BlueprintPin(PinNames.RETURN, PinDirection.Out, retParam.ParameterType, false)
//                         .WithAllowMultipleWires();
//                     nodeController.OutPorts.Add(PinNames.RETURN, slot);
//                 }
//             }
//
//             foreach (var pi in paramInfos)
//             {
//                 if (pi.IsOut)
//                 {
//                     // Out Ports
//                     var paramAttribute = pi.GetCustomAttribute<BlueprintParamAttribute>();
//                     bool isWildcard = false;
//                     string portName = pi.Name;
//                     var displayName = UnityEditor.ObjectNames.NicifyVariableName(pi.Name);
//                     
//                     if (paramAttribute != null)
//                     {
//                         isWildcard = paramAttribute.WildcardTypes != null;
//                         if (!paramAttribute.Name.EmptyOrNull())
//                         {
//                             displayName = paramAttribute.Name;
//                         }
//                     }
//
//                     var type = pi.ParameterType;
//                     if (type.IsByRef)
//                     {
//                         type = type.GetElementType();
//                     }
//
//                     var slot = new BlueprintPin(portName, PinDirection.Out, type, false)
//                         .WithDisplayName(displayName)
//                         .WithAllowMultipleWires();
//                     if (isWildcard)
//                     {
//                         slot.WithWildcardTypes(paramAttribute.WildcardTypes);
//                     }
//                     nodeController.OutPorts.Add(portName, slot);
//                 }
//                 else
//                 {
//                     // In Ports
//                     var paramAttribute = pi.GetCustomAttribute<BlueprintParamAttribute>();
//                     bool isWildcard = false;
//                     string portName = pi.Name;
//                     var displayName = UnityEditor.ObjectNames.NicifyVariableName(pi.Name);
//                     
//                     if (paramAttribute != null)
//                     {
//                         isWildcard = paramAttribute.WildcardTypes != null;
//                         if (!paramAttribute.Name.EmptyOrNull())
//                         {
//                             displayName = paramAttribute.Name;
//                         }
//                     }
//
//                     var slot = new BlueprintPin(portName, PinDirection.In, pi.ParameterType, false)
//                         .WithDisplayName(displayName)
//                         .WithIsOptional();
//                     if (isWildcard)
//                     {
//                         slot.WithWildcardTypes(paramAttribute.WildcardTypes);
//                     }
//                     if (pi.HasDefaultValue && slot.HasInlineValue)
//                     {
//                         slot.SetDefaultValue(pi.DefaultValue);
//                     }
//
//                     nodeController.InPorts.Add(portName, slot);
//                 }
//             }
//
// #else
//             Debug.LogError("UpdateDesignNode Called In Runtime Build");
// #endif
//         }
//
//         public BlueprintBaseNode Decompile(BlueprintDesignNodeDto dto)
//         {
//             return new BlueprintMethodNode(dto);
//         }
//     }
}