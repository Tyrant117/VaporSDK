using System;
using System.Runtime.CompilerServices;

namespace Vapor.GraphTools
{
    [SearchableNode("Value/Bool Value", "Bool", "values")]
    public class BoolValueNodeSo : ValueNodeSo<bool>, IEvaluatorNode<float>, IEvaluatorNode<bool>
    {
        [PortOut("", 0, true, typeof(bool))]
        public NodeSo Out;
        public int OutConnectedPort_Out;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Type GetValueType() => typeof(bool);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IEvaluatorNode<bool>.GetValue(int portIndex) => Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float IEvaluatorNode<float>.GetValue(int portIndex) => Value ? 1 : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float IEvaluatorNode<float>.Evaluate(IExternalValueGetter externalValues, int portIndex) => Value ? 1 : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IEvaluatorNode<bool>.Evaluate(IExternalValueGetter getter, int portIndex) => Value;        
    }
}
