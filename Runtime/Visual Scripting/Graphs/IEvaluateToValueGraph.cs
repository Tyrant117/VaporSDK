namespace Vapor.VisualScripting
{
    public interface IEvaluateToValueGraph<T> : IGraph
    {
        T Value { get; }
    }
}
