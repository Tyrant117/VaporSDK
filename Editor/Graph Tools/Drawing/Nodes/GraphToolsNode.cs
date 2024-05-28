using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Vapor.GraphTools;

namespace VaporEditor.GraphTools
{
    public abstract class GraphToolsNode<T, U> : Node, IGraphToolsNode where T : ScriptableObject where U : NodeSo
    {
        private U _node;
        public U Node { get => _node; protected set => _node = value; }

        public List<Port> InPorts { get; set; } = new();
        public List<Port> OutPorts { get; set; } = new();

        public GraphEditorView<T> View { get; protected set; }

        public NodeSo GetNode() => Node;
    }
}
