using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Vapor.Blueprints
{
    public struct FieldGetterNodeType : INodeType
    {
        public BlueprintNodeDataModel CreateDataModel(Vector2 position, List<(string, object)> parameters)
        {
            var fieldInfo = this.FindParam<FieldInfo>(parameters, INodeType.FIELD_INFO_PARAM);
            var node = BlueprintNodeDataModelUtility.CreateOrUpdateFieldGetterNode(null,
                fieldInfo.DeclaringType?.AssemblyQualifiedName, 
                fieldInfo.Name);
            node.Position = new Rect(position, Vector2.zero);
            return node;
        }

        
        public BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters)
        {
            var fieldInfo = this.FindParam<FieldInfo>(parameters, INodeType.FIELD_INFO_PARAM);
            var node = new BlueprintDesignNode(this)
            {
                Position = new Rect(position, Vector2.zero)
            };
            node.TryAddProperty(BlueprintDesignNode.FIELD_TYPE, fieldInfo.DeclaringType, true);
            node.TryAddProperty(BlueprintDesignNode.FIELD_NAME, fieldInfo.Name, true);
            UpdateDesignNode(node);
            return node;
        }
        public void UpdateDesignNode(BlueprintDesignNode node)
        {
#if UNITY_EDITOR
            node.TryGetProperty<Type>(BlueprintDesignNode.FIELD_TYPE, out var fieldType);
            node.TryGetProperty<string>(BlueprintDesignNode.FIELD_NAME, out var fieldName);

            var fieldInfo = fieldType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            node.NodeName = UnityEditor.ObjectNames.NicifyVariableName(fieldInfo.Name);

            // In Pin
            var ownerPin = new BlueprintPin(PinNames.OWNER, PinDirection.In, fieldInfo.DeclaringType, true);
            node.InPorts.Add(PinNames.OWNER, ownerPin);

            // Out Pin
            var returnPin = new BlueprintPin(PinNames.RETURN, PinDirection.Out, fieldInfo.FieldType, false)
                .WithAllowMultipleWires();
            node.OutPorts.Add(PinNames.RETURN, returnPin);
#endif
        }

        public BlueprintCompiledNodeDto Compile(BlueprintDesignNode node)
        {
            var dto = new BlueprintCompiledNodeDto
            {
                NodeType = node.NodeType,
                Guid = node.Guid,
                InputWires = node.InputWires,
                InputPinValues = new Dictionary<string, (Type, object)>(node.InPorts.Count),
                OutputPinNames = new List<string>(node.OutPorts.Count),
                Properties = new Dictionary<string, object>(),
            };
            
            node.TryGetProperty<Type>(BlueprintDesignNode.FIELD_TYPE, out var fieldType);
            node.TryGetProperty<string>(BlueprintDesignNode.FIELD_NAME, out var fieldName);
            
            dto.Properties.TryAdd(BlueprintDesignNode.FIELD_TYPE, fieldType);
            dto.Properties.TryAdd(BlueprintDesignNode.FIELD_NAME, fieldName);
            
            foreach (var inPort in node.InPorts.Values.Where(inPort => inPort.HasInlineValue))
            {
                dto.InputPinValues[inPort.PortName] = (inPort.Type, inPort.GetContent());
            }
            
            foreach (var outPort in node.OutPorts.Values.Where(outPort => !outPort.IsExecutePin))
            {
                dto.OutputPinNames.Add(outPort.PortName);
            }

            return dto;
        }

        public BlueprintBaseNode Decompile(BlueprintCompiledNodeDto dto)
        {
            dto.Properties.TryGetValue(BlueprintDesignNode.FIELD_TYPE, out var fieldType);
            dto.Properties.TryGetValue(BlueprintDesignNode.FIELD_NAME, out var fieldName);
            var fieldInfo = ((Type)fieldType).GetField((string)fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return new BlueprintFieldGetterNode(dto, fieldInfo);
        }
    }
}