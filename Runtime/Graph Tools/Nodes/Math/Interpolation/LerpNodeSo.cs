using System;
using UnityEngine;

namespace Vapor.GraphTools.Math
{
    [SearchableNode("Math/Interpolation/Lerp", "Lerp", "math")]
    public class LerpNodeSo : MathNodeSo
    {
        [PortIn("A", 0, true, typeof(float))]
        public NodeSo A;
        [PortIn("B", 1, true, typeof(float))]
        public NodeSo B;
        [PortIn("T", 2, true, typeof(float))]
        public NodeSo C;

        [PortOut("Out", 0, true, typeof(float))]
        public NodeSo Out;

        public int InConnectedPort_A;
        public int InConnectedPort_B;
        public int InConnectedPort_C;
        public int OutConnectedPort_Out;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<float> _a;
        [NonSerialized]
        private IEvaluatorNode<float> _b;
        [NonSerialized]
        private IEvaluatorNode<float> _c;

        public override float Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<float>)A;
                _b = (IEvaluatorNode<float>)B;
                _c = (IEvaluatorNode<float>)C;
                _hasInit = true;
            }

            return Mathf.Lerp(_a.Evaluate(externalValues, InConnectedPort_A), _b.Evaluate(externalValues, InConnectedPort_B), _c.Evaluate(externalValues, InConnectedPort_C));
        }
    }
}
