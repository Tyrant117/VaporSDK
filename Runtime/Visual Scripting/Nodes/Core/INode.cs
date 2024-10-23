using System;
using UnityEngine;

namespace Vapor.VisualScripting
{
    public interface INode
    {
        uint Id { get; }
        IGraph Graph { get; set; }

        void Traverse(Action<INode> callback);
    }
}
