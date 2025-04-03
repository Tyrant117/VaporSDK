using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class BlueprintIfElseNode : BlueprintBaseNode
    {
        private readonly string _trueNodeGuid;
        private BlueprintBaseNode _trueNode;
        
        private readonly string _falseNodeGuid;
        private BlueprintBaseNode _falseNode;
        
        private bool _true;

        public BlueprintIfElseNode(BlueprintDesignNodeDto dto)
        {
            Guid = dto.Guid;
            InputWires = dto.InputWires;
            OutputWires = dto.OutputWires;

            SetupInputPins(dto);

            _trueNodeGuid = GetNodeGuidForPinName(dto, PinNames.TRUE_OUT);
            _falseNodeGuid = GetNodeGuidForPinName(dto, PinNames.FALSE_OUT);
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
            GetAllInputPinValues();
        }

        protected override void WriteOutputValues()
        {
            _true = (bool)InPortValues[PinNames.VALUE_IN];
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