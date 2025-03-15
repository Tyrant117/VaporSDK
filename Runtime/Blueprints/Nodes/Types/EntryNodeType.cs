using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vapor.Blueprints
{
    public struct EntryNodeType : INodeType
    {
        public BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters)
        {
            var graph = this.FindParam<BlueprintMethodGraph>(parameters, INodeType.GRAPH_PARAM);
            var node = new BlueprintDesignNode(this)
            {
                Graph = graph,
                NodeName = "Entry",
                Position = new Rect(position, Vector2.zero)
            };
            UpdateDesignNode(node);
            return node;
        }

        public void UpdateDesignNode(BlueprintDesignNode node)
        {
            node.NodeName = "Entry";
            var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("");
            node.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);

            foreach (var parameter in node.Graph.InputArguments)
            {
                var slot = new BlueprintPin(parameter.Name, PinDirection.Out, parameter.Type, false)
                    .WithAllowMultipleWires();
                node.OutPorts.Add(parameter.Name, slot);
            }
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
            return new BlueprintEntryNode(dto);
        }
    }
}