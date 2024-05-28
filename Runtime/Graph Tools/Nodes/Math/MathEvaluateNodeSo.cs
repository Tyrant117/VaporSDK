using System;

namespace Vapor.GraphTools
{
    [NodeName("Evaluate()"), NodeIsToken]
    public class MathEvaluateNodeSo : NodeSo, IEvaluatorNode<float>
    {
        [PortIn("", 0, true, typeof(float))]
        public NodeSo Start;

        public int InConnectedPort_Start;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<float> _eval;

        public float GetValue(int portIndex)
        {
            if (!_hasInit)
            {
                _eval = (IEvaluatorNode<float>)Start;
                _hasInit = true;
            }

            return _eval.GetValue(portIndex);
        }

        public float Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _eval = (IEvaluatorNode<float>)Start;
                _hasInit = true;
            }

            return _eval.Evaluate(externalValues, InConnectedPort_Start);
        }        
    }
}
