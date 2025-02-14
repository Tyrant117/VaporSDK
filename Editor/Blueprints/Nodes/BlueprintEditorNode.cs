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

    public class BlueprintRedirectNode : Node, IBlueprintEditorNode
    {
        private static readonly StyleSheet s_PortColors = Resources.Load<StyleSheet>("Styles/PortColors");
        
        public BlueprintNodeDataModel Node { get; }
        public Dictionary<string, Port> InPorts { get; private set; } = new();
        public Dictionary<string, Port> OutPorts { get; private set; } = new();
        public BlueprintEditorView View { get; }
        
        public event Action<string> ConnectedPort;
        public event Action<string> DisconnectedPort;
        
        public BlueprintRedirectNode(BlueprintEditorView view, BlueprintNodeDataModel node, EdgeConnectorListener edgeConnectorListener)
        {
            View = view;
            Node = node;
            
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/RedirectNode"));
            
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
                port.tooltip = IBlueprintEditorNode.CreateTooltipForPin(slot);
                port.styleSheets.Add(s_PortColors);
                InPorts.Add(slot.PortName, port);
                port.Q<Label>().Hide();
                inputContainer.Add(port);
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
                port.tooltip = IBlueprintEditorNode.CreateTooltipForPin(slot);
                port.styleSheets.Add(s_PortColors);
                OutPorts.Add(slot.PortName, port);
                port.Q<Label>().Hide();
                outputContainer.Add(port);
            }
        }
        
        public void RedrawPorts(EdgeConnectorListener edgeConnectorListener)
        {
            
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
    
    public class BlueprintEditorToken : TokenNode, IBlueprintEditorNode
    {
        private static readonly StyleSheet s_PortColors = Resources.Load<StyleSheet>("Styles/PortColors");

        public BlueprintNodeDataModel Node { get; }
        public Dictionary<string, Port> InPorts { get; private set; } = new();
        public Dictionary<string, Port> OutPorts { get; private set; } = new();
        public BlueprintEditorView View { get; }
        
        public event Action<string> ConnectedPort;
        public event Action<string> DisconnectedPort;
        
        public BlueprintEditorToken(BlueprintEditorView view, BlueprintNodeDataModel node, EdgeConnectorListener edgeConnectorListener) : base(null, null)
        {
            View = view;
            Node = node;
            
            var nameTuple = node.GetNodeName();
            var iconTuple = node.GetNodeNameIcon();
            CreateTitle(nameTuple.Item1, nameTuple.Item2, iconTuple.Item1, iconTuple.Item2);
            style.width = 80;
            var top = this.Q<VisualElement>("top");
            top.style.alignSelf = Align.Stretch;
            top.style.justifyContent = Justify.SpaceBetween;
            
            CreateFlowInPorts(edgeConnectorListener);
            CreateFlowOutPorts(edgeConnectorListener);
            
            RefreshExpandedState();
        }
        
        private void CreateTitle(string newTitle, Color titleTint, Sprite titleIcon, Color titleIconTint)
        {
            title = newTitle;
            var label = this.Q<Label>("title-label");
            label.style.color = titleTint;
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginTop = 6;
            label.style.marginBottom = 6;
            label.style.marginLeft = 6;
            label.style.marginRight = 6;
            label.Hide();

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
                port.tooltip = IBlueprintEditorNode.CreateTooltipForPin(slot);
                port.styleSheets.Add(s_PortColors);
                InPorts.Add(slot.PortName, port);
                var pill = this.Q<Pill>("pill");
                port.Q<Label>().Hide();
                pill.left = port;

                if (slot.HasInlineValue)
                {
                    var ve = SerializedDrawerUtility.DrawFieldFromObject(slot.InlineValue, slot.InlineValue.GetType()); //SerializedDrawerUtility.DrawFieldFromType(slot, slot.ContentType, slot.ContentFieldInfo, true);
                    ve.Q<Label>()?.Hide();
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
                port.tooltip = IBlueprintEditorNode.CreateTooltipForPin(slot);
                port.styleSheets.Add(s_PortColors);
                OutPorts.Add(slot.PortName, port);
                var pill = this.Q<Pill>("pill");
                port.Q<Label>().Hide();
                pill.right = port;
            }
        }
        
        public void RedrawPorts(EdgeConnectorListener edgeConnectorListener)
        {
            
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
    
    public class BlueprintEditorNode : Node, IBlueprintEditorNode
    {
        private static readonly StyleSheet s_PortColors = Resources.Load<StyleSheet>("Styles/PortColors");
        
        public BlueprintNodeDataModel Node { get; protected set; }
        
        public Dictionary<string, Port> InPorts { get; private set; } = new();
        public Dictionary<string, Port> OutPorts { get; private set; } = new();
        
        public BlueprintEditorView View { get; protected set; }
        
        private List<FieldInfo> _nodeContentData;

        public event Action<string> ConnectedPort;
        public event Action<string> DisconnectedPort;
        
        public BlueprintEditorNode(BlueprintEditorView view, BlueprintNodeDataModel node, EdgeConnectorListener edgeConnectorListener)
        {
            View = view;
            Node = node;

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
            //     
            // }
            style.minWidth = 128;
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
                port.tooltip = IBlueprintEditorNode.CreateTooltipForPin(slot);
                port.styleSheets.Add(s_PortColors);
                InPorts.Add(slot.PortName, port);
                inputContainer.Add(port);

                if (slot.HasInlineValue)
                {
                    var ve = SerializedDrawerUtility.DrawFieldFromObject(slot.InlineValue, slot.InlineValue.GetType()); //SerializedDrawerUtility.DrawFieldFromType(slot, slot.ContentType, slot.ContentFieldInfo, true);//DrawerUtility.DrawVaporFieldFromType(slot, slot.ContentType, slot.ContentFieldInfo, true);
                    ve.Q<Label>()?.Hide();
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

                port.tooltip = IBlueprintEditorNode.CreateTooltipForPin(slot);
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

        #region - Helpers -

        

        #endregion
    }
}
