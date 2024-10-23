using System;
using System.Collections.Generic;
using System.Linq;

namespace Vapor.VisualScripting
{
    public class Graph : IGraph
    {
        public uint Id { get; }
        public readonly INode Root;

        public Graph(INode root)
        {
            Root = root;
        }

        public void Evaluate(IGraphOwner graphOwner)
        {

        }

        public void Traverse(Action<INode> callback)
        {

        }
    }

    [Serializable]
    public abstract class GraphModel
    {
        public Type AssemblyQualifiedType;
        public List<NodeModel> Nodes;

        public abstract IGraph Build(bool refresh = false);
        public abstract object GraphSettingsInspector();

        public NodeModel Get(NodeReference a)
        {
            return Nodes.First(c => c.Guid == a.Guid);
        }
    }
}
