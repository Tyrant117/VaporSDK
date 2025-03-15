using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.Blueprints
{
    public struct TemporaryDataSetterNodeType : INodeType
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
                node.NodeName = $"Set <b><i>{tempFieldName}</i></b>";
                return;
            }
            
            node.NodeName = $"Set <b><i>{tempData.Name}</i></b>";
            
            var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            node.InPorts.Add(PinNames.EXECUTE_IN, inSlot);

            var inData = new BlueprintPin(tempData.Name, PinDirection.In, tempData.Type, false)
                .WithDisplayName(string.Empty);
            node.InPorts.Add(tempData.Name, inData);

            var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty);
            node.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);

            var os = new BlueprintPin(tempData.Name, PinDirection.Out, tempData.Type, false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            node.OutPorts.Add(tempData.Name, os);
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
            
            var outEdge = node.OutputWires.FirstOrDefault(x => x.LeftSidePin.PinName == PinNames.EXECUTE_OUT);
            if (outEdge.RightSidePin.IsValid())
            {
                dto.Properties.TryAdd(BlueprintBaseNode.NEXT_NODE_GUID, outEdge.RightSidePin.NodeGuid);
            }

            return dto;
        }

        public BlueprintBaseNode Decompile(BlueprintCompiledNodeDto dto)
        {
            dto.Properties.TryGetValue(BlueprintDesignNode.VARIABLE_NAME, out var tempFieldName);
            return new BlueprintSetterNode(dto, (string)tempFieldName);
        }
    }
    
    public struct MakeSerializableNodeType : INodeType
    {
        public BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters)
        {
            var type = this.FindParam<Type>(parameters, INodeType.CONNECTION_TYPE_PARAM);
            var node = new BlueprintDesignNode(this)
            {
                Position = new Rect(position, Vector2.zero)
            };
            node.AddOrUpdateProperty(BlueprintDesignNode.CONNECTION_TYPE, type, true);
            UpdateDesignNode(node);
            return node;
        }

        public void UpdateDesignNode(BlueprintDesignNode node)
        {
            node.TryGetProperty<Type>(BlueprintDesignNode.CONNECTION_TYPE, out var makeType);
            
            node.NodeName = $"Make <b><i>{makeType.Name}</i></b>";
            
            var inData = new BlueprintPin(PinNames.IGNORE, PinDirection.In, makeType, false)
                .WithDisplayName(string.Empty);
            node.InPorts.Add(PinNames.IGNORE, inData);

            var os = new BlueprintPin(PinNames.RETURN, PinDirection.Out, makeType, false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            node.OutPorts.Add(PinNames.RETURN, os);
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
            return new BlueprintMakeSerializableNode(dto);
        }
    }
}