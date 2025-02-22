using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vapor.Blueprints
{
    public struct BranchNodeType : INodeType
    {
        public BlueprintNodeDataModel CreateDataModel(Vector2 position, List<(string, object)> parameters)
        {
            var node = BlueprintNodeDataModelUtility.CreateOrUpdateIfElseNode(null);
            node.Position = new Rect(position, Vector2.zero);
            return node;
        }

        public BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters)
        {
            var node = new BlueprintDesignNode(this)
            {
                NodeName = "Branch",
                Position = new Rect(position, Vector2.zero)
            };
            UpdateDesignNode(node);
            return node;
        }

        public void UpdateDesignNode(BlueprintDesignNode node)
        {
            var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName("")
                .WithAllowMultipleWires();
            node.InPorts.Add(PinNames.EXECUTE_IN, inSlot);

            var slot = new BlueprintPin(PinNames.VALUE_IN, PinDirection.In, typeof(bool), false);
            node.InPorts.Add(PinNames.VALUE_IN, slot);

            var trueSlot = new BlueprintPin(PinNames.TRUE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("True");
            node.OutPorts.Add(PinNames.TRUE_OUT, trueSlot);

            var falseSlot = new BlueprintPin(PinNames.FALSE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("False");
            node.OutPorts.Add(PinNames.FALSE_OUT, falseSlot);
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
            
            foreach (var inPort in node.InPorts.Values.Where(inPort => inPort.HasInlineValue))
            {
                dto.InputPinValues[inPort.PortName] = (inPort.Type, inPort.GetContent());
            }
            
            var trueEdge = node.OutputWires.FirstOrDefault(x => x.LeftSidePin.PinName == PinNames.TRUE_OUT);
            if (trueEdge.RightSidePin.IsValid())
            {
                dto.Properties.TryAdd(BlueprintIfElseNode.TRUE_NODE_GUID, trueEdge.RightSidePin.NodeGuid);
            }
            
            var falseEdge = node.OutputWires.FirstOrDefault(x => x.LeftSidePin.PinName == PinNames.FALSE_OUT);
            if (falseEdge.RightSidePin.IsValid())
            {
                dto.Properties.TryAdd(BlueprintIfElseNode.FALSE_NODE_GUID, falseEdge.RightSidePin.NodeGuid);
            }
            return dto;
        }

        public BlueprintBaseNode Decompile(BlueprintCompiledNodeDto dto)
        {
            return new BlueprintIfElseNode(dto);
        }
    }
}