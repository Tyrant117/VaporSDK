using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Vapor.VisualScripting;

namespace VaporEditor.VisualScripting
{
    public abstract class GraphToolsNode<U> : UnityEditor.Experimental.GraphView.Node, IGraphEditorNode where U : NodeModel
    {
        private U _node;
        public U Node { get => _node; protected set => _node = value; }

        public Dictionary<string, Port> InPorts { get; set; } = new();
        public Dictionary<string, Port> OutPorts { get; set; } = new();

        public GraphEditorView View { get; protected set; }

        public event Action<string> ConnectedPort;
        public event Action<string> DisconnectedPort;

        public NodeModel GetNode() => Node;

        public void OnConnectedInputEdge(string portName)
        {
            Debug.Log($"Connected {portName}");
            ConnectedPort?.Invoke(portName);
        }

        public void OnDisconnectedInputEdge(string portName)
        {
            Debug.Log($"Disconnected {portName}");
            DisconnectedPort?.Invoke(portName);
        }
    }
}
