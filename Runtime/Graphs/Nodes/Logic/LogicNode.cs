using UnityEngine;

namespace Vapor.Graphs
{
    public abstract class LogicNode : Node, IEvaluatorNode<bool, IExternalValueSource>
    {
        public abstract bool Evaluate(IExternalValueSource arg);
    }
}
