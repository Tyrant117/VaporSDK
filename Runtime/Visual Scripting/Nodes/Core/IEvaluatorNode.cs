using UnityEngine;

namespace Vapor.VisualScripting
{
    public interface IEvaluatorNode<T, U>
    {
        T Evaluate(GraphModel graph, U arg);
    }
}
