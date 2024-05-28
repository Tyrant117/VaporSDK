using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.GraphTools;
using Object = UnityEngine.Object;

namespace VaporEditor.GraphTools
{
    public abstract class GraphEditorView<T> : VisualElement, IDisposable where T : ScriptableObject
    {
        public GraphToolsEditWindow<T> Window { get; set; }

        public GraphView GraphView { get; set; }

        public T Graph { get; set; }
        public List<NodeSo> Nodes { get; set; }

        public string AssetName { get; set; }

        public Action SaveRequested { get; set; }
        public Action ShowInProjectRequested { get; set; }
        public List<IGraphToolsNode> EditorNodes { get; } = new();

        public GraphEditorView(GraphToolsEditWindow<T> editWindow, T graphObject, List<NodeSo> nodeObjects, string graphName)
        {
            Debug.Log("Nodes Set");
            Window = editWindow;
            Graph = graphObject;
            Nodes = nodeObjects;
            AssetName = graphName;

            name = "GraphEditorView";
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/GraphToolsEditorView"));

            CreateToolbar();
            CreateView();
        }

        private void CreateToolbar()
        {
            var toolbar = new IMGUIContainer(() =>
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                if (GUILayout.Button("Save Asset", EditorStyles.toolbarButton))
                {
                    SaveRequested?.Invoke();
                }
                if (GUILayout.Button("Show In Project", EditorStyles.toolbarButton))
                {
                    ShowInProjectRequested?.Invoke();
                }

                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();
                //m_UserViewSettings.isBlackboardVisible = GUILayout.Toggle(m_UserViewSettings.isBlackboardVisible, "Blackboard", EditorStyles.toolbarButton);

                //m_UserViewSettings.isInspectorVisible = GUILayout.Toggle(m_UserViewSettings.isInspectorVisible, "Graph Inspector", EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                {
                    //UserViewSettingsChangeCheck(newColorIndex);
                }
                GUILayout.EndHorizontal();
            });
            Add(toolbar);
        }

        protected abstract void CreateView();

        public virtual void Dispose()
        {
            if (GraphView != null)
            {
                SaveRequested = null;
                ShowInProjectRequested = null;
                foreach (var edge in GraphView.Query<Edge>().ToList())
                {
                    edge.output = null;
                    edge.input = null;
                }

                GraphView.nodeCreationRequest = null;
                GraphView = null;
                Debug.Log("Nodes Disposed");
                Nodes = null;
            }
        }

        protected virtual GraphViewChange GraphViewChanged(GraphViewChange graphViewChange)
        {
            _CreateEdges(graphViewChange);
            _MoveElements(graphViewChange);
            _RemoveElements(graphViewChange);

            //UpdateEdgeColors(nodesToUpdate);

            Window.MarkDirty();
            return graphViewChange;

            void _CreateEdges(GraphViewChange graphViewChange)
            {
                if (graphViewChange.edgesToCreate != null)
                {
                    foreach (var edge in graphViewChange.edgesToCreate)
                    {
                        AddEdge(edge);
                    }
                    //graphViewChange.edgesToCreate.Clear();
                }
            }

            void _MoveElements(GraphViewChange graphViewChange)
            {
                if (graphViewChange.movedElements != null)
                {
                    foreach (var element in graphViewChange.movedElements)
                    {
                        if (element is IGraphToolsNode node)
                        {
                            node.GetNode().Position = element.parent.ChangeCoordinatesTo(GraphView.contentViewContainer, element.GetPosition());
                        }

                        if (element is StickyNote stickyNote)
                        {
                            //SetStickyNotePosition(stickyNote);
                        }
                    }
                }
            }

            void _RemoveElements(GraphViewChange graphViewChange)
            {
                if (graphViewChange.elementsToRemove != null)
                {
                    foreach (var edge in graphViewChange.elementsToRemove.OfType<Edge>())
                    {
                        if (edge.input != null && edge.output != null && edge.input.node is IGraphToolsNode inN && edge.output.node is IGraphToolsNode outN)
                        {
                            if (inN.GetNode() is NodeSo parent && outN.GetNode() is NodeSo child)
                            {
                                // Remove the InEdge link from the parent.
                                var idx = parent.InEdges.FindIndex(edge => edge.GuidMatches(child.GetGuid()));
                                if (idx != -1)
                                {
                                    parent.InEdges.RemoveAt(idx);
                                }

                                // Remove the OutEdge Link From The Child
                                idx = child.OutEdges.FindIndex(edge => edge.GuidMatches(parent.GetGuid()));
                                if (idx != -1)
                                {
                                    child.OutEdges.RemoveAt(idx);
                                }
                            }
                        }
                    }

                    foreach (var node in graphViewChange.elementsToRemove.OfType<IGraphToolsNode>())
                    {
                        Debug.Log("Removing Node" + node);
                        EditorNodes.Remove(node);
                        Nodes.Remove(node.GetNode());
                    }
                }
            }
        }

        public abstract void AddNode(ScriptableObject node);

        protected virtual void AddEdges()
        {
            foreach (var root in EditorNodes)
            {
                var edges = root.GetNode().InEdges;
                foreach (var edge in edges)
                {
                    var child = EditorNodes.FirstOrDefault(iNode => edge.GuidMatches(iNode.GetNode().GetGuid()));
                    if (child != null)
                    {
                        var e = child.OutPorts[edge.OutPortIndex].ConnectTo(root.InPorts[edge.InPortIndex]);
                        GraphView.AddElement(e);
                    }
                }
            }
        }

        protected virtual void AddEdge(Edge edge)
        {
            var inNode = (IGraphToolsNode)edge.input.node;
            int inPortIndex = inNode.InPorts.IndexOf(edge.input);

            var outNode = (IGraphToolsNode)edge.output.node;
            int outPortIndex = outNode.OutPorts.IndexOf(edge.output);

            if (inNode != null && outNode != null)
            {
                var inNodeSo = inNode.GetNode();
                var outNodeSo = outNode.GetNode();
                inNodeSo.InEdges.Add(new EdgeConnection(inPortIndex, outPortIndex, outNodeSo.GetGuid()));
                outNodeSo.OutEdges.Add(new EdgeConnection(inPortIndex, outPortIndex, inNodeSo.GetGuid()));
            }
        }        
    }
}
