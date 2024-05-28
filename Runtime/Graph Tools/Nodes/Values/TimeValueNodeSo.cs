using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Vapor.GraphTools
{
    [SearchableNode("Value/Time Value", "Time", "values")]
    public class TimeValueNodeSo : ValueNodeSo<float>, IEvaluatorNode<float>
    {
        [PortOut("", 0, true, typeof(float))]
        public NodeSo Out;
        public int OutConnectedPort_Out;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Type GetValueType() => typeof(float);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetValue(int portIndex) => Time.time;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(IExternalValueGetter externalValues, int portIndex) => Time.time;        
    }
}
