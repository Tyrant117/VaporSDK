using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
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
        public BlueprintDesignNode Model { get; }
        public Dictionary<string, BlueprintEditorPort> InPorts { get; private set; } = new();
        public Dictionary<string, BlueprintEditorPort> OutPorts { get; private set; } = new();
        public BlueprintView View { get; }
        
        public event Action<string> ConnectedPort;
        public event Action<string> DisconnectedPort;

        public BlueprintRedirectNode(BlueprintView view, BlueprintDesignNode node)
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

        public void InvalidateName()
        {
            
        }

        public void InvalidateType()
        {
            
        }
    }
    
    public class BlueprintEditorToken : TokenNode, IBlueprintEditorNode
    {
        public BlueprintDesignNode Model { get; }
        public Dictionary<string, BlueprintEditorPort> InPorts { get; private set; } = new();
        public Dictionary<string, BlueprintEditorPort> OutPorts { get; private set; } = new();
        public BlueprintView View { get; }
        
        public event Action<string> ConnectedPort;
        public event Action<string> DisconnectedPort;
        
        public BlueprintEditorToken(BlueprintView view, BlueprintDesignNode node) : base(null, null)
        {
            View = view;
            Model = node;
            
            var nameTuple = node.GetNodeName();
            var iconTuple = node.GetNodeNameIcon();
            Texture2D text;
            if (iconTuple.Item1.EmptyOrNull())
            {
                text = null;
            }
            else
            {
                MethodInfo method = typeof(EditorGUIUtility).GetMethod(
                    "IconContent",
                    BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    new[] { typeof(string), typeof(bool) }, // Explicitly specify parameter types
                    null
                );
                text = ((GUIContent)method?.Invoke(null, new object[] { iconTuple.Item1, false }))?.image as Texture2D;
            }
            var s = text != null ? Sprite.Create(text, new Rect(0, 0, 16, 16), Vector2.zero) : Resources.Load<Sprite>($"BlueprintIcons/{iconTuple.Item1}");

            CreateTitle(nameTuple.Item1, nameTuple.Item2, s, iconTuple.Item2, iconTuple.Item3);
            style.width = 80;
            var top = this.Q<VisualElement>("top");
            top.style.alignSelf = Align.Stretch;
            top.style.justifyContent = Justify.SpaceBetween;
            
            CreateFlowInPorts();
            CreateFlowOutPorts();
            
            RefreshExpandedState();
        }
        
        private void CreateTitle(string newTitle, Color titleTint, Sprite titleIcon, Color titleIconTint, string iconTooltip)
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
                    tooltip = iconTooltip,
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

        public void InvalidateName()
        {
        }

        public void InvalidateType()
        {
            
        }
    }
    
    public class BlueprintEditorNode : Node, IBlueprintEditorNode
    {
        public BlueprintDesignNode Model { get; protected set; }
        
        public Dictionary<string, BlueprintEditorPort> InPorts { get; private set; } = new();
        public Dictionary<string, BlueprintEditorPort> OutPorts { get; private set; } = new();
        
        public BlueprintView View { get; protected set; }
        
        private List<FieldInfo> _nodeContentData;

        public event Action<string> ConnectedPort;
        public event Action<string> DisconnectedPort;

        public BlueprintEditorNode(BlueprintView view, BlueprintDesignNode node)
        {
            View = view;
            Model = node;

            m_CollapseButton.RemoveFromHierarchy();
            StyleNode();
            var nameTuple = node.GetNodeName();
            var iconTuple = node.GetNodeNameIcon();
            Texture2D text;
            if (iconTuple.Item1.EmptyOrNull())
            {
                text = null;
            }
            else
            {
                MethodInfo method = typeof(EditorGUIUtility).GetMethod(
                    "IconContent",
                    BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    new[] { typeof(string), typeof(bool) }, // Explicitly specify parameter types
                    null
                );
                text = ((GUIContent)method?.Invoke(null, new object[] { iconTuple.Item1, false }))?.image as Texture2D;
            }

            var s = text != null ? Sprite.Create(text, new Rect(0, 0, 16, 16), Vector2.zero) : Resources.Load<Sprite>($"BlueprintIcons/{iconTuple.Item1}");
            CreateTitle(nameTuple.Item1, nameTuple.Item2, s, iconTuple.Item2, iconTuple.Item3);

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
        
        private void CreateTitle(string newTitle, Color titleTint, Sprite titleIcon, Color titleIconTint, string iconTooltip)
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
                    tooltip = iconTooltip,
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

        public void InvalidateName()
        {
            var nameTuple = Model.GetNodeName();
            var iconTuple = Model.GetNodeNameIcon();
            Texture2D text;
            if (iconTuple.Item1.EmptyOrNull())
            {
                text = null;
            }
            else
            {
                MethodInfo method = typeof(EditorGUIUtility).GetMethod(
                    "IconContent",
                    BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    new[] { typeof(string), typeof(bool) }, // Explicitly specify parameter types
                    null
                );
                text = ((GUIContent)method?.Invoke(null, new object[] { iconTuple.Item1, false }))?.image as Texture2D;
            }
            var s = text != null ? Sprite.Create(text, new Rect(0, 0, 16, 16), Vector2.zero) : Resources.Load<Sprite>($"BlueprintIcons/{iconTuple.Item1}");
            CreateTitle(nameTuple.Item1, nameTuple.Item2, s, iconTuple.Item2, iconTuple.Item3);
            
            if (Model.Type == typeof(EntryNodeType))
            {
                List<string> removed = new();
                List<BlueprintEditorPort> updated = new();
                foreach (var ports in OutPorts)
                {
                    if (ports.Value.Pin.PortName != ports.Key)
                    {
                        removed.Add(ports.Key);
                        updated.Add(ports.Value);
                    }
                }

                foreach (var r in removed)
                {
                    OutPorts.Remove(r);
                }

                foreach (var add in updated)
                {
                    add.portName = add.Pin.DisplayName;
                    OutPorts.Add(add.Pin.PortName, add);
                }
            }
            
            if (Model.Type == typeof(ReturnNodeType))
            {
                List<string> removed = new();
                List<BlueprintEditorPort> updated = new();
                foreach (var ports in InPorts)
                {
                    if (ports.Value.Pin.PortName != ports.Key)
                    {
                        removed.Add(ports.Key);
                        updated.Add(ports.Value);
                    }
                }

                foreach (var r in removed)
                {
                    InPorts.Remove(r);
                }

                foreach (var add in updated)
                {
                    add.portName = add.Pin.DisplayName;
                    InPorts.Add(add.Pin.PortName, add);
                }
            }
        }

        public void InvalidateType()
        {
            if (Model.Type == typeof(EntryNodeType))
            {
                foreach (var ports in OutPorts.Values)
                {
                    ports.UpdateType();
                }
            }

            if (Model.Type == typeof(ReturnNodeType))
            {
                foreach (var ports in InPorts.Values)
                {
                    ports.UpdateType();
                }
            }
            
            if (Model.Type == typeof(TemporaryDataGetterNodeType))
            {
                foreach (var ports in OutPorts.Values)
                {
                    ports.UpdateType();
                }
            }
            
            if (Model.Type == typeof(TemporaryDataSetterNodeType))
            {
                foreach (var ports in InPorts.Values)
                {
                    ports.UpdateType();
                }
                foreach (var ports in OutPorts.Values)
                {
                    ports.UpdateType();
                }
            }
        }

        #region - Helpers -

        

        #endregion
    }
}
