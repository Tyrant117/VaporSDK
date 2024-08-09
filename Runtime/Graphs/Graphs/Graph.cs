using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.Graphs
{
    public class Graph : IGraph
    {
        public uint Id { get; }
        public readonly INode Root;

        public Graph(INode root)
        {
            Root = root;
        }
    }

    [Serializable]
    public class GraphModel
    {
        public Type AssemblyQualifiedType;
        public NodeModel Root;
        public List<NodeModel> Children;

        public virtual IGraph Build(bool refresh = false)
        {
            if (refresh)
            {
                Root.Refresh();
                foreach (var c in Children)
                {
                    c.Refresh();
                }
            }

            var root = Root.Build(this);
            return new Graph(root);
        }

        public virtual NodeModel GenerateDefaultRootNode() { return null; }

        internal NodeModel Get(NodeReference a)
        {
            return Children.First(c => c.Guid == a.Guid);
        }
    }
}
