using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.GraphTools;

namespace VaporEditor.GraphTools
{
    public class NParamEditorToken<T> : GraphToolsTokenNode<T, NodeSo> where T : ScriptableObject
    {
        private static readonly Texture2D s_ExposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");
        protected static readonly StyleSheet _portColors = Resources.Load<StyleSheet>("Styles/PortColors");

        private List<(PortInAttribute portAtr, Type[] portTypes)> _inPortData;
        private List<(PortOutAttribute portAtr, Type[] portTypes)> _outPortData;

        public NParamEditorToken(GraphEditorView<T> view, NodeSo node, EditorLabelVisualData visualData) : base(null, null)
        {
            View = view;
            Node = node;
            FindParams();

            name = "PropertyTokenView";
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/GraphToolsTokenView"));
            if (!string.IsNullOrEmpty(visualData.StyleSheet))
            {
                Debug.Log($"Adding StyleSheet: {visualData.StyleSheet}");
                styleSheets.Add(Resources.Load<StyleSheet>(visualData.StyleSheet));
                var border = this.Q<VisualElement>("node-border");
                border.name = visualData.BorderName;
                border.AddToClassList(visualData.ClassName);
            }

            if (string.IsNullOrEmpty(visualData.IconPath))
            {
                icon = s_ExposedIcon;
            }
            else
            {
                icon = Resources.Load<Texture2D>(visualData.IconPath);
                var ico = this.Q<Image>("icon");
                ico.style.width = 24;
                ico.style.height = 24;
                ico.style.marginTop = 4;
                ico.style.marginBottom = 4;
            }

            CreateTitle(node.Name);

            CreateFlowInPort();
            CreateFlowOutPort();

            RefreshExpandedState();
        }

        private void FindParams()
        {
            // Get all fields of the class
            var fields = Node.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // Loop through each field and check if it has the MathNodeParamAttribute
            _inPortData = new();
            _outPortData = new();
            foreach (FieldInfo field in fields)
            {
                if (field.IsDefined(typeof(PortInAttribute)))
                {
                    var atr = field.GetCustomAttribute<PortInAttribute>();
                    _inPortData.Add((atr, atr.PortTypes));
                }
                else if (field.IsDefined(typeof(PortOutAttribute)))
                {
                    var atr = field.GetCustomAttribute<PortOutAttribute>();
                    _outPortData.Add((atr, atr.PortTypes));
                }
            }

            _inPortData.Sort((l, r) => l.portAtr.PortIndex.CompareTo(r.portAtr.PortIndex));
            _outPortData.Sort((l, r) => l.portAtr.PortIndex.CompareTo(r.portAtr.PortIndex));
        }

        private void CreateTitle(string title)
        {
            var overrideAtr = Node.GetType().GetCustomAttribute<NodeNameAttribute>();
            if (overrideAtr != null)
                title = overrideAtr.Name;

            this.title = title;
            var titleLabel = this.Q<Label>("title-label");
            titleLabel.style.marginTop = 6;
            titleLabel.style.marginBottom = 6;
            titleLabel.style.marginLeft = 6;
            titleLabel.style.marginRight = 6;
        }

        private void CreateFlowInPort()
        {
            InPorts = new(_inPortData.Count);
            if (_inPortData.Count == 0)
                return;

            VisualElement inVe = inputContainer;
            bool inGroup = false;
            if (_inPortData.Count > 1)
            {
                inVe = new();
                inGroup = true;
            }
            foreach (var (portAtr, portTypes) in _inPortData)
            {
                var type = portTypes[0];
                if (portTypes.Length > 1)
                {
                    type = typeof(DynamicValuePort);
                }
                var capacity = portAtr.MultiPort ? Port.Capacity.Multi : Port.Capacity.Single;
                var port = InstantiatePort(Orientation.Horizontal, Direction.Input, capacity, type);
                if (portTypes.Length > 1)
                {
                    var set = new HashSet<Type>();
                    for (int i = 0; i < portTypes.Length; i++)
                    {
                        set.Add(portTypes[i]);
                    }
                    port.userData = set;
                }
                if (!portAtr.Required)
                {
                    port.AddToClassList("optionalPort");
                }
                port.portName = portAtr.Name;
                port.tooltip = "The flow input";
                port.styleSheets.Add(_portColors);
                InPorts.Add(port);
                inVe.Add(port);
            }
            if (inGroup)
            {
                inputContainer.Add(inVe);
                if (InPorts[0] != null)
                {
                    var pill = this.Q<Pill>("pill");
                    pill.left = inVe;
                }
            }
            else
            {
                if (InPorts[0] != null)
                {
                    var pill = this.Q<Pill>("pill");
                    pill.left = InPorts[0];
                }
            }
            _inPortData.Clear();
        }

        private void CreateFlowOutPort()
        {
            OutPorts = new(_outPortData.Count);
            if (_outPortData.Count == 0)
                return;

            VisualElement outVe = outputContainer;
            bool inGroup = false;
            if (_outPortData.Count > 1)
            {
                outVe = new();
                inGroup = true;
            }

            foreach (var (portAtr, portTypes) in _outPortData)
            {
                var type = portTypes[0];
                if (portTypes.Length > 1)
                {
                    type = typeof(DynamicValuePort);
                }
                var capacity = portAtr.MultiPort ? Port.Capacity.Multi : Port.Capacity.Single;
                var port = InstantiatePort(Orientation.Horizontal, Direction.Output, capacity, type);
                if (portTypes.Length > 1)
                {
                    var set = new HashSet<Type>();
                    for (int i = 0; i < portTypes.Length; i++)
                    {
                        set.Add(portTypes[i]);
                    }
                    port.userData = set;
                }
                port.portName = portAtr.Name;
                port.tooltip = "The flow output";
                port.styleSheets.Add(_portColors);
                OutPorts.Add(port);
                outVe.Add(port);
            }
            if (inGroup)
            {
                outputContainer.Add(outVe);
                if (OutPorts[0] != null)
                {
                    var pill = this.Q<Pill>("pill");
                    pill.right = outVe;
                }
            }
            else
            {
                if (OutPorts[0] != null)
                {
                    var pill = this.Q<Pill>("pill");
                    pill.right = OutPorts[0];
                }
            }
            _outPortData.Clear();
        }
    }
}
