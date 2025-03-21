﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class BlueprintForEachNode : BlueprintBaseNode
    {
        public const string LOOP_NODE_GUID = "LoopNodeGuid";
        public const string COMPLETE_NODE_GUID = "CompleteNodeGuid";
        
        private bool _looping;
        
        private readonly string _loopNodeGuid;
        private BlueprintBaseNode _loopNode;

        private readonly string _completedNodeGuid;
        private BlueprintBaseNode _completedNode;
        
        public BlueprintForEachNode(BlueprintNodeDataModel dataModel)
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
            
            OutPortValues = new Dictionary<string, object>(dataModel.OutPorts.Count);
            foreach (var outPort in dataModel.OutPorts.Values)
            {
                if (!outPort.IsExecutePin)
                {
                    OutPortValues[outPort.PortName] = null;
                }
            }
            
            
            var trueEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePin.PinName == "Loop");
            if (trueEdge.RightSidePin.IsValid())
            {
                _loopNodeGuid = trueEdge.RightSidePin.NodeGuid;
            }
            
            var falseEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePin.PinName == "Complete");
            if (falseEdge.RightSidePin.IsValid())
            {
                _completedNodeGuid = falseEdge.RightSidePin.NodeGuid;
            }
        }

        public BlueprintForEachNode(BlueprintCompiledNodeDto dto)
        {
            Guid = dto.Guid;
            InEdges = dto.InputWires;
            
            InPortValues = new Dictionary<string, object>(dto.InputPinValues.Count);
            foreach (var (key, tuple) in dto.InputPinValues)
            {
                var val = TypeUtility.CastToType(tuple.Item2, tuple.Item1);
                InPortValues[key] = val;
            }
            
            OutPortValues = new Dictionary<string, object>(dto.OutputPinNames.Count);
            foreach (var outPort in dto.OutputPinNames)
            {
                OutPortValues[outPort] = null;
            }

            if (dto.Properties.TryGetValue(LOOP_NODE_GUID, out var tNodeGuid))
            {
                _loopNodeGuid = tNodeGuid as string;
            }
            
            if (dto.Properties.TryGetValue(COMPLETE_NODE_GUID, out var fNodeGuid))
            {
                _completedNodeGuid = fNodeGuid as string;
            }
        }

        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
            if (!_loopNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_loopNodeGuid, out _loopNode);
            }
            if (!_completedNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_completedNodeGuid, out _completedNode);
            }
        }

        public override void InvokeAndContinue()
        {
            if (_looping)
            {
                _looping = false;
            }
            else
            {
                base.InvokeAndContinue();
            }
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
            if (InPortValues.TryGetValue("Array", out var array))
            {
                var arr = (IEnumerable)array;
                int idx = 0;
                _looping = true;
                foreach (var a in arr)
                {
                    OutPortValues["Element"] = a;
                    int i = idx;
                    OutPortValues["Index"] = i;
                    _loopNode?.InvokeAndContinue();
                    if (!_looping || !Graph.IsEvaluating)
                    {
                        break;
                    }
                    idx++;
                }
            }
        }

        protected override void Continue()
        {
            if (!Graph.IsEvaluating)
            {
                return;
            }
            
            _completedNode?.InvokeAndContinue();
        }
    }
}