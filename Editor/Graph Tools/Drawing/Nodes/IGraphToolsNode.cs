using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using Vapor.GraphTools;

namespace VaporEditor.GraphTools
{
    public interface IGraphToolsNode
    {
        List<Port> Ports { get; }

        NodeSo GetNode();
    }
}
