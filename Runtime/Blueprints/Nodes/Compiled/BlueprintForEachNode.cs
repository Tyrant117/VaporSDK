using System.Collections;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class BlueprintForEachNode : BlueprintBaseNode
    {
        private bool _looping;
        
        private readonly string _loopNodeGuid;
        private BlueprintBaseNode _loopNode;

        private readonly string _completedNodeGuid;
        private BlueprintBaseNode _completedNode;

        public BlueprintForEachNode(BlueprintDesignNodeDto dto)
        {
            Guid = dto.Guid;
            InputWires = dto.InputWires;
            OutputWires = dto.OutputWires;
            
            SetupInputPins(dto);
            SetupOutputPins(dto);

            _loopNodeGuid = GetNodeGuidForPinName(dto,  PinNames.LOOP_OUT);
            _completedNodeGuid = GetNodeGuidForPinName(dto, PinNames.COMPLETE_OUT);
        }

        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
            if (!_loopNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_loopNodeGuid, out _loopNode);
            }
            if (!_completedNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_completedNodeGuid, out _completedNode);
            }
        }

        public override void InvokeAndContinue()
        {
            if (_looping)
            {
                _looping = false;
            }
            else
            {
                base.InvokeAndContinue();
            }
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

            var arr = (IEnumerable)array;
            int idx = 0;
            _looping = true;
            foreach (var a in arr)
            {
                OutPortValues[PinNames.ELEMENT_OUT] = a;
                int i = idx;
                OutPortValues[PinNames.INDEX_OUT] = i;
                _loopNode?.InvokeAndContinue();
                if (!_looping || !Graph.IsEvaluating)
                {
                    break;
                }
                idx++;
            }
        }

        protected override void Continue()
        {
            if (!Graph.IsEvaluating)
            {
                return;
            }
            
            _completedNode?.InvokeAndContinue();
        }
    }
}