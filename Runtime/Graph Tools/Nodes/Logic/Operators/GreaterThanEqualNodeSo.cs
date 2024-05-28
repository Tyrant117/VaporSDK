using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.GraphTools.Math
{
    [SearchableNode("Logic/Operators/Greater Than Or Equal", "A >= B", "logic")]
    public class GreaterThanEqualNodeSo : LogicNodeSo
    {
        [PortIn("A", 0, true, typeof(float), typeof(int))]
        public NodeSo A;
        [PortIn("B", 1, true, typeof(float), typeof(int))]
        public NodeSo B;

        [PortOut("Out", 0, true, typeof(bool))]
        public NodeSo Out;

        public int InConnectedPort_A;
        public int InConnectedPort_B;

        public int OutConnectedPort_Out;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<DynamicValue> _a;
        [NonSerialized]
        private IEvaluatorNode<DynamicValue> _b;

        public override bool Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<DynamicValue>)A;
                _b = (IEvaluatorNode<DynamicValue>)B;
                _hasInit = true;
            }

            return _a.Evaluate(externalValues, portIndex) >= _b.Evaluate(externalValues, portIndex);
        }

        //public override void LinkNodeData(List<NodeSo> nodesToLink, Action<NodeSo> callback)
        //{
        //    Assert.IsTrue(Edges.Count == 2, $"Node:{name} of type:{nameof(GreaterThanEqualNodeSo)} must have a 2 edges. Edge Count:{Edges.Count}");

        //    base.LinkNodeData(nodesToLink, callback);
        //    A = nodesToLink.FirstOrDefault(x => Edges[0] == x.GetGuid());
        //    B = nodesToLink.FirstOrDefault(x => Edges[1] == x.GetGuid());
        //}
    }
}
