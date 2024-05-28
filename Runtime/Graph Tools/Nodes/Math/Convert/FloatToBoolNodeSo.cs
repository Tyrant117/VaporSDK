using System;

namespace Vapor.GraphTools
{
    [SearchableNode("Math/Convert/Float To Bool", "Float to Bool", "math")]
    public class FloatToBoolNodeSo : LogicNodeSo
    {
        [PortIn("Float", 0, true, typeof(float))]
        public NodeSo Float;

        [PortOut("Out", 0, true, typeof(float))]
        public NodeSo Out;

        public int InConnectedPort_Float;
        public int OutConnectedPort_Out;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<float> _a;

        public override bool Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<float>)Float;
                _hasInit = true;
            }

            return Convert.ToBoolean(_a.Evaluate(externalValues, InConnectedPort_Float));
        }
    }
}
