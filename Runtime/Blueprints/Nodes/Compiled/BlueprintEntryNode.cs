using UnityEngine;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class BlueprintEntryNode : BlueprintBaseNode
    {
        
        private readonly string _nextNodeGuid;
        private BlueprintBaseNode _nextNode;

        public BlueprintEntryNode(BlueprintDesignNodeDto dto)
        {
            Guid = dto.Guid;
            InputWires = dto.InputWires;
            OutputWires = dto.OutputWires;
            
            SetupOutputPins(dto);
            _nextNodeGuid = GetNodeGuidForPinName(dto);
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