using System;

namespace Vapor.GraphTools.Math
{
    [SearchableNode("Math/Basic/Subtract", "Subtract", "math")]
    public class SubtractNodeSo : MathNodeSo
    {
        [PortIn("A", 0, true, typeof(float))]
        public NodeSo A;
        [PortIn("B", 1, true, typeof(float))]
        public NodeSo B;

        [PortOut("Out", 0, true, typeof(float))]
        public NodeSo Out;

        public int InConnectedPort_A;
        public int InConnectedPort_B;
        public int OutConnectedPort_Out;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<float> _a;
        [NonSerialized]
        private IEvaluatorNode<float> _b;

        public override float Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<float>)A;
                _b = (IEvaluatorNode<float>)B;
                _hasInit = true;
            }

            return _a.Evaluate(externalValues, InConnectedPort_A) - _b.Evaluate(externalValues, InConnectedPort_B);
        }
    }
}
