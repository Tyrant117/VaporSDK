using System.Collections.Generic;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class PlayModeEntryNode : PlayModeNodeBase
    {
        
        private readonly string _nextNodeGuid;
        private PlayModeNodeBase _nextNode;

        public PlayModeEntryNode(BlueprintDesignNodeDto dto, List<BlueprintWireDto> wires)
        {
            Guid = dto.Guid;
            Uuid = Guid.GetStableHashU32();
            
            SetupWires(wires);
            SetupOutputPins(dto);
            _nextNodeGuid = GetOutputNodeGuidForPinName(OutputWires);
        }

        public override void Init(PlayModeClass playModeClass, PlayModeMethod playModeMethod)
        {
            base.Init(playModeClass, playModeMethod);
            if (!_nextNodeGuid.EmptyOrNull())
            {
                _nextNode = Method.GetNode(_nextNodeGuid);
            }
        }

        protected override void CacheInputValues()
        {
        }

        protected override void WriteOutputValues()
        {
            int index = 0;
            foreach (var param in OutPortValues)
            {
                var argument = Method.GetArgument(index);
                OutPortValues[param.Key] = argument;
                index++;
            }
        }

        protected override void Continue()
        {
            if (!Method.IsEvaluating)
            {
                return;
            }
            
            _nextNode?.InvokeAndContinue();
        }
    }
}