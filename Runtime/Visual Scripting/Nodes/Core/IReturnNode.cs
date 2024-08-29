using UnityEngine;

namespace Vapor.VisualScripting
{
    public interface IReturnNode<T>
    {
        T GetValue(IGraphOwner owner, string portName = "");
    }
}
