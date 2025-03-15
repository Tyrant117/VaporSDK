using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.Blueprints
{
    public struct TemporaryDataGetterNodeType : INodeType
    {
        public BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters)
        {
            var graph = this.FindParam<BlueprintMethodGraph>(parameters, INodeType.GRAPH_PARAM);
            var name = this.FindParam<string>(parameters, INodeType.NAME_DATA_PARAM);
            var node = new BlueprintDesignNode(this)
            {
                Graph = graph,
                Position = new Rect(position, Vector2.zero)
            };
            node.AddOrUpdateProperty(BlueprintDesignNode.VARIABLE_NAME, name, true);
            UpdateDesignNode(node);
            return node;
        }
        public void UpdateDesignNode(BlueprintDesignNode node)
        {
            node.TryGetProperty<string>(BlueprintDesignNode.VARIABLE_NAME, out var tempFieldName);
            var tempData = node.Graph.TemporaryVariables.FirstOrDefault(x => x.Name == tempFieldName);
            if (tempData == null)
            {
                Debug.LogError($"{tempFieldName} not found in graph, was the variable deleted?");
                node.SetError($"{tempFieldName} not found in graph, was the variable deleted?");
                node.NodeName = $"Get <b><i>{tempFieldName}</i></b>";
                return;
            }
            
            node.NodeName = $"Get <b><i>{tempData.Name}</i></b>";
            
            var slot = new BlueprintPin(tempData.Name, PinDirection.Out, tempData.Type, false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            node.OutPorts.Add(tempData.Name, slot);
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
            
            node.TryGetProperty<string>(BlueprintDesignNode.VARIABLE_NAME, out var tempFieldName);
            dto.Properties.TryAdd(BlueprintDesignNode.VARIABLE_NAME, tempFieldName);
            
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
            dto.Properties.TryGetValue(BlueprintDesignNode.VARIABLE_NAME, out var tempFieldName);
            return new BlueprintGetterNode(dto, (string)tempFieldName);
        }
    }
}