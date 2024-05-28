using System;

namespace Vapor.GraphTools.Math
{
    [SearchableNode("Math/Round/Truncate", "Truncate", "math")]
    public class TruncateNodeSo : MathNodeSo
    {
        [PortIn("A", 0, true, typeof(float))]
        public NodeSo A;

        [PortOut("Out", 0, true, typeof(float))]
        public NodeSo Out;

        public int InConnectedPort_A;
        public int OutConnectedPort_Out;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<float> _a;

        public override float Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<float>)A;
                _hasInit = true;
            }

            return (float)System.Math.Truncate(_a.Evaluate(externalValues, InConnectedPort_A));
        }
    }
}
