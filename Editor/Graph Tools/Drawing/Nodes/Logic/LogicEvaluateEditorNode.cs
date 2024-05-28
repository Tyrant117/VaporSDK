using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.GraphTools;

namespace VaporEditor.GraphTools
{
    public class LogicEvaluateEditorNode<T> : GraphToolsTokenNode<T, LogicEvaluateNodeSo> where T : ScriptableObject
    {
        private static readonly StyleSheet s_PortColors = Resources.Load<StyleSheet>("Styles/PortColors");

        public LogicEvaluateEditorNode(GraphEditorView<T> view, LogicEvaluateNodeSo node) : base(null, null)
        {
            View = view;
            Node = node;

            name = "PropertyTokenView";
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/GraphToolsTokenView"));

            CreateTitle("Evaluate()");

            CreateInOutPort();

            RefreshExpandedState();
        }

        private void CreateTitle(string title)
        {
            this.title = title;
            var titleLabel = this.Q<Label>("title-label");
            titleLabel.style.marginTop = 6;
            titleLabel.style.marginBottom = 6;
            titleLabel.style.marginLeft = 6;
            titleLabel.style.marginRight = 6;
        }

        private void CreateInOutPort()
        {
            var inPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
            //_inPort.portName = "In";
            inPort.tooltip = "The logic evaluation";
            inPort.Q("connector").pickingMode = PickingMode.Position;
            inPort.Q<Label>().style.display = DisplayStyle.None;
            inPort.styleSheets.Add(s_PortColors);
            InPorts.Add(inPort);
            inputContainer.Add(inPort);

            if (inPort != null)
            {
                var pill = this.Q<Pill>("pill");
                pill.left = inPort;
            }
        }
    }
}
