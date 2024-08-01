using UnityEngine;

namespace Vapor
{
    public interface IEvaluatorNode<T, U>
    {
        T Evaluate(U arg);
    }
}
