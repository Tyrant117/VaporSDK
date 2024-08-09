using UnityEngine;

namespace Vapor.Graphs
{
    public interface IEvaluatorNode<T, U>
    {
        T Evaluate(GraphModel graph, U arg);
    }
}
