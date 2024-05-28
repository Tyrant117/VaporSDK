using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace Vapor.GraphTools
{
    [SearchableNode("Logic/Chance/Random Chance", "Random Chance", "logic")]
    public class RandomChanceNodeSo : LogicNodeSo
    {
        [PortIn("Chance", 0, true, typeof(float))]
        public NodeSo Chance;

        [PortOut("Out", 0, true, typeof(bool))]
        public NodeSo Out;

        public int InConnectedPort_Chance;
        public int OutConnectedPort_Out;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<float> _chance;

        public override bool Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _chance = (IEvaluatorNode<float>)Chance;
                _hasInit = true;
            }

            return UnityEngine.Random.Range(0, 1f) <= _chance.Evaluate(externalValues, InConnectedPort_Chance);
        }
    }
}
