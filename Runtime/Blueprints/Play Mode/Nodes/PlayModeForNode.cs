using System.Collections;
using System.Collections.Generic;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class PlayModeForNode : PlayModeIteratorNode
    {
        private readonly string _loopNodeGuid;
        private PlayModeNodeBase _loopNode;

        private readonly string _completedNodeGuid;
        private PlayModeNodeBase _completedNode;
        
        public PlayModeForNode(BlueprintDesignNodeDto dto, List<BlueprintWireDto> wires)
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
            if (!InPortValues.TryGetValue(PinNames.ARRAY_IN, out var array))
            {
                return;
            }
            if (!InPortValues.TryGetValue(PinNames.START_INDEX_IN, out var f))
            {
                return;
            }
            if (!InPortValues.TryGetValue(PinNames.LAST_INDEX_IN, out var l))
            {
                return;
            }

            var arr = (ICollection)array;
            var copiedArr = new object[arr.Count];
            arr.CopyTo(copiedArr, 0);
            int first = (int)f;
            int last = (int)l;
            Looping = true;
            if(first < last)
            {
                for (int i = first; i <= last; i++)
                {
                    var a = copiedArr[i];
                    OutPortValues[PinNames.ELEMENT_OUT] = a;
                    OutPortValues[PinNames.INDEX_OUT] = i;
                    _loopNode?.InvokeAndContinue();
                    if (!Looping || !Method.IsEvaluating)
                    {
                        break;
                    }
                }
            }
            else
            {
                for (int i = first; i >= last; i--)
                {
                    var a = copiedArr[i];
                    OutPortValues[PinNames.ELEMENT_OUT] = a;
                    OutPortValues[PinNames.INDEX_OUT] = i;
                    _loopNode?.InvokeAndContinue();
                    if (!Looping || !Method.IsEvaluating)
                    {
                        break;
                    }
                }
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