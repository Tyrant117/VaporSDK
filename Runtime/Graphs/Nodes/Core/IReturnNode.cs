using UnityEngine;

namespace Vapor.Graphs
{
    public interface IReturnNode<T>
    {
        T GetValue(IGraphOwner owner, string portName = "");
    }
}
