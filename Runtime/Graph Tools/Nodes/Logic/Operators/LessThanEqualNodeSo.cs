using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.GraphTools.Math
{
    [SearchableNode("Logic/Operators/Less Than Or Equal", "A <= B", "logic")]
    public class LessThanEqualNodeSo : LogicNodeSo
    {
        [PortIn("A", 0, true, typeof(float))]
        public NodeSo A;
        [PortIn("B", 1, true, typeof(float))]
        public NodeSo B;

        [PortOut("Out", 0, true, typeof(bool))]
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

        public override bool Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<float>)A;
                _b = (IEvaluatorNode<float>)B;
                _hasInit = true;
            }

            return _a.Evaluate(externalValues, InConnectedPort_A) <= _b.Evaluate(externalValues, InConnectedPort_B);
        }
    }
}
