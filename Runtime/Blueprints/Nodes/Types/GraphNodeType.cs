using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.Blueprints
{
    public struct GraphNodeType : INodeType
    {
        public BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters)
        {
            var assetGuid = this.FindParam<string>(parameters, INodeType.NAME_DATA_PARAM);
            var node = new BlueprintDesignNode(this)
            {
                Position = new Rect(position, Vector2.zero)
            };
            node.AddOrUpdateProperty(INodeType.NAME_DATA_PARAM, assetGuid, true);
            UpdateDesignNode(node);
            return node;
        }

        public void UpdateDesignNode(BlueprintDesignNode node)
        {
#if UNITY_EDITOR
            node.TryGetProperty<string>(INodeType.NAME_DATA_PARAM, out var assetGuid);
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
            var found = UnityEditor.AssetDatabase.LoadAssetAtPath<BlueprintGraphSo>(path);
            Assert.IsTrue(found, $"Graph With Guid [{assetGuid}] Not Found");
            node.AddOrUpdateProperty(INodeType.KEY_DATA_PARAM, assetGuid, false);

            node.NodeName = UnityEditor.ObjectNames.NicifyVariableName(found.DisplayName);

            // Execute Pins
            var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            node.InPorts.Add(PinNames.EXECUTE_IN, inSlot);
            var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty);
            node.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);

            // Value Pins

            // Input
            foreach (var inputParameter in found.DesignGraph.Current.InputArguments)
            {
                string portName = inputParameter.Name;
                string displayName = inputParameter.Name;
                
                displayName = UnityEditor.ObjectNames.NicifyVariableName(inputParameter.Name);

                var slot = new BlueprintPin(portName, PinDirection.In, inputParameter.Type, false)
                    .WithDisplayName(displayName);
                node.InPorts.Add(portName, slot);
            }

            // Output
            foreach (var outputParameter in found.DesignGraph.Current.OutputArguments)
            {
                string portName = outputParameter.Name;
                string displayName = outputParameter.Name;
                
                displayName = UnityEditor.ObjectNames.NicifyVariableName(outputParameter.Name);
                
                var type = outputParameter.Type;
                if (type.IsByRef)
                {
                    type = type.GetElementType();
                }

                var slot = new BlueprintPin(portName, PinDirection.Out, type, false)
                    .WithDisplayName(displayName)
                    .WithAllowMultipleWires();
                node.OutPorts.Add(portName, slot);
            }
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
            
            node.TryGetProperty<string>(INodeType.NAME_DATA_PARAM, out var assetGuid);
            node.TryGetProperty<int>(INodeType.KEY_DATA_PARAM, out var graphKey);
            
            dto.Properties.TryAdd(INodeType.NAME_DATA_PARAM, assetGuid);
            dto.Properties.TryAdd(INodeType.KEY_DATA_PARAM, graphKey);
            
            foreach (var inPort in node.InPorts.Values.Where(inPort => inPort.HasInlineValue || !inPort.IsExecutePin))
            {
                if (inPort.HasInlineValue)
                {
                    dto.InputPinValues[inPort.PortName] = (inPort.Type, inPort.GetContent());
                }
                else
                {
                    dto.InputPinValues[inPort.PortName] = (null, null);
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
            dto.Properties.TryGetValue(INodeType.NAME_DATA_PARAM, out var assetGuid);
            dto.Properties.TryGetValue(INodeType.KEY_DATA_PARAM, out var graphKey);
            dto.Properties.TryGetValue(BlueprintBaseNode.NEXT_NODE_GUID, out var nextNodeGuid);
            return new BlueprintGraphNode(dto, (int)graphKey, (string)assetGuid, (string)nextNodeGuid);
        }
    }
}