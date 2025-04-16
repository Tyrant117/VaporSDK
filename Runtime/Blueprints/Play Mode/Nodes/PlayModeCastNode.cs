using System;
using System.Collections.Generic;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class PlayModeCastNode : PlayModeNodeBase
    {
        private readonly Type _convertTo;
        
        private readonly string _validNodeGuid;
        private PlayModeNodeBase _validNode;
        
        private readonly string _invalidNodeGuid;
        private PlayModeNodeBase _invalidNode;

        public PlayModeCastNode(BlueprintDesignNodeDto dto, List<BlueprintWireDto> wires)
        {
            Guid = dto.Guid;
            Uuid = Guid.GetStableHashU32();
            _convertTo = dto.GetProperty<Type>(NodePropertyNames.DATA_VALUE);
            
            SetupWires(wires);
            SetupInputPins(dto);
            SetupOutputPins(dto);
            
            _validNodeGuid = GetOutputNodeGuidForPinName(OutputWires, PinNames.VALID_OUT);
            _invalidNodeGuid = GetOutputNodeGuidForPinName(OutputWires, PinNames.INVALID_OUT);
        }
        
        public override void Init(PlayModeClass playModeClass, PlayModeMethod playModeMethod)
        {
            base.Init(playModeClass, playModeMethod);
            if (!_validNodeGuid.EmptyOrNull())
            {
                _validNode = Method.GetNode(_validNodeGuid);
            }
            if (!_invalidNodeGuid.EmptyOrNull())
            {
                _invalidNode = Method.GetNode(_invalidNodeGuid);
            }
        }
        
        protected override void CacheInputValues()
        {
            GetAllInputPinValues();
        }

        protected override void WriteOutputValues()
        {
            OutPortValues[PinNames.AS_OUT] = TypeUtility.SafeCastToType(InPortValues[PinNames.VALUE_IN], _convertTo);
        }

        protected override void Continue()
        {
            if (!Method.IsEvaluating)
            {
                return;
            }
            
            if (OutPortValues[PinNames.AS_OUT] != null)
            {
                _validNode?.InvokeAndContinue();
            }
            else
            {
                _invalidNode?.InvokeAndContinue();
            }
        }
    }
}