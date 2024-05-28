using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Vapor.GraphTools
{
    [SearchableNode("Value/Delta Time Value", "Delta Time", "values")]
    public class DeltaTimeValueNodeSo : ValueNodeSo<float>, IEvaluatorNode<float>
    {
        [PortOut("", 0, true, typeof(float))]
        public NodeSo Out;
        public int OutConnectedPort_Out;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Type GetValueType() => typeof(float);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetValue(int portIndex) => Time.deltaTime;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(IExternalValueGetter externalValues, int portIndex) => Time.deltaTime;

    }
}
