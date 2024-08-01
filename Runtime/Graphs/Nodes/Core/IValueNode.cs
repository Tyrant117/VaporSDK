using UnityEngine;

namespace Vapor
{
    public interface IValueNode<T>
    {
        T GetValue(int portIndex);
    }
}
