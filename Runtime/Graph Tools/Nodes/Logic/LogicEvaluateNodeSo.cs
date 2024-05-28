using System;

namespace Vapor.GraphTools
{
    [NodeName("Evaluate()")]
    public class LogicEvaluateNodeSo : NodeSo, IEvaluatorNode<bool>
    {
        [PortIn("", 0, true, typeof(bool))]
        public NodeSo Start;

        public int InConnectedPort_Start;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<bool> _eval;

        public bool GetValue(int portIndex)
        {
            if (!_hasInit)
            {
                _eval = (IEvaluatorNode<bool>)Start;
                _hasInit = true;
            }

            return _eval.GetValue(portIndex);
        }

        public bool Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _eval = (IEvaluatorNode<bool>)Start;
                _hasInit = true;
            }

            return _eval.Evaluate(externalValues, InConnectedPort_Start);
        }
    }
}
