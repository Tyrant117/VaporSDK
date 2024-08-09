using UnityEngine;
using Vapor.Graphs;

namespace Vapor
{
    public interface IConvertNode<T,U>
    {
        U Convert(IGraphOwner owner, T from);
    }
}
