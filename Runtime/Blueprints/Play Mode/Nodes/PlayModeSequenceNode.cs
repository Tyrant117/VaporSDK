using System.Collections.Generic;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class PlayModeSequenceNode : PlayModeNodeBase
    {
        private readonly string[] _outputNodeGuids;
        private List<PlayModeNodeBase> _outputNodes; 
        
        public PlayModeSequenceNode(BlueprintDesignNodeDto dto, List<BlueprintWireDto> wires)
        {
            Guid = dto.Guid;
            Uuid = Guid.GetStableHashU32();

            SetupWires(wires);
            _outputNodeGuids = new string[OutputWires.Count];
            for (var i = 0; i < OutputWires.Count; i++)
            {
                var wire = OutputWires[i];
                if (wire.Guid.EmptyOrNull())
                {
                    continue;
                }

                _outputNodeGuids[i] = wire.RightGuid;
            }
        }

        public override void Init(PlayModeClass playModeClass, PlayModeMethod playModeMethod)
        {
            base.Init(playModeClass, playModeMethod);
            foreach (var rightGuid in _outputNodeGuids)
            {
                if (rightGuid.EmptyOrNull())
                {
                    continue;
                }

                _outputNodes.Add(Method.GetNode(rightGuid));
            }
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

            for (int i = 0; i < _outputNodeGuids.Length; i++)
            {
                if (!Method.IsEvaluating)
                {
                    return;
                }
                
                _outputNodes[i]?.InvokeAndContinue();
            }
        }
    }
}