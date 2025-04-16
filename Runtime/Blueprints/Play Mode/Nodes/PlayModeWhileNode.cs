using System.Collections.Generic;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class PlayModeWhileNode : PlayModeIteratorNode
    {
        private readonly string _loopNodeGuid;
        private PlayModeNodeBase _loopNode;

        private readonly string _completedNodeGuid;
        private PlayModeNodeBase _completedNode;
        
        public PlayModeWhileNode(BlueprintDesignNodeDto dto, List<BlueprintWireDto> wires)
        {
            Guid = dto.Guid;
            Uuid = Guid.GetStableHashU32();
            
            SetupWires(wires);
            SetupInputPins(dto);
            SetupOutputPins(dto);

            _loopNodeGuid = GetOutputNodeGuidForPinName(OutputWires,  PinNames.LOOP_OUT);
            _completedNodeGuid = GetOutputNodeGuidForPinName(OutputWires, PinNames.COMPLETE_OUT);
        }

        public override void Init(PlayModeClass playModeClass, PlayModeMethod playModeMethod)
        {
            base.Init(playModeClass, playModeMethod);
            if (!_loopNodeGuid.EmptyOrNull())
            {
                _loopNode = Method.GetNode(_loopNodeGuid);
            }
            if (!_completedNodeGuid.EmptyOrNull())
            {
                _completedNode = Method.GetNode(_completedNodeGuid);
            }
        }

        public override void InvokeAndContinue()
        {
            Method.IteratorNodeStack.Push(this);
            base.InvokeAndContinue();
        }

        protected override void CacheInputValues()
        {
            GetAllInputPinValues();
        }

        protected override void WriteOutputValues()
        {
            if (!InPortValues.TryGetValue(PinNames.VALUE_IN, out var w))
            {
                return;
            }

            bool whileTrue = (bool)w;
            Looping = true;
            while (whileTrue)
            {
                _loopNode?.InvokeAndContinue();
                if (!Looping || !Method.IsEvaluating)
                {
                    break;
                }

                // Update the while bool
                GetAllInputPinValues();
                whileTrue = (bool)InPortValues[PinNames.VALUE_IN];
            }

            Looping = false;
        }

        protected override void Continue()
        {
            Method.IteratorNodeStack.Pop();
            if (!Method.IsEvaluating)
            {
                return;
            }
            
            _completedNode?.InvokeAndContinue();
        }
    }
}