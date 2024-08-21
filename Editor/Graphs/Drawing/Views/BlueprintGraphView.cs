using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Inspector;
using VaporEditor.Inspector;

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

        #region - Inspector -
        public VisualElement DrawElement()
        {
            var element = GraphObject.Graph.ElementToDraw(out var fields);

            var ve = new VisualElement();
            foreach (var field in fields)
            {
                Debug.Log(IsList(field.FieldType));
                if (IsList(field.FieldType))
                {
                    Debug.Log("Drawing Array");
                    ve.Add(new IntegerField());
                    ve.Add(new ListView((System.Collections.IList)field.GetValue(element)));
                }
                else
                {
                    Debug.Log("Drawing Single");
                    ve.Add(DrawerUtility.DrawVaporFieldFromType(element, field, true));
                }
            }

            return ve;
        }

        private static bool IsList(Type type)
        {
            // Check if the type is a generic type and if it matches List<T>
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }
        #endregion
    }
}
