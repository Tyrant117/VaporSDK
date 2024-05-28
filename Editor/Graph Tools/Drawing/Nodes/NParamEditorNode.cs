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
    public class NParamEditorNode<T> : GraphToolsNode<T, NodeSo> where T : ScriptableObject
    {
        private static readonly StyleSheet _portColors = Resources.Load<StyleSheet>("Styles/PortColors");

        private List<(PortInAttribute portAtr, Type[] portTypes)> _inPortData;
        private List<(PortOutAttribute portAtr, Type[] portTypes)> _outPortData;

        public NParamEditorNode(GraphEditorView<T> view, NodeSo node)
        {
            View = view;
            Node = node;
            FindParams();

            m_CollapseButton.RemoveFromHierarchy();
            CreateTitle(node.Name);

            CreateAdditionalContent(mainContainer.Q("contents"));

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
                inputContainer.Add(port);
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
                if (!portAtr.Required)
                {
                    port.AddToClassList("optionalPort");
                }
                port.portName = portAtr.Name;
                port.tooltip = "The flow output";
                port.styleSheets.Add(_portColors);
                OutPorts.Add(port);
                outputContainer.Add(port);
            }
            _outPortData.Clear();
        }

        protected virtual void CreateAdditionalContent(VisualElement content)
        {

        } 
    }
}
