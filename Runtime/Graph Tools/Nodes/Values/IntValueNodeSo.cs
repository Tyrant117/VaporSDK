using System;
using System.Runtime.CompilerServices;

namespace Vapor.GraphTools
{
    [SearchableNode("Value/Int Value", "Int", "values")]
    public class IntValueNodeSo : ValueNodeSo<int>, IEvaluatorNode<int>
    {
        [PortOut("", 0, true, typeof(int))]
        public NodeSo Out;
        public int OutConnectedPort_Out;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Type GetValueType() => typeof(int);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetValue(int portIndex) => Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Evaluate(IExternalValueGetter externalValues, int portIndex) => Value;
    }
}
