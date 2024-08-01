using UnityEngine;

namespace Vapor.Graphs
{
    public abstract class MathNode : Node, IEvaluatorNode<double, IExternalValueSource>
    {
        public abstract double Evaluate(IExternalValueSource arg);
    }
}
