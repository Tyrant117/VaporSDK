using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Vapor.GraphTools.Math;

namespace Vapor.GraphTools
{
    [SearchableNode("Value/Fixed Delta Time Value", "Fixed Delta Time")]
    public class FixedDeltaTimeValueNodeSo : ValueNodeSo<float>, IEvaluatorNode<float>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetValue(int portIndex) => Time.fixedDeltaTime;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(IExternalValueGetter externalValues, int portIndex) => Time.fixedDeltaTime;        
    }
}
