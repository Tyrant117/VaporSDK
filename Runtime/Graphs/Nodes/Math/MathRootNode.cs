using System;

namespace Vapor.Graphs
{
    [NodeName("Evaluate()"), NodeIsToken]
    public class MathRootNode : MathNode
    {
        [PortIn("", 0, true, typeof(float))]
        public Node Start;

        public int InConnectedPort_Start;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<double, IExternalValueSource> _eval;

        public override double Evaluate(IExternalValueSource arg)
        {
            if (!_hasInit)
            {
                _eval = (IEvaluatorNode<double, IExternalValueSource>)Start;
                _hasInit = true;
            }

            return _eval.Evaluate(arg);
        }
    }
}
