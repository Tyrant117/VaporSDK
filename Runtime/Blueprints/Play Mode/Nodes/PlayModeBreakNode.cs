using System.Collections.Generic;

namespace Vapor.Blueprints
{
    public class PlayModeBreakNode : PlayModeNodeBase
    {
        public PlayModeBreakNode(BlueprintDesignNodeDto dto, List<BlueprintWireDto> wires)
        {
            Guid = dto.Guid;
            Uuid = Guid.GetStableHashU32();
            
            SetupWires(wires);
        }
        
        protected override void CacheInputValues()
        {
            
        }

        protected override void WriteOutputValues()
        {
            
        }

        protected override void Continue()
        {
            if (!Method.IsEvaluating)
            {
                return;
            }
            
            Method.Break();
        }
    }
}