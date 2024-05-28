using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace Vapor.GraphTools
{
    [SearchableNode("Logic/Operators/Equal To", "A == B", "logic")]
    public class EqualToNodeSo : LogicNodeSo
    {
        [PortIn("A", 0, true, typeof(int))]
        public NodeSo A;
        [PortIn("B", 1, true, typeof(int))]
        public NodeSo B;

        [PortOut("Out", 0, true, typeof(bool))]
        public NodeSo Out;

        public int InConnectedPort_A;
        public int InConnectedPort_B;

        public int OutConnectedPort_Out;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<int> _a;
        [NonSerialized]
        private IEvaluatorNode<int> _b;

        public override bool Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<int>)A;
                _b = (IEvaluatorNode<int>)B;
                _hasInit = true;
            }

            return _a.Evaluate(externalValues, InConnectedPort_A) == _b.Evaluate(externalValues, InConnectedPort_B);
        }
    }
}
