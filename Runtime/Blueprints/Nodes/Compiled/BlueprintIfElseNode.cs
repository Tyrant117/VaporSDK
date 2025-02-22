using System;
using System.Collections.Generic;
using System.Linq;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class BlueprintIfElseNode : BlueprintBaseNode
    {
        public const string TRUE_NODE_GUID = "TrueNodeGuid";
        public const string FALSE_NODE_GUID = "FalseNodeGuid";
        
        private readonly string _trueNodeGuid;
        private BlueprintBaseNode _trueNode;
        private readonly string _falseNodeGuid;
        private BlueprintBaseNode _falseNode;
        private bool _true;
        
        public BlueprintIfElseNode(BlueprintNodeDataModel dataModel)
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
            
            var trueEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePin.PinName == "True");
            if (trueEdge.RightSidePin.IsValid())
            {
                _trueNodeGuid = trueEdge.RightSidePin.NodeGuid;
            }
            
            var falseEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePin.PinName == "False");
            if (falseEdge.RightSidePin.IsValid())
            {
                _falseNodeGuid = falseEdge.RightSidePin.NodeGuid;
            }
        }

        public BlueprintIfElseNode(BlueprintCompiledNodeDto dto)
        {
            Guid = dto.Guid;
            InEdges = dto.InputWires;
            
            InPortValues = new Dictionary<string, object>(dto.InputPinValues.Count);
            foreach (var (key, tuple) in dto.InputPinValues)
            {
                var val = Convert.ChangeType(tuple.Item2, tuple.Item1);
                InPortValues[key] = val;
            }
            
            if (dto.Properties.TryGetValue(TRUE_NODE_GUID, out var tNodeGuid))
            {
                _trueNodeGuid = tNodeGuid as string;
            }
            
            if (dto.Properties.TryGetValue(FALSE_NODE_GUID, out var fNodeGuid))
            {
                _falseNodeGuid = fNodeGuid as string;
            }
        }

        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
            if (!_trueNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_trueNodeGuid, out _trueNode);
            }
            if (!_falseNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_falseNodeGuid, out _falseNode);
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
            _true = (bool)InPortValues["Value"];
        }

        protected override void Continue()
        {
            if (!Graph.IsEvaluating)
            {
                return;
            }
            
            if (_true)
            {
                _trueNode?.InvokeAndContinue();
            }
            else
            {
                _falseNode?.InvokeAndContinue();
            }
        }
    }
}