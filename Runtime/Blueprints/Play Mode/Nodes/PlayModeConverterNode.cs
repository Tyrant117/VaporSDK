using System;
using System.Collections.Generic;

namespace Vapor.Blueprints
{
    public class PlayModeConverterNode : PlayModeNodeBase
    {
        private readonly Type _convertTo;

        public PlayModeConverterNode(BlueprintDesignNodeDto dto, List<BlueprintWireDto> wires)
        {
            Guid = dto.Guid;
            Uuid = Guid.GetStableHashU32();
            var fromToTuple = dto.GetProperty<(Type, Type)>(NodePropertyNames.DATA_VALUE);
            _convertTo = fromToTuple.Item2;
            
            SetupWires(wires);
            SetupInputPins(dto);
            SetupOutputPins(dto);
        }

        protected override void CacheInputValues()
        {
            GetAllInputPinValues();
        }

        protected override void WriteOutputValues()
        {
            OutPortValues[PinNames.GET_OUT] = TypeUtility.CastToType(InPortValues[PinNames.SET_IN], _convertTo);
        }

        protected override void Continue()
        {
        }
    }
}