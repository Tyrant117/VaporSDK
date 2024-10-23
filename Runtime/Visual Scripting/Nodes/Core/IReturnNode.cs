using UnityEngine;

namespace Vapor.VisualScripting
{
    public interface IReturnNode<T> : INode
    {
        T GetValue(IGraphOwner owner, string portName = "");
    }
}
