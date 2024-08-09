using UnityEngine;

namespace Vapor.Graphs
{
    public abstract class LogicNode : NodeModel, IEvaluatorNode<bool, IExternalValueSource>
    {
        public abstract bool Evaluate(GraphModel graph, IExternalValueSource arg);
    }
}
