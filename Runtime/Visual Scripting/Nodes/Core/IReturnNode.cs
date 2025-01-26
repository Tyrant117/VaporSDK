using UnityEngine;

namespace Vapor.VisualScripting
{
    public interface IReturnNode : INode
    {
        object GetBoxedValue(IGraphOwner owner, int portIndex);
    }

    public interface IReturnNode<out T> : IReturnNode
    {
        T GetValue(IGraphOwner owner, int portIndex);
    }

    public interface IHasValuePorts : INode
    {
        public T GetValue<T>(IGraphOwner owner, string portName);
    }
}
