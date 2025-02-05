using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class BlueprintSetterNode : BlueprintBaseNode
    {
        private readonly string _tempFieldName;
        
        private readonly string _nextNodeGuid;
        private BlueprintBaseNode _nextNode;
        
        public BlueprintSetterNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            _tempFieldName = dataModel.MethodName;
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
            
            var outEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePin.PinName == "OUT");
            if (outEdge.RightSidePin.IsValid())
            {
                _nextNodeGuid = outEdge.RightSidePin.NodeGuid;
            }
        }

        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
            if (!_nextNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_nextNodeGuid, out _nextNode);
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

            foreach (var ipv in InPortValues.Values)
            {
                Graph.TrySetTempValue(_tempFieldName, ipv);
            }
        }

        protected override void WriteOutputValues()
        {
            Graph.TryGetTempValue(_tempFieldName, out var temp);
            if (OutPortValues.ContainsKey(_tempFieldName))
            {
                OutPortValues[_tempFieldName] = temp;
            }
            else
            {
                Debug.LogError($"Failed to get output value for {_tempFieldName}");
            }
        }

        protected override void Continue()
        {
            if (!Graph.IsEvaluating)
            {
                return;
            }
            
            _nextNode?.InvokeAndContinue();
        }
    }
}