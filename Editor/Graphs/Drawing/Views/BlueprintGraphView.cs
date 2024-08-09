using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Inspector;

namespace VaporEditor.Graphs
{
    public class BlueprintGraphView : GraphView, IDrawableElement, ISelectionProvider
    {
        public GraphEditorView View { get; }
        public GraphObject GraphObject { get; }

        public List<ISelectable> GetSelection => selection;

        public delegate void SelectionChanged(List<ISelectable> selection);
        public SelectionChanged OnSelectionChange;

        public BlueprintGraphView(GraphEditorView view, GraphObject graphObject)
        {
            View = view;
            GraphObject = graphObject;
        }

        #region - Nodes -
        public void CreateRedirectNode(Vector2 pos, Edge edgeTarget)
        {

        }
        #endregion

        #region - Ports -
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var allPorts = new List<Port>();
            var ports = new List<Port>();

            foreach (var node in View.EditorNodes)
            {
                if (startPort.direction == Direction.Input)
                {
                    allPorts.AddRange(node.OutPorts);
                }
                else
                {
                    allPorts.AddRange(node.InPorts);
                }
            }

            foreach (var p in allPorts)
            {
                if (p == startPort) { continue; }
                if (p.node == startPort.node) { continue; }
                if (p.portType == startPort.portType)
                {
                    ports.Add(p);
                }
                else if (p.userData is HashSet<Type> dynamicPorts && dynamicPorts.Contains(startPort.portType))
                {
                    ports.Add(p);
                }
                else if (startPort.portType == typeof(object) && startPort.userData is HashSet<Type> dynamicStartPort && dynamicStartPort.Contains(p.portType))
                {
                    ports.Add(p);
                }
            }

            return ports;
        }
        #endregion

        #region - Inspector -
        public VisualElement DrawElement()
        {
            return new VisualElement();
        }
        #endregion
    }
}
