using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class BlueprintRedirectNode : BlueprintBaseNode
    {
        private readonly string _leftNodeGuid;
        private BlueprintBaseNode _leftNode;
        
        private readonly string _rightNodeGuid;
        private BlueprintBaseNode _rightNode;

        private readonly bool _isExecuteRedirect;
        private readonly string _leftPortName;

        public BlueprintRedirectNode(BlueprintDesignNodeDto dto)
        {
            Guid = dto.Guid;
            if (dto.InputWires.Count != 1)
            {
                return;
            }

            if (dto.OutputWires.Count != 1)
            {
                return;
            }

            var inWire = dto.InputWires[0];
            var outWire = dto.OutputWires[0];
            _isExecuteRedirect = inWire.IsExecuteWire && outWire.IsExecuteWire;

            _leftPortName = inWire.LeftSidePin.PinName;
            _leftNodeGuid = inWire.LeftSidePin.NodeGuid;

            _rightNodeGuid = outWire.RightSidePin.NodeGuid;
        }

        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
            if (!_leftNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_leftNodeGuid, out _leftNode);
            }
            if (!_rightNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_rightNodeGuid, out _rightNode);
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
            if (!Graph.IsEvaluating)
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