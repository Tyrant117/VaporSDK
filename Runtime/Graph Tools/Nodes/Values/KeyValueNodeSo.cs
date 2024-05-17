using System.Runtime.CompilerServices;
using Vapor.GraphTools.Math;
using Vapor.Keys;

namespace Vapor.GraphTools
{
    public class KeyValueNodeSo : ValueNodeSo<KeyDropdownValue>, IEvaluatorNode<int>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetValue(int portIndex) => Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Evaluate(IExternalValueGetter getter, int portIndex) => Value;
    }
}
