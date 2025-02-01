using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Blueprints;
using Vapor.Inspector;
using VaporEditor.Inspector;

namespace VaporEditor.Blueprints
{
    public class BlueprintEditorNode : UnityEditor.Experimental.GraphView.Node, IBlueprintEditorNode
    {
        private static readonly StyleSheet s_PortColors = Resources.Load<StyleSheet>("Styles/PortColors");
        
        public BlueprintNodeDataModel Node { get; protected set; }
        
        public Dictionary<string, Port> InPorts { get; set; } = new();
        public Dictionary<string, Port> OutPorts { get; set; } = new();
        
        public BlueprintEditorView View { get; protected set; }
        
        private List<FieldInfo> _nodeContentData;

        public event Action<string> ConnectedPort;
        public event Action<string> DisconnectedPort;
        
        public BlueprintEditorNode(BlueprintEditorView view, BlueprintNodeDataModel node, EdgeConnectorListener edgeConnectorListener)
        {
            View = view;
            Node = node;
            Node.RenameNode = OnRenameNode;

            m_CollapseButton.RemoveFromHierarchy();
            StyleNode();
            var nameTuple = node.GetNodeName();
            var iconTuple = node.GetNodeNameIcon();
            CreateTitle(nameTuple.Item1, nameTuple.Item2, iconTuple.Item1, iconTuple.Item2);

            CreateFlowInPorts(edgeConnectorListener);
            CreateFlowOutPorts(edgeConnectorListener);

            RefreshExpandedState();
        }
        
        private void StyleNode()
        {
            // var nodeType = Node.GetType();
            // var width = nodeType.GetCustomAttribute<NodeWidthAttribute>();
            // if (width != null)
            // {
            //     style.minWidth = width.MinWidth;
            // }
        }
        
        private void CreateTitle(string newTitle, Color titleTint, Sprite titleIcon, Color titleIconTint)
        {
            title = newTitle;
            var label = titleContainer.Q<Label>();
            label.style.color = titleTint;
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginRight = 6;
            label.style.marginLeft = 6;

            if (titleIcon != null)
            {
                titleContainer.Insert(0, new Image()
                {
                    sprite = titleIcon,
                    tintColor = titleIconTint,
                    scaleMode = ScaleMode.ScaleToFit,
                    style =
                    {
                        maxWidth = 16,
                        maxHeight = 16,
                        alignSelf = Align.Center,
                        marginLeft = 6,
                    }
                });
            }
        }

        private void OnRenameNode()
        {
            Debug.Log($"Setting Title: [{Node.GetNodeName()}]");
            var nameTuple = Node.GetNodeName();
            title = nameTuple.Item1;
            titleContainer.Q<Label>().style.color = nameTuple.Item2;
            var iconTuple = Node.GetNodeNameIcon();
            if (iconTuple.Item1 != null)
            {
                var image = titleContainer.Q<Image>();
                if(image == null)
                {
                    titleContainer.Insert(0,new Image()
                    {
                        sprite = iconTuple.Item1,
                        tintColor = iconTuple.Item2,
                        scaleMode = ScaleMode.ScaleToFit,
                        style =
                        {
                            maxWidth = 16,
                            maxHeight = 16,
                            alignSelf = Align.Center,
                            marginLeft = 6,
                        }
                    });
                }
                else
                {
                    image.sprite = iconTuple.Item1;
                    image.tintColor = iconTuple.Item2;
                }
            }
        }
        
        public void RedrawPorts(EdgeConnectorListener edgeConnectorListener)
        {
            List<Edge> edgesToRemove = new();
            foreach (var port in InPorts.Values)
            {
                edgesToRemove.AddRange(port.connections);
                port.DisconnectAll();
            }
            InPorts.Clear();

            foreach (var port in OutPorts.Values)
            {
                edgesToRemove.AddRange(port.connections);
                port.DisconnectAll();
            }
            OutPorts.Clear();
            
            foreach (var e in edgesToRemove)
            {
                e.output.Disconnect(e);
                e.input.Disconnect(e);
                e.RemoveFromHierarchy();
            }

            inputContainer.DisconnectChildren();
            outputContainer.DisconnectChildren();

            CreateFlowInPorts(edgeConnectorListener);
            CreateFlowOutPorts(edgeConnectorListener);
        }
        
        private void CreateFlowInPorts(EdgeConnectorListener edgeConnectorListener)
        {
            if (Node.InPorts == null)
            {
                return;
            }
            InPorts = new(Node.InPorts.Count);
            foreach (var slot in Node.InPorts.Values)
            {
                var port = BlueprintEditorPort.Create(this, slot, edgeConnectorListener);
                if (slot.IsOptional)
                {
                    port.AddToClassList("optionalPort");
                }
                port.tooltip = "The flow input";
                port.styleSheets.Add(s_PortColors);
                InPorts.Add(slot.PortName, port);
                inputContainer.Add(port);

                if (slot.HasContent)
                {
                    var ve = DrawerUtility.DrawVaporFieldFromType(slot, slot.ContentType, slot.ContentFieldInfo, true);
                    inputContainer.Add(ve);
                    ConnectedPort += (p) =>
                    {
                        if (p != slot.PortName)
                        {
                            return;
                        }
                        ve.Hide();
                    };
                    DisconnectedPort += (p) =>
                    {
                        if (p != slot.PortName)
                        {
                            return;
                        }
                        ve.Show();
                    };
                }
            }
        }

        private void CreateFlowOutPorts(EdgeConnectorListener edgeConnectorListener)
        {
            if (Node.OutPorts == null)
            {
                return;
            }
            OutPorts = new(Node.OutPorts.Count);
            foreach (var slot in Node.OutPorts.Values)
            {
                var port = BlueprintEditorPort.Create(this, slot, edgeConnectorListener);
                if (slot.IsOptional)
                {
                    port.AddToClassList("optionalPort");
                }
                port.tooltip = "The flow output";
                port.styleSheets.Add(s_PortColors);
                OutPorts.Add(slot.PortName, port);
                outputContainer.Add(port);
            }
        }
        
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
