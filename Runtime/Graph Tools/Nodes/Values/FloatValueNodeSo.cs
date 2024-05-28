using System;
using System.Runtime.CompilerServices;

namespace Vapor.GraphTools
{
    [SearchableNode("Value/Float Value", "Float", "values")]
    public class FloatValueNodeSo : ValueNodeSo<float>, IEvaluatorNode<float>
    {
        [PortOut("", 0, true, typeof(float))]
        public NodeSo Out;
        public int OutConnectedPort_Out;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Type GetValueType() => typeof(float);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetValue(int portIndex) => Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(IExternalValueGetter externalValues, int portIndex) => Value;        
    }
}
