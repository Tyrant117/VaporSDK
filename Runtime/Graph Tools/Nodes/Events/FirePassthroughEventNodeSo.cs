using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vapor.GraphTools
{
    [SearchableNode("Events/Fire Passthrough Event", "Fire Event", "events")]
    public class FirePassthroughEventNodeSo : NodeSo, IEvaluatorNode<bool>, IEvaluatorNode<int>, IEvaluatorNode<float>
    {
        [PortIn("In", 0, true, typeof(bool), typeof(int), typeof(float))]
        public NodeSo In;
        [PortIn("Event", 1, false, typeof(int))]
        public NodeSo Event;

        [PortOut("Out", 0, true, typeof(bool), typeof(int), typeof(float))]
        public NodeSo Out;

        public int InConnectedPort_In;
        public int InConnectedPort_Event;
        public int OutConnectedPort_Out;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<bool> _bool;
        [NonSerialized]
        private IEvaluatorNode<int> _int;
        [NonSerialized]
        private IEvaluatorNode<float> _float;
        [NonSerialized]
        private IEvaluatorNode<int> _event;
        [NonSerialized]
        private IArgsEvaluatorNode _eventArgs;

        private void Init()
        {
            if (!_hasInit)
            {
                _bool = (IEvaluatorNode<bool>)In;
                _int = (IEvaluatorNode<int>)In;
                _float = (IEvaluatorNode<float>)In;
                _event = (IEvaluatorNode<int>)Event;
                _eventArgs = (IArgsEvaluatorNode)Event;
                _hasInit = true;
            }
        }

        bool IEvaluatorNode<bool>.GetValue(int portIndex)
        {
            Init();

            return _bool.GetValue(portIndex);
        }

        bool IEvaluatorNode<bool>.Evaluate(IExternalValueGetter getter, int portIndex)
        {
            Init();

            var val = _bool.Evaluate(getter, InConnectedPort_In);
            getter.FireEvent(_event.Evaluate(getter, InConnectedPort_Event), _eventArgs.GetArgs(getter));
            return val;
        }


        int IEvaluatorNode<int>.GetValue(int portIndex)
        {
            Init();

            return _int.GetValue(portIndex);
        }
        int IEvaluatorNode<int>.Evaluate(IExternalValueGetter getter, int portIndex)
        {
            Init();

            var val = _int.Evaluate(getter, InConnectedPort_In);
            getter.FireEvent(_event.Evaluate(getter, InConnectedPort_Event), _eventArgs.GetArgs(getter));
            return val;
        }

        public float GetValue(int portIndex)
        {
            Init();

            return _float.GetValue(portIndex);
        }
        float IEvaluatorNode<float>.Evaluate(IExternalValueGetter getter, int portIndex)
        {
            Init();

            var val = _float.Evaluate(getter, InConnectedPort_In);
            getter.FireEvent(_event.Evaluate(getter, InConnectedPort_Event), _eventArgs.GetArgs(getter));
            return val;
        }

    }
}
