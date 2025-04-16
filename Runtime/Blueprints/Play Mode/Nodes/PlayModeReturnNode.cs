using System;
using System.Collections.Generic;

namespace Vapor.Blueprints
{
    public class PlayModeReturnNode : PlayModeNodeBase
    {
        public PlayModeReturnNode(BlueprintDesignNodeDto dto, List<BlueprintWireDto> wires)
        {
            Guid = dto.Guid;
            Uuid = Guid.GetStableHashU32();

            SetupWires(wires);
            SetupInputPins(dto);
        }

        protected override void CacheInputValues()
        {
            foreach (var edge in InputWires)
            {
                if (edge.IsExecuteWire)
                {
                    continue;
                }

                var leftNode = Method.GetNode(edge.LeftGuid);
                leftNode.Invoke();
                if (leftNode.TryGetOutputValue(edge.LeftName, out var outputValue))
                {
                    InPortValues[edge.RightName] = outputValue;
                }
            }
        }

        protected override void WriteOutputValues()
        {
            int idx = 0;
            foreach (var p in InPortValues)
            {
                if (idx == 0)
                {
                    Method.SetReturnValue(p.Value);
                }
                else
                {
                    var i = idx - 1;
                    Method.SetOutArguments(i, p.Value);
                }

                idx++;
            }
        }

        protected override void Continue()
        {
        }
    }
}