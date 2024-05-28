using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Vapor.GraphTools
{
    [SearchableNode("Value/Fixed Delta Time Value", "Fixed Delta Time", "values")]
    public class FixedDeltaTimeValueNodeSo : ValueNodeSo<float>, IEvaluatorNode<float>
    {
        [PortOut("", 0, true, typeof(float))]
        public NodeSo Out;
        public int OutConnectedPort_Out;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Type GetValueType() => typeof(float);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetValue(int portIndex) => Time.fixedDeltaTime;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(IExternalValueGetter externalValues, int portIndex) => Time.fixedDeltaTime;        
    }
}
