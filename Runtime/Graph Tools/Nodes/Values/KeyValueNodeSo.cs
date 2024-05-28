using System;
using System.Runtime.CompilerServices;
using Vapor.Keys;

namespace Vapor.GraphTools
{
    public class KeyValueNodeSo : ValueNodeSo<KeyDropdownValue>, IEvaluatorNode<int>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Type GetValueType() => typeof(KeyDropdownValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetValue(int portIndex) => Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Evaluate(IExternalValueGetter getter, int portIndex) => Value;
    }
}
