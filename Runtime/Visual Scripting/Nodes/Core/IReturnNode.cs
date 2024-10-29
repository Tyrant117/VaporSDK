using UnityEngine;

namespace Vapor.VisualScripting
{
    public interface IReturnNode : INode
    {
        object GetBoxedValue(IGraphOwner owner, int portIndex);
    }

    public interface IReturnNode<T> : IReturnNode
    {
        T GetValue(IGraphOwner owner, int portIndex);
    }
}
