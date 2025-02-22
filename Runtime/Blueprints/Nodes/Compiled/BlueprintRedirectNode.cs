using System.Linq;
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

        public BlueprintRedirectNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            var inEdge = dataModel.InEdges.FirstOrDefault(x => x.RightSidePin.PinName == PinNames.EXECUTE_IN);
            _isExecuteRedirect = inEdge.RightSidePin.IsExecutePin;
            if (inEdge.LeftSidePin.IsValid())
            {
                _leftPortName = inEdge.LeftSidePin.PinName;
                _leftNodeGuid = inEdge.LeftSidePin.NodeGuid;
            }

            var outEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePin.PinName == PinNames.EXECUTE_OUT);
            if (outEdge.RightSidePin.IsValid())
            {
                _rightNodeGuid = outEdge.RightSidePin.NodeGuid;
            }
        }

        public BlueprintRedirectNode(BlueprintCompiledNodeDto dto)
        {
            Guid = dto.Guid;
            var inEdge = dto.InputWires.FirstOrDefault(x => x.RightSidePin.PinName == PinNames.EXECUTE_IN);
            _isExecuteRedirect = inEdge.RightSidePin.IsExecutePin;
            if (inEdge.LeftSidePin.IsValid())
            {
                _leftPortName = inEdge.LeftSidePin.PinName;
                _leftNodeGuid = inEdge.LeftSidePin.NodeGuid;
            }

            if (dto.Properties.TryGetValue(NEXT_NODE_GUID, out var nextNodeGuid))
            {
                _rightNodeGuid = nextNodeGuid as string;
            }
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