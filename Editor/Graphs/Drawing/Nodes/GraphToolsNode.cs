using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Vapor.Graphs;

namespace VaporEditor.Graphs
{
    public abstract class GraphToolsNode<U> : UnityEditor.Experimental.GraphView.Node, IGraphEditorNode where U : NodeModel
    {
        private U _node;
        public U Node { get => _node; protected set => _node = value; }

        public List<Port> InPorts { get; set; } = new();
        public List<Port> OutPorts { get; set; } = new();

        public GraphEditorView View { get; protected set; }

        public event Action<int> ConnectedPort;
        public event Action<int> DisconnectedPort;

        public NodeModel GetNode() => Node;

        public void OnConnectedInputEdge(int index)
        {
            ConnectedPort?.Invoke(index);
        }

        public void OnDisconnectedInputEdge(int index)
        {
            Debug.Log($"Disconnected {index}");
            DisconnectedPort?.Invoke(index);
        }
    }
}
