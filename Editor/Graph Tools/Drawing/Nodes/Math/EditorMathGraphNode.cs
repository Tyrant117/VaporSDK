using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.GraphTools;

namespace VaporEditor.GraphTools
{
    public class EditorMathGraphNode<T> : GraphToolsNode<T, MathGraphNodeSo> where T : ScriptableObject
    {
        private static readonly StyleSheet _portColors = Resources.Load<StyleSheet>("Styles/PortColors");

        public EditorMathGraphNode(GraphEditorView<T> view, MathGraphNodeSo node)
        {
            View = view;
            Node = node;

            m_CollapseButton.RemoveFromHierarchy();
            CreateTitle(node.Name);

            CreateFlowInPort();
            CreateFlowOutPort();

            RefreshExpandedState();
        }

        private void CreateTitle(string title)
        {
            this.title = title;
            var label = titleContainer.Q<Label>();
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginRight = 6;
            label.style.marginLeft = 6;
        }

        private void CreateFlowInPort()
        {
            InPorts = new(Node.Graph.ExposedProperties.Count);
            foreach (var exposedProp in Node.Graph.ExposedProperties)
            {
                var port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(DynamicValuePort));
                var set = new HashSet<Type>
                {
                    typeof(bool),
                    typeof(int),
                    typeof(float)
                };
                port.userData = set;
                port.portName = exposedProp.ValueName;
                port.tooltip = "The flow input";
                port.styleSheets.Add(_portColors);
                InPorts.Add(port);
                inputContainer.Add(port);
            }
        }

        private void CreateFlowOutPort()
        {
            var outPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            outPort.portName = "Out";
            outPort.tooltip = "The graph evaluation";
            //_inPort.Q<Label>().style.display = DisplayStyle.None;
            OutPorts.Add(outPort);
            outputContainer.Add(outPort);
        }
    }
}
