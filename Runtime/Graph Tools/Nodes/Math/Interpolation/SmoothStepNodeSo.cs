using System;
using UnityEngine;

namespace Vapor.GraphTools.Math
{
    [SearchableNode("Math/Interpolation/Smooth Step", "Smooth Step")]
    public class SmoothStepNodeSo : MathNodeSo
    {
        [NodeParam("From", 0, true, typeof(float))]
        public NodeSo A;
        [NodeParam("To", 1, true, typeof(float))]
        public NodeSo B;
        [NodeParam("T", 2, true, typeof(float))]
        public NodeSo C;

        public int ConnectedPort_A;
        public int ConnectedPort_B;
        public int ConnectedPort_C;

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

            return Mathf.SmoothStep(_a.Evaluate(externalValues, ConnectedPort_A), _b.Evaluate(externalValues, ConnectedPort_B), _c.Evaluate(externalValues, ConnectedPort_C));
        }
    }
}
