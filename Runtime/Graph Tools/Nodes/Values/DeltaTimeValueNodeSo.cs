using System.Runtime.CompilerServices;
using UnityEngine;

namespace Vapor.GraphTools
{
    [SearchableNode("Value/Delta Time Value", "Delta Time")]
    public class DeltaTimeValueNodeSo : ValueNodeSo<float>, IEvaluatorNode<float>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetValue(int portIndex) => Time.deltaTime;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(IExternalValueGetter externalValues, int portIndex) => Time.deltaTime;

    }
}
