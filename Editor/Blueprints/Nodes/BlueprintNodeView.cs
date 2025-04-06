using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Blueprints;
using Vapor.Inspector;
using VaporEditor.Inspector;

// ReSharper disable ClassNeverInstantiated.Global

namespace VaporEditor.Blueprints
{

    public class BlueprintRedirectNodeView : Node, IBlueprintNodeView
    {
        public NodeModelBase Controller { get; }
        public Dictionary<string, BlueprintPortView> InPorts { get; private set; } = new();
        public Dictionary<string, BlueprintPortView> OutPorts { get; private set; } = new();
        public BlueprintView View { get; }
        
        public event Action<string> ConnectedPort;
        public event Action<string> DisconnectedPort;

        public BlueprintRedirectNodeView(BlueprintView view, NodeModelBase nodeController)
        {
            View = view;
            Controller = nodeController;
            
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/RedirectNode"));
            
            CreateFlowInPorts();
            CreateFlowOutPorts();
        }

        private void CreateFlowInPorts()
        {
            if (Controller.InputPins == null)
            {
                return;
            }
            InPorts = new(Controller.InputPins.Count);
            foreach (var slot in Controller.InputPins.Values)
            {
                var portContainer = BlueprintPortView.Create(this, slot, out var port);
                if (slot.IsOptional)
                {
                    port.AddToClassList("optionalPort");
                }
                InPorts.Add(slot.PortName, port);
                port.Q<Label>().Hide();
                inputContainer.Add(portContainer);
                port.DrawnField.Hide();
            }
        }

        private void CreateFlowOutPorts()
        {
            if (Controller.OutputPins == null)
            {
                return;
            }
            
            OutPorts = new(Controller.OutputPins.Count);
            foreach (var slot in Controller.OutputPins.Values)
            {
                var portContainer = BlueprintPortView.Create(this, slot, out var port);
                if (slot.IsOptional)
                {
                    port.AddToClassList("optionalPort");
                }
                OutPorts.Add(slot.PortName, port);
                port.Q<Label>().Hide();
                outputContainer.Add(portContainer);
                port.DrawnField.Hide();
            }
        }
        
        public void OnConnectedInputEdge(BlueprintWireReference wire, bool shouldModifyDataModel)
        {
            Debug.Log($"Connected {wire}");
            ConnectedPort?.Invoke(wire.RightSidePin.PinName);
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
    
    public class BlueprintToken : TokenNode, IBlueprintNodeView
    {
        public NodeModelBase Controller { get; }
        public Dictionary<string, BlueprintPortView> InPorts { get; private set; } = new();
        public Dictionary<string, BlueprintPortView> OutPorts { get; private set; } = new();
        public BlueprintView View { get; }
        
        public event Action<string> ConnectedPort;
        public event Action<string> DisconnectedPort;
        
        public BlueprintToken(BlueprintView view, NodeModelBase nodeController) : base(null, null)
        {
            View = view;
            Controller = nodeController;
            
            var nameTuple = nodeController.GetNodeName();
            var iconTuple = nodeController.GetNodeNameIcon();
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
            if (Controller.InputPins == null)
            {
                return;
            }
            InPorts = new(Controller.InputPins.Count);
            foreach (var slot in Controller.InputPins.Values)
            {
                var portContainer = BlueprintPortView.Create(this, slot, out var port);
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
            if (Controller.OutputPins == null)
            {
                return;
            }
            OutPorts = new(Controller.InputPins.Count);
            foreach (var slot in Controller.OutputPins.Values)
            {
                var portContainer = BlueprintPortView.Create(this, slot, out var port);
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

        public void OnConnectedInputEdge(BlueprintWireReference wire, bool shouldModifyDataModel)
        {
            Debug.Log($"Connected {wire}");
            ConnectedPort?.Invoke(wire.RightSidePin.PinName);
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
    
    public class BlueprintNodeView : Node, IBlueprintNodeView
    {
        public NodeModelBase Controller { get; protected set; }
        
        public Dictionary<string, BlueprintPortView> InPorts { get; private set; } = new();
        public Dictionary<string, BlueprintPortView> OutPorts { get; private set; } = new();
        
        public BlueprintView View { get; protected set; }
        
        private List<FieldInfo> _nodeContentData;

        public event Action<BlueprintWireReference, bool> ConnectedPort;
        public event Action<string> DisconnectedPort;

        public BlueprintNodeView(BlueprintView view, NodeModelBase nodeController)
        {
            View = view;
            Controller = nodeController;

            m_CollapseButton.RemoveFromHierarchy();
            StyleNode();
            var nameTuple = nodeController.GetNodeName();
            var iconTuple = nodeController.GetNodeNameIcon();
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

            // if (nodeController.Model.NodeType == NodeType.Method && nodeController.ModelAs<MethodNodeModel>().MethodInfo.IsGenericMethod)
            // {
            //     const string n = "Gen_";
            //     int i = 0;
            //     foreach (var genM in nodeController.ModelAs<MethodNodeModel>().MethodInfo.GetGenericArguments())
            //     {
            //         int idx = i;
            //         var ts = new TypeSelectorField($"{n}{idx}", typeof(bool));
            //         ts.TypeChanged += type =>
            //         {
            //             Debug.Log("Update Type " + type.Name + " " + idx);
            //             if (nodeController.GenericArgumentPortMap.TryGetValue(idx, out var port))
            //             {
            //                 port.Type = type;
            //                 OutPorts[port.PortName].UpdateType();
            //                 Debug.Log("Update Type " + port.PortName);
            //             }
            //         };
            //         Add(ts);
            //         i++;
            //     }
            // }

            RefreshExpandedState();

            ConnectedPort += OnConnectedPort;
            DisconnectedPort += OnDisconnectedPort;
        }

        protected virtual void OnConnectedPort(BlueprintWireReference wire, bool shouldModifyDataModel)
        {
            if (Controller.NodeType == NodeType.Switch && wire.RightSidePin.PinName == PinNames.VALUE_IN)
            {
                if(shouldModifyDataModel && Controller.OnEnumChanged())
                {
                    ClearOutputPins();
                }
            }
        }
        
        protected virtual void OnDisconnectedPort(string portName)
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

            if (titleIcon)
            {
                if (titleContainer[0] is Image titleImage)
                {
                    titleImage.tooltip = iconTooltip;
                    titleImage.sprite = titleIcon;
                    titleImage.tintColor = titleIconTint;
                }
                else
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
        }
        
        private void CreateFlowInPorts()
        {
            if (Controller.InputPins == null)
            {
                return;
            }
            InPorts = new Dictionary<string, BlueprintPortView>(Controller.InputPins.Count);
            foreach (var pin in Controller.InputPins.Values)
            {
                CreateInputPin(pin);
            }
        }

        protected void CreateInputPin(BlueprintPin pin)
        {
            var portContainer = BlueprintPortView.Create(this, pin, out var port);
            if (pin.IsOptional)
            {
                port.AddToClassList("optionalPort");
            }
            InPorts.Add(pin.PortName, port);
            inputContainer.Add(portContainer);
                
            // inputContainer.Add(port.DrawnField);
            if (pin.IsGenericPin)
            {
                var ts = GetDrawnFieldForInPort<TypeSelectorField>(pin.PortName);
                ts.userData = port;
                ts.TypeChanged += GenericTypePinChanged;
            }
        }
        
        protected void RemoveInputPin(string pinName)
        {
            if (!InPorts.Remove(pinName, out var pin))
            {
                return;
            }

            pin.DisconnectAll();
            // pin.DrawnField?.RemoveFromHierarchy();
            pin.parent.RemoveFromHierarchy();
        }
        
        private void CreateFlowOutPorts()
        {
            if (Controller.OutputPins == null)
            {
                return;
            }
            OutPorts = new Dictionary<string, BlueprintPortView>(Controller.OutputPins.Count);
            foreach (var pin in Controller.OutputPins.Values)
            {
                CreateOutputPin(pin);
            }
        }

        protected void CreateOutputPin(BlueprintPin pin)
        {
            var portContainer = BlueprintPortView.Create(this, pin, out var port);
            if (pin.IsOptional)
            {
                port.AddToClassList("optionalPort");
            }
                
            OutPorts.Add(pin.PortName, port);
            outputContainer.Add(portContainer);

            if (pin.IsGenericPin)
            {
                var ts = GetDrawnFieldForOutPort<TypeSelectorField>(pin.PortName);
                ts.userData = port;
                ts.TypeChanged += GenericTypePinChanged;
            }
        }

        protected void RemoveOutputPin(string pinName)
        {
            if (!OutPorts.Remove(pinName, out var pin))
            {
                return;
            }

            pin.DisconnectAll();
            pin.parent.RemoveFromHierarchy();
        }

        public void OnConnectedInputEdge(BlueprintWireReference wire, bool shouldModifyDataModel)
        {
            Debug.Log($"Connected {wire}");
            ConnectedPort?.Invoke(wire, shouldModifyDataModel);
        }

        public void OnDisconnectedInputEdge(string portName)
        {
            Debug.Log($"Disconnected {portName}");
            DisconnectedPort?.Invoke(portName);
        }

        public void InvalidateName()
        {
            var nameTuple = Controller.GetNodeName();
            var iconTuple = Controller.GetNodeNameIcon();
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

            var s = text ? Sprite.Create(text, new Rect(0, 0, 16, 16), Vector2.zero) : Resources.Load<Sprite>($"BlueprintIcons/{iconTuple.Item1}");
            CreateTitle(nameTuple.Item1, nameTuple.Item2, s, iconTuple.Item2, iconTuple.Item3);

            List<string> removed = new();
            List<BlueprintPortView> updated = new();
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

            removed.Clear();
            updated.Clear();
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

        public void InvalidateType()
        {
            if (Controller.NodeType == NodeType.Entry)
            {
                foreach (var ports in OutPorts.Values)
                {
                    ports.UpdateType();
                }
            }

            if (Controller.NodeType == NodeType.Return)
            {
                foreach (var ports in InPorts.Values)
                {
                    ports.UpdateType();
                }
            }
            
            if (Controller.NodeType == NodeType.MemberAccess)
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

        public void InvalidateTopology()
        {
            ClearInputPins();
            ClearOutputPins();
        }

        protected void ClearInputPins(bool rebuild = true)
        {
            foreach (var port in InPorts.Values)
            {
                port.DisconnectAll();
            }
            InPorts.Clear();
            
            inputContainer.DisconnectChildren();

            if (rebuild)
            {
                CreateFlowInPorts();
            }
        }

        protected void ClearOutputPins(bool rebuild = true)
        {
            foreach (var port in OutPorts.Values)
            {
                port.DisconnectAll();
            }
            OutPorts.Clear();

            
            outputContainer.DisconnectChildren();

            if (rebuild)
            {
                CreateFlowOutPorts();
            }
        }

        #region - Helpers -
        protected T GetDrawnFieldForInPort<T>(string inPortName) where T : VisualElement
        {
            var typePort = InPorts[inPortName];
            return typePort.DrawnField.Q<T>();
        }
        protected T GetDrawnFieldForOutPort<T>(string outPortName) where T : VisualElement
        {
            var typePort = OutPorts[outPortName];
            return typePort.DrawnField.Q<T>();
        }


        private void GenericTypePinChanged(VisualElement sender, Type newType, Type oldType)
        {
            if (sender.userData is not BlueprintPortView view)
            {
                return;
            }

            view.Pin.Type = newType;
            view.UpdateType();

            if (InPorts.ContainsKey(view.Pin.PortName))
            {
                var ts = GetDrawnFieldForInPort<TypeSelectorField>(view.Pin.PortName);
                ts.userData = view;
                ts.TypeChanged += GenericTypePinChanged;
            }
            else
            {
                var ts = GetDrawnFieldForOutPort<TypeSelectorField>(view.Pin.PortName);
                ts.userData = view;
                ts.TypeChanged += GenericTypePinChanged;
            }
            
        }
        #endregion
    }

    public class BlueprintCastNodeView : BlueprintNodeView
    {
        
        public BlueprintCastNodeView(BlueprintView view, NodeModelBase nodeController) : base(view, nodeController)
        {
            GetDrawnFieldForOutPort<TypeSelectorField>(PinNames.AS_OUT).TypeChanged += OnTypeChanged;
        }

        private void OnTypeChanged(VisualElement sender, Type newType, Type oldType)
        {
            title = $"Cast<{GetDrawnFieldForOutPort<TypeSelectorField>(PinNames.AS_OUT).LabelName}>";
            OutPorts[PinNames.AS_OUT].Pin.Type = newType;
            OutPorts[PinNames.AS_OUT].UpdateType();
            
            var ts = GetDrawnFieldForOutPort<TypeSelectorField>(PinNames.AS_OUT);
            ts.TypeChanged += OnTypeChanged;
        }
    }
    
    public class BlueprintSequenceNodeView : BlueprintNodeView
    {
        private SequenceNode sequenceNode;
        public BlueprintSequenceNodeView(BlueprintView view, SequenceNode nodeController) : base(view, nodeController)
        {
            sequenceNode = nodeController;
            titleContainer[0].style.paddingLeft = 28;
            titleContainer.Add(new Button(OnAddSequencePin)
            {
                tooltip = "Add Pin",
                text = "+",
                style =
                {
                    maxWidth = 16,
                    maxHeight = 16,
                    alignSelf = Align.Center,
                    marginLeft = 6,
                }
            });
        }

        private void OnAddSequencePin()
        {
            var i = Controller.OutputPins.Count;
            string formattedName = $"{PinNames.SEQUENCE_OUT}_{i}";
            var pin = new BlueprintPin(Controller, formattedName, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(formattedName);
            Controller.OutputPins.Add(formattedName, pin);

            sequenceNode.SetSequenceCount(Controller.OutputPins.Count);
            
            CreateOutputPin(pin);
        }

        public void DeletePin(string pinName)
        {
            if (!Controller.OutputPins.ContainsKey(pinName))
            {
                return;
            }

            Controller.OutputPins.Remove(pinName);
            sequenceNode.SetSequenceCount(Controller.OutputPins.Count);
                
            RemoveOutputPin(pinName);
            
            InvalidateName();
        }
    }

    public class BlueprintSwitchNodeView : BlueprintNodeView
    {
        public BlueprintSwitchNodeView(BlueprintView view, SwitchNode nodeController) : base(view, nodeController)
        {
        }

        public override void OnSelected()
        {
            base.OnSelected();
            if(View.selection.Count == 1)
            {
                View.Window.InspectorView.SetInspectorTarget(new BlueprintInspectorSwitchView(this));
            }
        }
    }
}
