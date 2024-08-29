using UnityEngine;
using Vapor.VisualScripting;

namespace Vapor
{
    public interface IConvertNode<T,U>
    {
        U Convert(IGraphOwner owner, T from);
    }
}
