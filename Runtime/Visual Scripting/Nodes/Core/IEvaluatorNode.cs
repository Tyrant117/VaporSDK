using UnityEngine;

namespace Vapor.VisualScripting
{
    public interface IEvaluatorNode2<TArg>
    {
        bool TryEvaluate<TResult>(IGraph graph, TArg arg, out TResult result);
    }

    public interface IEvaluatorNode<T, U>
    {
        T Evaluate(GraphModel graph, U arg);
    }
}
