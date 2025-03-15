using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vapor.Blueprints
{
    public struct ForEachNodeType : INodeType
    {
        public BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters)
        {
            var node = new BlueprintDesignNode(this)
            {
                NodeName = "ForEach",
                Position = new Rect(position, Vector2.zero)
            };
            UpdateDesignNode(node);
            return node;
        }
        public void UpdateDesignNode(BlueprintDesignNode node)
        {
            var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            node.InPorts.Add(PinNames.EXECUTE_IN, inSlot);

            var breakSlot = new BlueprintPin(PinNames.BREAK_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName("Break")
                .WithAllowMultipleWires();
            node.InPorts.Add(PinNames.BREAK_IN, breakSlot);

            var arraySlot = new BlueprintPin(PinNames.ARRAY_IN, PinDirection.In, typeof(object), false)
                .WithDisplayName("Array");
            node.InPorts.Add(PinNames.ARRAY_IN, arraySlot);

            var loopSlot = new BlueprintPin(PinNames.LOOP_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("Loop");
            node.OutPorts.Add(PinNames.LOOP_OUT, loopSlot);

            var indexSlot = new BlueprintPin(PinNames.INDEX_OUT, PinDirection.Out, typeof(int), false)
                .WithDisplayName("Index");
            node.OutPorts.Add(PinNames.INDEX_OUT, indexSlot);

            var elementSlot = new BlueprintPin(PinNames.ELEMENT_OUT, PinDirection.Out, typeof(object), false)
                .WithDisplayName("Element");
            node.OutPorts.Add(PinNames.ELEMENT_OUT, elementSlot);

            var completedSlot = new BlueprintPin(PinNames.COMPLETE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("Complete");
            node.OutPorts.Add(PinNames.COMPLETE_OUT, completedSlot);
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
            
            var loopEdge = node.OutputWires.FirstOrDefault(x => x.LeftSidePin.PinName == PinNames.LOOP_OUT);
            if (loopEdge.RightSidePin.IsValid())
            {
                dto.Properties.TryAdd(BlueprintForEachNode.LOOP_NODE_GUID, loopEdge.RightSidePin.NodeGuid);
            }
            
            var completeEdge = node.OutputWires.FirstOrDefault(x => x.LeftSidePin.PinName == PinNames.COMPLETE_OUT);
            if (completeEdge.RightSidePin.IsValid())
            {
                dto.Properties.TryAdd(BlueprintForEachNode.COMPLETE_NODE_GUID, completeEdge.RightSidePin.NodeGuid);
            }
            
            return dto;
        }

        public BlueprintBaseNode Decompile(BlueprintCompiledNodeDto dto)
        {
            return new BlueprintForEachNode(dto);
        }
    }
}