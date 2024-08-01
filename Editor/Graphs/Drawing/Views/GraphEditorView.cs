using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Graphs;
using Node = Vapor.Graphs.Node;

namespace VaporEditor.Graphs
{
    public class GraphEditorView : GraphView, IDisposable
    {
        public GraphEditorWindow Window { get; set; }
        public string AssetName { get; set; }

        private GraphSearchProvider _searchProvider;

        public GraphEditorView(List<string> searchIncludeFlags)
        {
            name = "GraphEditorView";
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/GraphToolsEditorView"));
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/GraphToolsGraphView"));

            style.flexGrow = 1;
            
            this.SetupZoom(0.05f, 8);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ClickSelector());
            CreateToolbar();

            _searchProvider = ScriptableObject.CreateInstance<GraphSearchProvider>();
            _searchProvider.SetupIncludes(searchIncludeFlags);
            _searchProvider.View = this;
            nodeCreationRequest = NodeCreationRequest;
        }

        private void NodeCreationRequest(NodeCreationContext context)
        {
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchProvider);
        }

        private void CreateToolbar()
        {
            var toolbar = new IMGUIContainer(() =>
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                if (GUILayout.Button("Save Asset", EditorStyles.toolbarButton))
                {
                    //SaveRequested?.Invoke();
                }
                if (GUILayout.Button("Show In Project", EditorStyles.toolbarButton))
                {
                    //ShowInProjectRequested?.Invoke();
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
            Insert(0, toolbar);
        }

        public void Dispose()
        {
            if (this != null)
            {
                //SaveRequested = null;
                //ShowInProjectRequested = null;
                //foreach (var edge in GraphView.Query<Edge>().ToList())
                //{
                //    edge.output = null;
                //    edge.input = null;
                //}

                //GraphView.nodeCreationRequest = null;
                //GraphView = null;
                //Debug.Log("Nodes Disposed");
                //Nodes = null;
            }
        }

        public void AddNode(Node node)
        {
            //if (AddSpecialNode(node))
            //    return;

            //if (ValueNodeDrawerHelper.AddNodes(node, this, GraphView, EditorNodes, Nodes))
            //    return;
            //if (MathNodeDrawerHelper.AddNodes(node, this, GraphView, EditorNodes, Nodes))
            //    return;
            //if (LogicNodeDrawerHelper.AddNodes(node, this, GraphView, EditorNodes, Nodes))
            //    return;
            //if (NParamNodeDrawerHelper.AddNodes(node, this, GraphView, EditorNodes, Nodes))
            //    return;
        }
    }
}
