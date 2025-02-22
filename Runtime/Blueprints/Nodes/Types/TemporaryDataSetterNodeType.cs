using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.Blueprints
{
    public struct TemporaryDataSetterNodeType : INodeType
    {
        public BlueprintNodeDataModel CreateDataModel(Vector2 position, List<(string, object)> parameters)
        {
            var graph = this.FindParam<BlueprintGraphSo>(parameters, INodeType.GRAPH_PARAM);
            var name = this.FindParam<string>(parameters, INodeType.NAME_DATA_PARAM);
            var tempData = graph.TempData.FirstOrDefault(x => x.FieldName == name);
            var node = BlueprintNodeDataModelUtility.CreateOrUpdateSetterNode(null, tempData);
            node.Position = new Rect(position, Vector2.zero);
            return node;
        }

        public BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters)
        {
            var graph = this.FindParam<BlueprintGraphSo>(parameters, INodeType.GRAPH_PARAM);
            var name = this.FindParam<string>(parameters, INodeType.NAME_DATA_PARAM);
            var node = new BlueprintDesignNode(this)
            {
                Graph = graph,
                Position = new Rect(position, Vector2.zero)
            };
            node.TryAddProperty(BlueprintDesignNode.TEMP_FIELD_NAME, name, true);
            UpdateDesignNode(node);
            return node;
        }

        public void UpdateDesignNode(BlueprintDesignNode node)
        {
            node.TryGetProperty<string>(BlueprintDesignNode.TEMP_FIELD_NAME, out var tempFieldName);
            var tempData = node.Graph.TempData.FirstOrDefault(x => x.FieldName == tempFieldName);
            Assert.IsNotNull(tempData, $"{tempFieldName} not found in graph");
            
            var tuple = tempData.ToParameter();
            node.NodeName = $"Set <b><i>{tuple.Item1}</i></b>";
            
            var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            node.InPorts.Add(PinNames.EXECUTE_IN, inSlot);

            var inData = new BlueprintPin(tuple.Item1, PinDirection.In, tuple.Item2, false)
                .WithDisplayName(string.Empty);
            node.InPorts.Add(tuple.Item1, inData);

            var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty);
            node.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);

            var os = new BlueprintPin(tuple.Item1, PinDirection.Out, tuple.Item2, false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            node.OutPorts.Add(tuple.Item1, os);
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
            
            node.TryGetProperty<string>(BlueprintDesignNode.TEMP_FIELD_NAME, out var tempFieldName);
            dto.Properties.TryAdd(BlueprintDesignNode.TEMP_FIELD_NAME, tempFieldName);
            
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
            dto.Properties.TryGetValue(BlueprintDesignNode.TEMP_FIELD_NAME, out var tempFieldName);
            return new BlueprintSetterNode(dto, (string)tempFieldName);
        }
    }
}