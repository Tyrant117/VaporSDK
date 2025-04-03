using UnityEngine;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    [System.Obsolete]
    public class BlueprintSetterNode : BlueprintBaseNode
    {
        private readonly string _tempFieldName;
        
        private readonly string _nextNodeGuid;
        private BlueprintBaseNode _nextNode;

        public BlueprintSetterNode(BlueprintDesignNodeDto dto)
        {
            Guid = dto.Guid;
            if(dto.Properties.TryGetValue(NodePropertyNames.VARIABLE_NAME, out var tempFieldName))
            {
                _tempFieldName = (string)tempFieldName.Item2;
            }

            InputWires = dto.InputWires;
            OutputWires = dto.OutputWires;
            
            SetupInputPins(dto);
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
            GetAllInputPinValues();

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