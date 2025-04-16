using System.Collections.Generic;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class PlayModeRedirectNode : PlayModeNodeBase
    {
        private readonly string _leftNodeGuid;
        private PlayModeNodeBase _leftNode;
        
        private readonly string _rightNodeGuid;
        private PlayModeNodeBase _rightNode;

        private readonly bool _isExecuteRedirect;
        private readonly string _leftPortName;

        public PlayModeRedirectNode(BlueprintDesignNodeDto dto, List<BlueprintWireDto> wires)
        {
            Guid = dto.Guid;
            Uuid = Guid.GetStableHashU32();

            SetupWires(wires);
            if (!InputWires.IsValidIndex(0))
            {
                return;
            }

            if (!OutputWires.IsValidIndex(0))
            {
                return;
            }

            var inWire = InputWires[0];
            var outWire = OutputWires[0];
            _isExecuteRedirect = inWire.IsExecuteWire && outWire.IsExecuteWire;

            _leftPortName = inWire.LeftName;
            _leftNodeGuid = inWire.LeftGuid;

            _rightNodeGuid = outWire.RightGuid;
        }

        public override void Init(PlayModeClass playModeClass, PlayModeMethod playModeMethod)
        {
            base.Init(playModeClass, playModeMethod);
            if (!_leftNodeGuid.EmptyOrNull())
            {
                _leftNode = Method.GetNode(_leftNodeGuid);
            }
            if (!_rightNodeGuid.EmptyOrNull())
            {
                _rightNode = Method.GetNode(_rightNodeGuid);
            }
        }

        protected override void CacheInputValues()
        {
            if (!_isExecuteRedirect)
            {
                _leftNode.Invoke();
            }
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

            if (!_isExecuteRedirect)
            {
                return;
            }
            
            _rightNode?.InvokeAndContinue();
        }

        public override bool TryGetOutputValue(string outPortName, out object outputValue)
        {
            if (_leftNode != null)
            {
                return _leftNode.TryGetOutputValue(_leftPortName, out outputValue);
            }

            outputValue = null;
            return false;
        }
    }
}