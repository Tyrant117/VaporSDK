using System;
using UnityEngine;

namespace Vapor.GraphTools.Math
{
    [SearchableNode("Math/Basic/Square Root", "Square Root", "math")]
    public class SquareRootNodeSo : MathNodeSo
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

            return Mathf.Sqrt(_a.Evaluate(externalValues, InConnectedPort_A));
        }
    }
}
