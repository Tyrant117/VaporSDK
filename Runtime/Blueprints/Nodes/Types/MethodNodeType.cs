using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public struct MethodNodeType : INodeType
    {
        public BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters)
        {
            var methodInfo = this.FindParam<MethodInfo>(parameters, INodeType.METHOD_INFO_PARAM);
            var node = new BlueprintDesignNode(this)
            {
                Position = new Rect(position, Vector2.zero)
            };
            node.AddOrUpdateProperty(BlueprintDesignNode.K_METHOD_DECLARING_TYPE, methodInfo.DeclaringType, true);
            node.AddOrUpdateProperty(BlueprintDesignNode.K_METHOD_NAME, methodInfo.Name, true);
            node.AddOrUpdateProperty(BlueprintDesignNode.K_METHOD_PARAMETER_TYPES, methodInfo.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName).ToArray(), true);
            UpdateDesignNode(node);
            return node;
        }
        public void UpdateDesignNode(BlueprintDesignNode node)
        {
#if UNITY_EDITOR
            node.TryGetProperty<Type>(BlueprintDesignNode.K_METHOD_DECLARING_TYPE, out var methodAssemblyType);
            node.TryGetProperty<string>(BlueprintDesignNode.K_METHOD_NAME, out var methodName);
            node.TryGetProperty<string[]>(BlueprintDesignNode.K_METHOD_PARAMETER_TYPES, out var methodParameterTypes);
            
            var methodInfo = GetMethodInfo(methodAssemblyType, methodName, methodParameterTypes);
            node.AddOrUpdateProperty(BlueprintDesignNode.K_METHOD_INFO, methodInfo, false);
            var paramInfos = methodInfo.GetParameters();
            bool hasOutParameter = paramInfos.Any(p => p.IsOut);
            var callableAttribute = methodInfo.GetCustomAttribute<BlueprintCallableAttribute>();
            
            var nodeName = methodInfo.IsSpecialName ? BlueprintNodeDataModelUtility.ToTitleCase(methodInfo.Name) : methodInfo.Name;
            node.NodeName = callableAttribute == null || callableAttribute.NodeName.EmptyOrNull() ? UnityEditor.ObjectNames.NicifyVariableName(nodeName) : callableAttribute.NodeName;
            
            if (methodInfo.ReturnType == typeof(void) || hasOutParameter)
            {
                var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                    .WithDisplayName(string.Empty)
                    .WithAllowMultipleWires();
                node.InPorts.Add(PinNames.EXECUTE_IN, inSlot);
                var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                    .WithDisplayName(string.Empty);
                node.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);
            }

            if (!methodInfo.IsStatic)
            {
                var slot = new BlueprintPin(PinNames.OWNER, PinDirection.In, methodInfo.DeclaringType, false);
                node.InPorts.Add(PinNames.OWNER, slot);
            }

            if (methodInfo.ReturnType != typeof(void))
            {
                var retParam = methodInfo.ReturnParameter;
                if (retParam is { IsRetval: true })
                {
                    // Out Ports
                    var slot = new BlueprintPin(PinNames.RETURN, PinDirection.Out, retParam.ParameterType, false)
                        .WithAllowMultipleWires();
                    node.OutPorts.Add(PinNames.RETURN, slot);
                }
            }

            foreach (var pi in paramInfos)
            {
                if (pi.IsOut)
                {
                    // Out Ports
                    var paramAttribute = pi.GetCustomAttribute<BlueprintParamAttribute>();
                    bool isWildcard = false;
                    string portName = pi.Name;
                    var displayName = UnityEditor.ObjectNames.NicifyVariableName(pi.Name);
                    
                    if (paramAttribute != null)
                    {
                        isWildcard = paramAttribute.WildcardTypes != null;
                        if (!paramAttribute.Name.EmptyOrNull())
                        {
                            displayName = paramAttribute.Name;
                        }
                    }

                    var type = pi.ParameterType;
                    if (type.IsByRef)
                    {
                        type = type.GetElementType();
                    }

                    var slot = new BlueprintPin(portName, PinDirection.Out, type, false)
                        .WithDisplayName(displayName)
                        .WithAllowMultipleWires();
                    if (isWildcard)
                    {
                        slot.WithWildcardTypes(paramAttribute.WildcardTypes);
                    }
                    node.OutPorts.Add(portName, slot);
                }
                else
                {
                    // In Ports
                    var paramAttribute = pi.GetCustomAttribute<BlueprintParamAttribute>();
                    bool isWildcard = false;
                    string portName = pi.Name;
                    var displayName = UnityEditor.ObjectNames.NicifyVariableName(pi.Name);
                    
                    if (paramAttribute != null)
                    {
                        isWildcard = paramAttribute.WildcardTypes != null;
                        if (!paramAttribute.Name.EmptyOrNull())
                        {
                            displayName = paramAttribute.Name;
                        }
                    }

                    var slot = new BlueprintPin(portName, PinDirection.In, pi.ParameterType, false)
                        .WithDisplayName(displayName)
                        .WithIsOptional();
                    if (isWildcard)
                    {
                        slot.WithWildcardTypes(paramAttribute.WildcardTypes);
                    }
                    if (pi.HasDefaultValue && slot.HasInlineValue)
                    {
                        slot.SetDefaultValue(pi.DefaultValue);
                    }

                    node.InPorts.Add(portName, slot);
                }
            }

#else
            Debug.LogError("UpdateDesignNode Called In Runtime Build");
#endif
        }

        public BlueprintCompiledNodeDto Compile(BlueprintDesignNode node)
        {
            var dto = new BlueprintCompiledNodeDto
            {
                NodeType = node.Type,
                Guid = node.Guid,
                InputWires = node.InputWires,
                InputPinValues = new Dictionary<string, (Type, object)>(node.InPorts.Count),
                OutputPinNames = new List<string>(node.OutPorts.Count),
                Properties = new Dictionary<string, object>(),
            };
            
            node.TryGetProperty<Type>(BlueprintDesignNode.K_METHOD_DECLARING_TYPE, out var methodAssemblyType);
            node.TryGetProperty<string>(BlueprintDesignNode.K_METHOD_NAME, out var methodName);
            node.TryGetProperty<string[]>(BlueprintDesignNode.K_METHOD_PARAMETER_TYPES, out var methodParameterTypes);
            
            dto.Properties.TryAdd(BlueprintDesignNode.K_METHOD_DECLARING_TYPE, methodAssemblyType);
            dto.Properties.TryAdd(BlueprintDesignNode.K_METHOD_NAME, methodName);
            dto.Properties.TryAdd(BlueprintDesignNode.K_METHOD_PARAMETER_TYPES, methodParameterTypes);
            
            foreach (var inPort in node.InPorts.Values)
            {
                if (inPort.HasInlineValue)
                {
                    dto.InputPinValues[inPort.PortName] = (inPort.Type, inPort.GetContent());
                }
                else if(!inPort.IsExecutePin)
                {
                    if (inPort.Type.IsClass)
                    {
                        dto.InputPinValues[inPort.PortName] = (inPort.Type, null);
                    }
                    else
                    {
                        dto.InputPinValues[inPort.PortName] = (inPort.Type, Activator.CreateInstance(inPort.Type));
                    }
                }
            }
            
            foreach (var outPort in node.OutPorts.Values.Where(outPort => !outPort.IsExecutePin))
            {
                dto.OutputPinNames.Add(outPort.PortName);
            }
            
            var outEdge = node.OutputWires.FirstOrDefault(x => x.LeftSidePin.PinName == PinNames.EXECUTE_OUT);
            if (outEdge.RightSidePin.IsValid())
            {
                dto.Properties.TryAdd(BlueprintBaseNode.NEXT_NODE_GUID, outEdge.RightSidePin.NodeGuid);
            }

            return dto;
        }

        public BlueprintBaseNode Decompile(BlueprintCompiledNodeDto dto)
        {
            dto.Properties.TryGetValue(BlueprintDesignNode.K_METHOD_DECLARING_TYPE, out var methodAssemblyType);
            dto.Properties.TryGetValue(BlueprintDesignNode.K_METHOD_NAME, out var methodName);
            dto.Properties.TryGetValue(BlueprintDesignNode.K_METHOD_PARAMETER_TYPES, out var methodParameterTypes);
            var methodInfo = GetMethodInfo((Type)methodAssemblyType, (string)methodName, (string[])methodParameterTypes);
            return new BlueprintMethodNode(dto, methodInfo);
        }

        private static MethodInfo GetMethodInfo(Type declaringType, string methodName, string[] parameterTypes)
        {
            if (declaringType == null)
            {
                return null;
            }

            if (parameterTypes.Length > 0)
            {
                var paramTypes = new Type[parameterTypes.Length];
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    paramTypes[i] = Type.GetType(parameterTypes[i]);
                    Assert.IsNotNull(parameterTypes[i], $"Invalid parameter type: {parameterTypes[i]}");
                }

                return declaringType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, paramTypes, null);
            }
            else
            {
                return declaringType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            }
        }
    }
}