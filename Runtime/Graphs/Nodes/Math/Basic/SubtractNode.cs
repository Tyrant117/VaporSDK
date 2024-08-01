using System;

namespace Vapor.Graphs
{
    [SearchableNode("Math/Basic/Subtract", "Subtract", "math")]
    public class SubtractNode : MathNode
    {
        [PortIn("A", 0, true, typeof(double))]
        public Node A;
        [PortIn("B", 1, true, typeof(double))]
        public Node B;

        [PortOut("Out", 0, true, typeof(double))]
        public Node Out;

        public int InConnectedPort_A;
        public int InConnectedPort_B;
        public int OutConnectedPort_Out;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<double, IExternalValueSource> _a;
        [NonSerialized]
        private IEvaluatorNode<double, IExternalValueSource> _b;

        public override double Evaluate(IExternalValueSource arg)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<double, IExternalValueSource>)A;
                _b = (IEvaluatorNode<double, IExternalValueSource>)B;
                _hasInit = true;
            }

            return _a.Evaluate(arg) - _b.Evaluate(arg);
        }
    }
}
