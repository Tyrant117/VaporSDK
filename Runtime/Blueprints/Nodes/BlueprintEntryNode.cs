using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class BlueprintEntryNode : BlueprintBaseNode
    {
        
        private readonly string _nextNodeGuid;
        private BlueprintBaseNode _nextNode;
        
        public BlueprintEntryNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            
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
        }

        protected override void WriteOutputValues()
        {
            foreach (var param in Graph.GetParameters())
            {
                if (OutPortValues.ContainsKey(param.Key))
                {
                    OutPortValues[param.Key] = param.Value;
                }
                else
                {
                    Debug.LogError($"Failed to get output value for {param.Key}");
                }
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