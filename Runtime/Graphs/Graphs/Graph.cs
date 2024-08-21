using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
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
        public NodeModel Entry;
        public NodeModel Exit;
        public List<NodeModel> Children;

        public virtual IGraph Build(bool refresh = false)
        {
            if (refresh)
            {
                Exit.Refresh();
                foreach (var c in Children)
                {
                    c.Refresh();
                }
            }

            var root = Exit.Build(this);
            return new Graph(root);
        }

        public virtual NodeModel GenerateDefaultEntryNode() { return null; }
        public virtual NodeModel GenerateDefaultExitNode() { return null; }

        public virtual object ElementToDraw(out FieldInfo[] fields) { fields = null;  return null; }

        internal NodeModel Get(NodeReference a)
        {
            return Children.First(c => c.Guid == a.Guid);
        }
    }
}
