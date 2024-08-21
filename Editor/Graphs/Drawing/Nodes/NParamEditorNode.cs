using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Graphs;
using Vapor.Inspector;
using VaporEditor.Inspector;
using NodeModel = Vapor.Graphs.NodeModel;

namespace VaporEditor.Graphs
{
    public class NParamEditorNode : GraphToolsNode<NodeModel>
    {
        private static readonly StyleSheet s_PortColors = Resources.Load<StyleSheet>("Styles/PortColors");

        private List<(PortInAttribute portAtr, Type[] portTypes)> _inPortData;
        private List<(PortOutAttribute portAtr, Type[] portTypes)> _outPortData;
        private List<FieldInfo> _nodeContentData;
        private Dictionary<int, FieldInfo> _portContentData;

        public NParamEditorNode(GraphEditorView view, NodeModel node)
        {
            View = view;
            Node = node;
            FindParams();

            m_CollapseButton.RemoveFromHierarchy();
            StyleNode();
            CreateTitle(node.Name);

            CreateAdditionalContent(mainContainer.Q("contents"));

            //CreateFlowInPort();
            //CreateFlowOutPort();
            CreateFlowInPorts();
            CreateFlowOutPorts();

            RefreshExpandedState();
        }

        private void FindParams()
        {
            // Get all fields of the class
            var fields = Node.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);


            // Loop through each field and check if it has the MathNodeParamAttribute
            _inPortData = new();
            _outPortData = new();
            _nodeContentData = new();
            _portContentData = new();
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
                else if (field.IsDefined(typeof(NodeContentAttribute)))
                {
                    _nodeContentData.Add(field);
                }
                else if (field.IsDefined(typeof(PortContentAttribute)))
                {
                    var atr = field.GetCustomAttribute<PortContentAttribute>();
                    _portContentData.Add(atr.PortInIndex, field);
                }
            }

            _inPortData.Sort((l, r) => l.portAtr.PortIndex.CompareTo(r.portAtr.PortIndex));
            _outPortData.Sort((l, r) => l.portAtr.PortIndex.CompareTo(r.portAtr.PortIndex));
        }

        private void StyleNode()
        {
            var nodeType = Node.GetType();
            var width = nodeType.GetCustomAttribute<NodeWidthAttribute>();
            if (width != null)
                style.minWidth = width.MinWidth;
        }

        private void CreateTitle(string title)
        {
            var overrideAtr = Node.GetType().GetCustomAttribute<NodeNameAttribute>();
            if (overrideAtr != null)
                title = overrideAtr.Name;

            this.title = title;
            var label = titleContainer.Q<Label>();
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginRight = 6;
            label.style.marginLeft = 6;
        }

        private void CreateFlowInPort()
        {
            InPorts = new(_inPortData.Count);
            foreach (var (portAtr, portTypes) in _inPortData)
            {
                var type = portTypes[0];
                if(portTypes.Length > 1)
                {
                    type = typeof(object);
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
                port.styleSheets.Add(s_PortColors);
                InPorts.Add(portAtr.Name, port);
                inputContainer.Add(port);

                if (_portContentData.TryGetValue(portAtr.PortIndex, out var field))
                {
                    var ve = DrawerUtility.DrawVaporFieldFromType(Node, field, true);
                    inputContainer.Add(ve);
                    ConnectedPort += (p) =>
                    {                        
                        if (p != portAtr.PortIndex.ToString())
                        {
                            return;
                        }
                        ve.Hide();
                    };
                    DisconnectedPort += (p) =>
                    {
                        if (p != portAtr.PortIndex.ToString())
                        {
                            return;
                        }
                        ve.Show();
                    };
                }
            }
            _inPortData.Clear();
        }

        private void CreateFlowOutPort()
        {
            OutPorts = new(_outPortData.Count);
            foreach (var (portAtr, portTypes) in _outPortData)
            {
                var type = portTypes[0];
                if (portTypes.Length > 1)
                {
                    type = typeof(object);
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
                if (!portAtr.Required)
                {
                    port.AddToClassList("optionalPort");
                }
                port.portName = portAtr.Name;
                port.tooltip = "The flow output";
                port.styleSheets.Add(s_PortColors);
                OutPorts.Add(portAtr.Name, port);
                outputContainer.Add(port);
            }
            _outPortData.Clear();
        }

        private void CreateFlowInPorts()
        {
            InPorts = new(Node.InSlots.Count);
            foreach (var slot in Node.InSlots.Values)
            {
                var type = slot.Type;
                //if (portTypes.Length > 1)
                //{
                //    type = typeof(object);
                //}
                var capacity = slot.AllowMultiple ? Port.Capacity.Multi : Port.Capacity.Single;
                var port = InstantiatePort(Orientation.Horizontal, Direction.Input, capacity, type);
                //if (portTypes.Length > 1)
                //{
                //    var set = new HashSet<Type>();
                //    for (int i = 0; i < portTypes.Length; i++)
                //    {
                //        set.Add(portTypes[i]);
                //    }
                //    port.userData = set;
                //}
                if (slot.IsOptional)
                {
                    port.AddToClassList("optionalPort");
                }
                port.userData = slot;
                port.portName = slot.DisplayName;
                port.tooltip = "The flow input";
                port.styleSheets.Add(s_PortColors);
                InPorts.Add(slot.UniqueName, port);
                inputContainer.Add(port);

                if (slot.HasContent)
                {
                    var ve = DrawerUtility.DrawVaporFieldFromType(slot, slot.ContentType, slot.GetContentFieldInfo(), true);
                    inputContainer.Add(ve);
                    ConnectedPort += (p) =>
                    {
                        if (p != slot.UniqueName)
                        {
                            return;
                        }
                        ve.Hide();
                    };
                    DisconnectedPort += (p) =>
                    {
                        if (p != slot.UniqueName)
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
            OutPorts = new(Node.OutSlots.Count);
            foreach (var slot in Node.OutSlots.Values)
            {
                var type = slot.Type;
                //if (portTypes.Length > 1)
                //{
                //    type = typeof(object);
                //}
                var capacity = slot.AllowMultiple ? Port.Capacity.Multi : Port.Capacity.Single;
                var port = InstantiatePort(Orientation.Horizontal, Direction.Output, capacity, type);
                //if (portTypes.Length > 1)
                //{
                //    var set = new HashSet<Type>();
                //    for (int i = 0; i < portTypes.Length; i++)
                //    {
                //        set.Add(portTypes[i]);
                //    }
                //    port.userData = set;
                //}
                if (slot.IsOptional)
                {
                    port.AddToClassList("optionalPort");
                }
                port.userData = slot;
                port.portName = slot.DisplayName;
                port.tooltip = "The flow output";
                port.styleSheets.Add(s_PortColors);
                OutPorts.Add(slot.UniqueName, port);
                outputContainer.Add(port);
            }
            _outPortData.Clear();
        }

        protected virtual void CreateAdditionalContent(VisualElement content)
        {
            if (_nodeContentData.Count > 0)
            {
                var foldout = new StyledFoldout("Content");
                foreach (var field in _nodeContentData)
                {
                    var ve = DrawerUtility.DrawVaporFieldFromType(Node, field, true);
                    foldout.Add(ve);
                }
                content.Add(foldout);
            }
        }
    }
}
