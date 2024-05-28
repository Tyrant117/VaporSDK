using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.GraphTools
{
    [SearchableNode("Logic/Convert/Bool To Float", "Bool to Float", "logic")]
    public class BoolToFloatNodeSo : MathNodeSo
    {
        [PortIn("Bool", 0, true, typeof(bool))]
        public NodeSo Bool;

        [PortOut("Out", 0, true, typeof(bool))]
        public NodeSo Out;

        public int InConnectedPort_Bool;

        public int OutConnectedPort_Out;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<bool> _a;

        public override float Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<bool>)Bool;
                _hasInit = true;
            }

            return Convert.ToSingle(_a.Evaluate(externalValues, 0));
        }

        //public override void LinkNodeData(List<NodeSo> nodesToLink, Action<NodeSo> callback)
        //{
        //    Assert.IsTrue(Edges.Count == 1, $"Node:{name} of type:{nameof(BoolToFloatNodeSo)} must have a 1 edges. Edge Count:{Edges.Count}");

        //    _bool = nodesToLink.First(x => Edges[0] == x.GetGuid());
        //}
    }
}
