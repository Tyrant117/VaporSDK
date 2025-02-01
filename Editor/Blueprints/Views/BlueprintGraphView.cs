using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Vapor.Blueprints;
using ISelectable = UnityEditor.Experimental.GraphView.ISelectable;

namespace VaporEditor.Blueprints
{
    public class BlueprintGraphView : GraphView
    {
        public BlueprintEditorView View { get; }
        public BlueprintGraphSo GraphObject { get; }

        public List<ISelectable> GetSelection => selection;

        public delegate void SelectionChanged(List<ISelectable> selection);
        public SelectionChanged OnSelectionChange;

        public BlueprintGraphView(BlueprintEditorView view, BlueprintGraphSo graphObject)
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
                    allPorts.AddRange(node.OutPorts.Values);
                }
                else
                {
                    allPorts.AddRange(node.InPorts.Values);
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
    }
}
