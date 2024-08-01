using System;
using UnityEngine;

namespace Vapor.Graphs
{
    [SearchableNode("Math/Basic/Square Root", "Square Root", "math")]
    public class SquareRootNode : MathNode
    {
        [PortIn("A", 0, true, typeof(double))]
        public Node A;

        [PortOut("Out", 0, true, typeof(double))]
        public Node Out;

        public int InConnectedPort_A;
        public int OutConnectedPort_Out;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<double, IExternalValueSource> _a;

        public override double Evaluate(IExternalValueSource arg)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<double, IExternalValueSource>)A;
                _hasInit = true;
            }

            return Math.Sqrt(_a.Evaluate(arg));
        }
    }
}
