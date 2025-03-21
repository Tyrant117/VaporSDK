﻿using System;
using System.Collections.Generic;

namespace Vapor.Blueprints
{
    public class BlueprintReturnNode : BlueprintBaseNode
    {
        public BlueprintReturnNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            InEdges = dataModel.InEdges;
            
            InPortValues = new Dictionary<string, object>(dataModel.InPorts.Count);
            foreach (var inPort in dataModel.InPorts.Values)
            {
                if (inPort.HasInlineValue)
                {
                    InPortValues[inPort.PortName] = inPort.GetContent();
                }
            }
        }

        public BlueprintReturnNode(BlueprintCompiledNodeDto dto)
        {
            Guid = dto.Guid;
            InEdges = dto.InputWires;
            
            InPortValues = new Dictionary<string, object>(dto.InputPinValues.Count);
            foreach (var (key, tuple) in dto.InputPinValues)
            {
                var val = TypeUtility.CastToType(tuple.Item2, tuple.Item1);
                InPortValues[key] = val;
            }
        }

        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
        }

        protected override void CacheInputValues()
        {
            foreach (var edge in InEdges)
            {
                if (edge.LeftSidePin.IsExecutePin)
                {
                    continue;
                }
                
                if (!Graph.TryGetNode(edge.LeftSidePin.NodeGuid, out var leftNode))
                {
                    continue;
                }

                leftNode.Invoke();
                if (leftNode.TryGetOutputValue(edge.LeftSidePin.PinName, out var outputValue))
                {
                    InPortValues[edge.RightSidePin.PinName] = outputValue;
                }
            }
        }

        protected override void WriteOutputValues()
        {
            Graph.WriteReturnValues(InPortValues);
        }

        protected override void Continue()
        {
            if (!Graph.IsEvaluating)
            {
                return;
            }
            
            Graph.Return();
        }
    }
}