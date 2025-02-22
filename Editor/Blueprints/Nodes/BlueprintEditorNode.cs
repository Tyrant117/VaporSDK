using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Blueprints;
using Vapor.Inspector;
using VaporEditor.Inspector;
using PointerType = UnityEngine.UIElements.PointerType;

namespace VaporEditor.Blueprints
{

    public class BlueprintRedirectNode : Node, IBlueprintEditorNode
    {
        public BlueprintNodeDataModel Model { get; }
        public Dictionary<string, BlueprintEditorPort> InPorts { get; private set; } = new();
        public Dictionary<string, BlueprintEditorPort> OutPorts { get; private set; } = new();
        public BlueprintView View { get; }
        
        public event Action<string> ConnectedPort;
        public event Action<string> DisconnectedPort;
        
        public BlueprintRedirectNode(BlueprintEditorView view, BlueprintNodeDataModel node, EdgeConnectorListener edgeConnectorListener)
        {
            View = view.GraphView;
            Model = node;
            
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/RedirectNode"));
            
            CreateFlowInPorts();
            CreateFlowOutPorts();
        }

        public BlueprintRedirectNode(BlueprintView view, BlueprintNodeDataModel node)
        {
            View = view;
            Model = node;
            
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/RedirectNode"));
            
            CreateFlowInPorts();
            CreateFlowOutPorts();
        }

        private void CreateFlowInPorts()
        {
            if (Model.InPorts == null)
            {
                return;
            }
            InPorts = new(Model.InPorts.Count);
            foreach (var slot in Model.InPorts.Values)
            {
                var port = BlueprintEditorPort.Create(this, slot);
                if (slot.IsOptional)
                {
                    port.AddToClassList("optionalPort");
                }
                InPorts.Add(slot.PortName, port);
                port.Q<Label>().Hide();
                inputContainer.Add(port);
            }
        }

        private void CreateFlowOutPorts()
        {
            if (Model.OutPorts == null)
            {
                return;
            }
            
            OutPorts = new(Model.OutPorts.Count);
            foreach (var slot in Model.OutPorts.Values)
            {
                var port = BlueprintEditorPort.Create(this, slot);
                if (slot.IsOptional)
                {
                    port.AddToClassList("optionalPort");
                }
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
        public BlueprintNodeDataModel Model { get; }
        public Dictionary<string, BlueprintEditorPort> InPorts { get; private set; } = new();
        public Dictionary<string, BlueprintEditorPort> OutPorts { get; private set; } = new();
        public BlueprintView View { get; }
        
        public event Action<string> ConnectedPort;
        public event Action<string> DisconnectedPort;
        
        public BlueprintEditorToken(BlueprintEditorView view, BlueprintNodeDataModel node, EdgeConnectorListener edgeConnectorListener) : base(null, null)
        {
            View = view.GraphView;
            Model = node;
            
            var nameTuple = node.GetNodeName();
            var iconTuple = node.GetNodeNameIcon();
            CreateTitle(nameTuple.Item1, nameTuple.Item2, iconTuple.Item1, iconTuple.Item2);
            style.width = 80;
            var top = this.Q<VisualElement>("top");
            top.style.alignSelf = Align.Stretch;
            top.style.justifyContent = Justify.SpaceBetween;
            
            CreateFlowInPorts();
            CreateFlowOutPorts();
            
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

        private void CreateFlowInPorts()
        {
            if (Model.InPorts == null)
            {
                return;
            }
            InPorts = new(Model.InPorts.Count);
            foreach (var slot in Model.InPorts.Values)
            {
                var port = BlueprintEditorPort.Create(this, slot);
                if (slot.IsOptional)
                {
                    port.AddToClassList("optionalPort");
                }
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

        private void CreateFlowOutPorts()
        {
            if (Model.OutPorts == null)
            {
                return;
            }
            OutPorts = new(Model.OutPorts.Count);
            foreach (var slot in Model.OutPorts.Values)
            {
                var port = BlueprintEditorPort.Create(this, slot);
                if (slot.IsOptional)
                {
                    port.AddToClassList("optionalPort");
                }
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
        public BlueprintNodeDataModel Model { get; protected set; }
        
        public Dictionary<string, BlueprintEditorPort> InPorts { get; private set; } = new();
        public Dictionary<string, BlueprintEditorPort> OutPorts { get; private set; } = new();
        
        public BlueprintView View { get; protected set; }
        
        private List<FieldInfo> _nodeContentData;

        public event Action<string> ConnectedPort;
        public event Action<string> DisconnectedPort;
        
        public BlueprintEditorNode(BlueprintEditorView view, BlueprintNodeDataModel node, EdgeConnectorListener edgeConnectorListener)
        {
            View = view.GraphView;
            Model = node;

            m_CollapseButton.RemoveFromHierarchy();
            StyleNode();
            var nameTuple = node.GetNodeName();
            var iconTuple = node.GetNodeNameIcon();
            CreateTitle(nameTuple.Item1, nameTuple.Item2, iconTuple.Item1, iconTuple.Item2);

            CreateFlowInPorts();
            CreateFlowOutPorts();

            RefreshExpandedState();
        }

        public BlueprintEditorNode(BlueprintView view, BlueprintNodeDataModel node)
        {
            View = view;
            Model = node;

            m_CollapseButton.RemoveFromHierarchy();
            StyleNode();
            var nameTuple = node.GetNodeName();
            var iconTuple = node.GetNodeNameIcon();
            CreateTitle(nameTuple.Item1, nameTuple.Item2, iconTuple.Item1, iconTuple.Item2);

            CreateFlowInPorts();
            CreateFlowOutPorts();

            RefreshExpandedState();

            ConnectedPort += OnConnectedPort;
            DisconnectedPort += OnDisconnectedPort;
        }

        private void OnConnectedPort(string portName)
        {
            
        }
        
        private void OnDisconnectedPort(string portName)
        {
            
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
        
        public void RedrawPorts()
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

            CreateFlowInPorts();
            CreateFlowOutPorts();
        }
        
        private void CreateFlowInPorts()
        {
            if (Model.InPorts == null)
            {
                return;
            }
            InPorts = new Dictionary<string, BlueprintEditorPort>(Model.InPorts.Count);
            foreach (var pin in Model.InPorts.Values)
            {
                var port = BlueprintEditorPort.Create(this, pin);
                if (pin.IsOptional)
                {
                    port.AddToClassList("optionalPort");
                }
                InPorts.Add(pin.PortName, port);
                inputContainer.Add(port);

                if (pin.HasInlineValue)
                {
                    inputContainer.Add(port.DrawnField);
                }
            }
        }

        private void CreateFlowOutPorts()
        {
            if (Model.OutPorts == null)
            {
                return;
            }
            OutPorts = new Dictionary<string, BlueprintEditorPort>(Model.OutPorts.Count);
            foreach (var pin in Model.OutPorts.Values)
            {
                var port = BlueprintEditorPort.Create(this, pin);
                if (pin.IsOptional)
                {
                    port.AddToClassList("optionalPort");
                }

                
                OutPorts.Add(pin.PortName, port);
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
