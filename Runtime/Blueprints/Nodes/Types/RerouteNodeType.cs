using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vapor.Blueprints
{
    public struct RerouteNodeType : INodeType
    {
        public BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters)
        {
            var type = this.FindParam<Type>(parameters, INodeType.CONNECTION_TYPE_PARAM);
            var node = new BlueprintDesignNode(this)
            {
                NodeName = string.Empty,
                Position = new Rect(position, Vector2.zero)
            };
            node.AddOrUpdateProperty(BlueprintDesignNode.CONNECTION_TYPE, type, true);
            UpdateDesignNode(node);
            return node;
        }

        public void UpdateDesignNode(BlueprintDesignNode node)
        {
            node.TryGetProperty<Type>(BlueprintDesignNode.CONNECTION_TYPE, out var rerouteType);
            if (rerouteType != typeof(ExecutePin))
            {
                var slot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, rerouteType, false)
                    .WithDisplayName(string.Empty);
                node.InPorts.Add(PinNames.EXECUTE_IN, slot);

                var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, rerouteType, false)
                    .WithDisplayName(string.Empty)
                    .WithAllowMultipleWires();
                node.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);
            }
            else
            {
                var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                    .WithDisplayName(string.Empty)
                    .WithAllowMultipleWires();
                node.InPorts.Add(PinNames.EXECUTE_IN, inSlot);
                var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                    .WithDisplayName(string.Empty);
                node.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);
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
            
            var outEdge = node.OutputWires.FirstOrDefault(x => x.LeftSidePin.PinName == PinNames.EXECUTE_OUT);
            if (outEdge.RightSidePin.IsValid())
            {
                dto.Properties.TryAdd(BlueprintBaseNode.NEXT_NODE_GUID, outEdge.RightSidePin.NodeGuid);
            }
            
            return dto;
        }

        public BlueprintBaseNode Decompile(BlueprintCompiledNodeDto dto)
        {
            return new BlueprintRedirectNode(dto);
        }
    }
}