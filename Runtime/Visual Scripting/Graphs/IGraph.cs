using System;
using UnityEngine;

namespace Vapor.VisualScripting
{
    public interface IGraph
    {
        uint Id { get; }

        void Evaluate(IGraphOwner graphOwner);

        void Traverse(Action<INode> callback);
    }
}
