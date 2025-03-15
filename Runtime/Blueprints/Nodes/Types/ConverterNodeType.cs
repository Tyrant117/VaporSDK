using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.Blueprints
{
    public struct ConverterNodeType : INodeType
    {
        public BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters)
        {
            var methodInfo = this.FindParam<MethodInfo>(parameters, INodeType.METHOD_INFO_PARAM);
            Assert.IsTrue(methodInfo.DeclaringType != null, $"{methodInfo.Name} DeclaringType is null");
            var node = new BlueprintDesignNode(this)
            {
                NodeName = string.Empty,
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
            node.TryGetProperty<Type>(BlueprintDesignNode.K_METHOD_DECLARING_TYPE, out var methodAssemblyType);
            node.TryGetProperty<string>(BlueprintDesignNode.K_METHOD_NAME, out var methodName);
            node.TryGetProperty<string[]>(BlueprintDesignNode.K_METHOD_PARAMETER_TYPES, out var methodParameterTypes);
            
            var methodInfo = GetMethodInfo(methodAssemblyType, methodName, methodParameterTypes);
            node.AddOrUpdateProperty(BlueprintDesignNode.K_METHOD_INFO, methodInfo, false);
            
            var atr = methodInfo.GetCustomAttribute<BlueprintPinConverterAttribute>();

            var slot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, atr.SourceType, false)
                .WithDisplayName(string.Empty);
            node.InPorts.Add(PinNames.EXECUTE_IN, slot);

            var outSlot = new BlueprintPin(PinNames.RETURN, PinDirection.Out, atr.TargetType, false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            node.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);
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
            
            return dto;
        }

        public BlueprintBaseNode Decompile(BlueprintCompiledNodeDto dto)
        {
            dto.Properties.TryGetValue(BlueprintDesignNode.K_METHOD_DECLARING_TYPE, out var methodAssemblyType);
            dto.Properties.TryGetValue(BlueprintDesignNode.K_METHOD_NAME, out var methodName);
            dto.Properties.TryGetValue(BlueprintDesignNode.K_METHOD_PARAMETER_TYPES, out var methodParameterTypes);
            var methodInfo = GetMethodInfo((Type)methodAssemblyType, (string)methodName, (string[])methodParameterTypes);
            return new BlueprintConverterNode(dto, methodInfo);
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