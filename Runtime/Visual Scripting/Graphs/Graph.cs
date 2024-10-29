using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using Vapor.Inspector;

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

        public string DebugName { get; set; }

        public abstract IGraph Build(bool refresh = false, string debugName = "");
        public abstract object GraphSettingsInspector();

        public NodeModel Get(NodeReference a)
        {
            Assert.IsTrue(Nodes.Exists(c => c.Guid == a.Guid), $"{TooltipMarkup.Class(AssemblyQualifiedType.Name)} - [{DebugName}] - Does not have nodes that matches Guid {a.Guid}");
            return Nodes.First(c => c.Guid == a.Guid);
        }
    }
}
