using System.Collections.Generic;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class PlayModeIfElseNode : PlayModeNodeBase
    {
        private readonly string _trueNodeGuid;
        private PlayModeNodeBase _trueNode;
        
        private readonly string _falseNodeGuid;
        private PlayModeNodeBase _falseNode;
        
        private bool _true;

        public PlayModeIfElseNode(BlueprintDesignNodeDto dto, List<BlueprintWireDto> wires)
        {
            Guid = dto.Guid;
            Uuid = Guid.GetStableHashU32();
            
            SetupWires(wires);
            SetupInputPins(dto);

            _trueNodeGuid = GetOutputNodeGuidForPinName(OutputWires, PinNames.TRUE_OUT);
            _falseNodeGuid = GetOutputNodeGuidForPinName(OutputWires, PinNames.FALSE_OUT);
        }

        public override void Init(PlayModeClass playModeClass, PlayModeMethod playModeMethod)
        {
            base.Init(playModeClass, playModeMethod);
            if (!_trueNodeGuid.EmptyOrNull())
            {
                _trueNode = Method.GetNode(_trueNodeGuid);
            }
            if (!_falseNodeGuid.EmptyOrNull())
            {
                _falseNode = Method.GetNode(_falseNodeGuid);
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
            if (!Method.IsEvaluating)
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