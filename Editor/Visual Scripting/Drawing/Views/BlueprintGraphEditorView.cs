using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Inspector;
using Vapor.VisualScripting;
using static UnityEditor.Experimental.GraphView.Port;
using NodeModel = Vapor.VisualScripting.NodeModel;

namespace VaporEditor.VisualScripting
{
    #region Types
    [Serializable]
    public class FloatingWindowsLayout
    {
        public WindowDockingLayout previewLayout = new()
        {
            DockingTop = false,
            DockingLeft = false,
            VerticalOffset = 8,
            HorizontalOffset = 8
        };
    }

    [Serializable]
    public class UserViewSettings
    {
        public bool isBlackboardVisible = true;
        public bool isInspectorVisible = true;
    }

    public class EdgeConnectorListener : IEdgeConnectorListener
    {
        private readonly GraphObject m_Graph;
        private readonly SearchWindowProvider m_SearchWindowProvider;
        private readonly EditorWindow m_editorWindow;

        private GraphViewChange m_GraphViewChange;
        private List<Edge> m_EdgesToCreate;
        private List<GraphElement> m_EdgesToDelete;

        public EdgeConnectorListener(GraphObject graph, SearchWindowProvider searchWindowProvider, EditorWindow editorWindow)
        {
            m_Graph = graph;
            m_SearchWindowProvider = searchWindowProvider;
            m_editorWindow = editorWindow;

            m_EdgesToCreate = new List<Edge>();
            m_EdgesToDelete = new List<GraphElement>();
            m_GraphViewChange.edgesToCreate = m_EdgesToCreate;
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(EdgeConnectorListener), nameof(OnDropOutsidePort))} - Edge [{edge}] - Pos [{position}])");

            var draggedPort = (edge.output != null ? edge.output.edgeConnector.edgeDragHelper.draggedPort : null) ?? (edge.input != null ? edge.input.edgeConnector.edgeDragHelper.draggedPort : null);
            m_SearchWindowProvider.target = null;
            m_SearchWindowProvider.connectedPort = (BlueprintPort)draggedPort;
            m_SearchWindowProvider.regenerateEntries = true;//need to be sure the entires are relevant to the edge we are dragging
            SearcherWindow.Show(m_editorWindow, (m_SearchWindowProvider as SearcherProvider).LoadSearchWindow(),
                item => (m_SearchWindowProvider as SearcherProvider).OnSearcherSelectEntry(item, position),
                position, null);
            m_SearchWindowProvider.regenerateEntries = true;//entries no longer necessarily relevant, need to regenerate
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(EdgeConnectorListener), nameof(OnDropOutsidePort))} - ({graphView}, {edge})");

            m_EdgesToCreate.Clear();
            m_EdgesToCreate.Add(edge);
            m_EdgesToDelete.Clear();
            if (edge.input.capacity == Capacity.Single)
            {
                foreach (Edge connection in edge.input.connections)
                {
                    if (connection != edge)
                    {
                        m_EdgesToDelete.Add(connection);
                    }
                }
            }

            if (edge.output.capacity == Capacity.Single)
            {
                foreach (Edge connection2 in edge.output.connections)
                {
                    if (connection2 != edge)
                    {
                        m_EdgesToDelete.Add(connection2);
                    }
                }
            }

            if (m_EdgesToDelete.Count > 0)
            {
                graphView.DeleteElements(m_EdgesToDelete);
            }

            List<Edge> edgesToCreate = m_EdgesToCreate;
            if (graphView.graphViewChanged != null)
            {
                edgesToCreate = graphView.graphViewChanged(m_GraphViewChange).edgesToCreate;
            }

            foreach (Edge item in edgesToCreate)
            {
                graphView.AddElement(item);
                edge.input.Connect(item);
                edge.output.Connect(item);
            }
        }
    }
    #endregion

    public class BlueprintGraphEditorView : VisualElement, IDisposable
    {
        const string k_UserViewSettings = "VaporEditor.Graphs.ToggleSettings";
        const string k_FloatingWindowsLayoutKey = "VaporEditor.Graphs.FloatingWindowsLayout";

        public GraphEditorWindow Window { get; set; }
        public GraphObject GraphObject { get; set; }
        public string AssetName { get; set; }
        public List<string> SearchIncludeFlags { get; }
        public BlueprintGraphView GraphView { get; private set; }
        public List<IGraphEditorNode> EditorNodes { get; } = new();
        internal UserViewSettings ViewSettings { get; private set; }
        public InspectorViewController ViewController => _inspectorController;

        private InspectorViewController _inspectorController;
        private FloatingWindowsLayout _floatingWindowsLayout = new();
        private SearchWindowProvider m_SearchWindowProvider;
        private EdgeConnectorListener _edgeConnectorListener;
        private bool _maximized;
        private bool _debugMode;

        public event Action SaveRequested = delegate { };
        public event Action ShowInProjectRequested = delegate { };

        public Action<Group, string> m_GraphViewGroupTitleChanged;
        public Action<Group, IEnumerable<GraphElement>> m_GraphViewElementsAddedToGroup;
        public Action<Group, IEnumerable<GraphElement>> m_GraphViewElementsRemovedFromGroup;

        public BlueprintGraphEditorView(GraphEditorWindow graphEditorWindow, GraphObject graphObject, string graphName, List<string> searchIncludeFlags)
        {
            Window = graphEditorWindow;
            GraphObject = graphObject;
            AssetName = graphName;
            SearchIncludeFlags = searchIncludeFlags;
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/BlueprintGraphEditorView"));
            ColorUtility.TryParseHtmlString("#07070D", out var bgColor);
            style.backgroundColor = bgColor;
            var serializedSettings = EditorUserSettings.GetConfigValue(k_UserViewSettings);
            ViewSettings = JsonUtility.FromJson<UserViewSettings>(serializedSettings) ?? new UserViewSettings();

            CreateToolbar();
            var content = CreateContent();
            CreateSearchProvider();

            GraphView.nodeCreationRequest = NodeCreationRequest;
            //regenerate entries when graph view is refocused, to propogate subgraph changes
            GraphView.RegisterCallback<FocusInEvent>(evt => { m_SearchWindowProvider.regenerateEntries = true; });

            _edgeConnectorListener = new EdgeConnectorListener(GraphObject, m_SearchWindowProvider, Window);
            GraphObject.RequireRedraw = OnRedrawNode;

            AddNodes();
            AddEdges();
            Add(content);

            // Graph settings need to be initialized after the target setup
            _inspectorController.View.InitializeGraphSettings();
        }

        private void OnRedrawNode(NodeModel model)
        {
            if (model == null)
            {
                Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintGraphEditorView), nameof(OnRedrawNode))} - Moduel Null");
                return;
            }
            var editorNode = EditorNodes.FirstOrDefault(n => n.GetNode() == model);
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintGraphEditorView), nameof(OnRedrawNode))} - Redrawing [{editorNode}]");
            editorNode?.RedrawPorts(_edgeConnectorListener);
            AddEdges();
        }

        private void NodeCreationRequest(NodeCreationContext context)
        {
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintGraphEditorView), nameof(NodeCreationRequest))} - Target [{context.target}]");
            if (EditorWindow.focusedWindow == Window) //only display the search window when current graph view is focused
            {
                m_SearchWindowProvider.connectedPort = null;
                m_SearchWindowProvider.target = context.target;
                var displayPosition = context.screenMousePosition;

                SearcherWindow.Show(Window, (m_SearchWindowProvider as SearcherProvider).LoadSearchWindow(),
                    item => (m_SearchWindowProvider as SearcherProvider).OnSearcherSelectEntry(item, displayPosition),
                    displayPosition, null, new SearcherWindow.Alignment(SearcherWindow.Alignment.Vertical.Center, SearcherWindow.Alignment.Horizontal.Left));
            }
        }

        private void CreateToolbar()
        {
            var toolbar = new IMGUIContainer(() =>
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_SaveAs","Save"), EditorStyles.toolbarButton))
                {
                    SaveRequested.Invoke();
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_scenepicking_pickable_hover", "Show In Hierarchy"), EditorStyles.toolbarButton))
                {
                    ShowInProjectRequested.Invoke();
                }

                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();
                _maximized = _maximized
                    ? GUILayout.Toggle(_maximized, EditorGUIUtility.IconContent("d_FullscreenOn", "Maximize"), EditorStyles.toolbarButton)
                    : GUILayout.Toggle(_maximized, EditorGUIUtility.IconContent("d_Fullscreen", "Minimize"), EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                {
                    Window.maximized = _maximized;
                    Debug.Log($"Window Maximized: {_maximized}");
                }


                EditorGUI.BeginChangeCheck();
                _debugMode = _debugMode
                    ? GUILayout.Toggle(_debugMode, EditorGUIUtility.IconContent("debug On", "Toggle Debug On"), EditorStyles.toolbarButton)
                    : GUILayout.Toggle(_debugMode, EditorGUIUtility.IconContent("debug", "Toggle Debug Off"), EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                {
                    Debug.Log($"Debug Mode: {_debugMode}");
                }

                if (GUILayout.Button("Mock Evaluate", EditorStyles.toolbarButton))
                {
                    var mockGraph = GraphObject.Graph.Build(true);
                    if(mockGraph is MathGraph mathGraph)
                    {
                        mathGraph.Evaluate(null);
                        Debug.Log($"{TooltipMarkup.Method("MockEvaluate")} - Value [{mathGraph.Value}]");
                    }

                    if (mockGraph is FunctionGraph funcGraph)
                    {
                        funcGraph.Evaluate(null);
                        Debug.Log($"{TooltipMarkup.Method("MockEvaluate")} - Function Graph");
                    }
                }

                EditorGUI.BeginChangeCheck();
                //m_UserViewSettings.isInspectorVisible = GUILayout.Toggle(m_UserViewSettings.isInspectorVisible, "Graph Inspector", EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                {
                    //UserViewSettingsChangeCheck(newColorIndex);
                }
                GUILayout.EndHorizontal();
            });
            Add(toolbar);
        }

        private VisualElement CreateContent()
        {
            var content = new VisualElement { name = "content" };
            GraphView = new BlueprintGraphView(this, GraphObject)
            { name = "GraphView", viewDataKey = "BlueprintGraphView" };
            GraphView.SetupZoom(0.05f, 8);
            GraphView.AddManipulator(new ContentDragger());
            GraphView.AddManipulator(new SelectionDragger());
            GraphView.AddManipulator(new RectangleSelector());
            GraphView.AddManipulator(new ClickSelector());

            RegisterGraphViewCallbacks();
            content.Add(GraphView);

            string serializedWindowLayout = EditorUserSettings.GetConfigValue(k_FloatingWindowsLayoutKey);
            if (!string.IsNullOrEmpty(serializedWindowLayout))
            {
                _floatingWindowsLayout = JsonUtility.FromJson<FloatingWindowsLayout>(serializedWindowLayout);
            }

            CreateInspector();

            GraphView.graphViewChanged = OnGraphViewChanged;

            RegisterCallbackOnce<GeometryChangedEvent>(ApplySerializedWindowLayouts);
            return content;
        }

        private void CreateInspector()
        {
            var inspectorViewModel = new InspectorViewModel() { ParentView = GraphView };
            _inspectorController = new InspectorViewController(GraphObject.Graph, inspectorViewModel, GraphObject);
            GraphView.OnSelectionChange += _inspectorController.View.TriggerInspectorUpdate;
            // Undo/redo actions that only affect selection don't trigger the above callback for some reason, so we also have to do this
            Undo.undoRedoPerformed += (() => { _inspectorController?.View?.TriggerInspectorUpdate(GraphView?.selection); });
        }

        private void CreateSearchProvider()
        {
            m_SearchWindowProvider = new SearcherProvider();
            m_SearchWindowProvider.Initialize(Window, this);
        }

        public void Dispose()
        {
            if (this != null)
            {
                if (GraphView != null)
                {
                    SaveRequested = null;
                    ShowInProjectRequested = null;
                    foreach (var edge in this.Query<Edge>().ToList())
                    {
                        edge.output = null;
                        edge.input = null;
                    }

                    GraphView.nodeCreationRequest = null;
                    GraphView = null;
                    Debug.Log("Nodes Disposed");
                }
                _inspectorController?.Dispose();
                _inspectorController = null;

                if (m_SearchWindowProvider != null)
                {
                    m_SearchWindowProvider.Dispose();
                    m_SearchWindowProvider = null;
                }
            }
        }

        #region - Callbacks -
        private void RegisterGraphViewCallbacks()
        {
            GraphView.groupTitleChanged = m_GraphViewGroupTitleChanged;
            GraphView.elementsAddedToGroup = m_GraphViewElementsAddedToGroup;
            GraphView.elementsRemovedFromGroup = m_GraphViewElementsRemovedFromGroup;
        }

        private void UnregisterGraphViewCallbacks()
        {
            GraphView.groupTitleChanged = null;
            GraphView.elementsAddedToGroup = null;
            GraphView.elementsRemovedFromGroup = null;
        }

        /// <summary>
        /// Create the default docking(position/size) layouts for the windows.
        /// </summary>
        /// <param name="evt"></param>
        private void ApplySerializedWindowLayouts(GeometryChangedEvent evt)
        {
            _inspectorController.View.DeserializeLayout();
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == (int)MouseButton.LeftMouse && evt.clickCount == 2)
            {
                if (evt.target is Edge edgeTarget)
                {
                    Vector2 pos = evt.mousePosition;
                    GraphView.CreateRedirectNode(pos, edgeTarget);
                }
            }
        }
        #endregion

        #region - Setup Nodes -
        private void AddNodes()
        {
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintGraphEditorView), nameof(AddNodes))} - {GraphObject.Graph.Nodes.Count}");
            foreach (var node in GraphObject.Graph.Nodes)
            {
                NParamNodeDrawerHelper.AddNodes(node, this, _edgeConnectorListener, EditorNodes, null);
            }
        }

        private void AddEdges()
        {
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintGraphEditorView), nameof(AddEdges))} - EditorNodes:{EditorNodes.Count}");
            foreach (var rightNode in EditorNodes)
            {
                var edges = rightNode.GetNode().InEdges;
                foreach (var edge in edges)
                {
                    var leftNode = EditorNodes.FirstOrDefault(iNode => edge.OutputGuidMatches(iNode.GetNode().Guid));
                    if (leftNode != null)
                    {
                        if(leftNode.OutPorts.TryGetValue(edge.OutSlot.SlotName, out var outPort) && rightNode.InPorts.TryGetValue(edge.InSlot.SlotName, out var inPort))
                        {
                            if(outPort.connected || inPort.connected)
                            {
                                Debug.LogWarning($"{TooltipMarkup.ClassMethod(nameof(BlueprintGraphEditorView), nameof(AddEdges))} - A port already has a connection. {edge.OutSlot.SlotName}: {outPort.connected} -> {edge.InSlot.SlotName} {inPort.connected}");
                                continue;
                            }
                            var e = outPort.ConnectTo(inPort);
                            rightNode.OnConnectedInputEdge(edge.InSlot.SlotName);
                            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintGraphEditorView), nameof(AddEdges))} - Connected {edge.OutSlot.SlotName} -> {edge.InSlot.SlotName}");
                            GraphView.AddElement(e);
                        }
                        else
                        {
                            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintGraphEditorView), nameof(AddEdges))} - Could Not Connect {edge.OutSlot.SlotName} -> {edge.InSlot.SlotName}");
                        }                        
                    }
                    else
                    {
                        Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintGraphEditorView), nameof(AddEdges))} - Could Not Find Output Node {edge.OutSlot.SlotName} -> {edge.InSlot.SlotName}");
                    }
                }
            }
        }
        #endregion

        #region - Edit Graph -
        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
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
                    Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintGraphEditorView), nameof(OnGraphViewChanged))} - Edges To Create [{graphViewChange.edgesToCreate.Count}]");
                    foreach (var edge in graphViewChange.edgesToCreate)
                    {
                        AddEdge(edge);
                    }
                    // We don't clear the edges here because we let the EdgeConnectionListener handle the connecting.
                }
            }

            void _MoveElements(GraphViewChange graphViewChange)
            {
                if (graphViewChange.movedElements != null)
                {
                    Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintGraphEditorView), nameof(OnGraphViewChanged))} - Moved Elements [{graphViewChange.movedElements.Count}]");
                    foreach (var element in graphViewChange.movedElements)
                    {
                        if (element is IGraphEditorNode node)
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
                    Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintGraphEditorView), nameof(OnGraphViewChanged))} - Removed Elements [{graphViewChange.elementsToRemove.Count}]");
                    foreach (var edge in graphViewChange.elementsToRemove.OfType<Edge>())
                    {
                        if (edge.input != null && edge.output != null && edge.input.node is IGraphEditorNode rightN && edge.output.node is IGraphEditorNode leftN)
                        {
                            var left = leftN.GetNode();
                            var right = rightN.GetNode();

                            // Remove the InEdge link from the parent.
                            var idx = right.InEdges.FindIndex(edgeCon => edgeCon.OutputGuidMatches(left.Guid));
                            if (idx != -1)
                            {
                                Debug.Log($"Removed Right Index: {idx}");
                                var rightSlot = edge.input.GetSlot();
                                rightN.OnDisconnectedInputEdge(rightSlot.UniqueName);
                                right.InEdges.RemoveAt(idx);
                            }

                            // Remove the OutEdge Link From The Child
                            idx = left.OutEdges.FindIndex(edge => edge.InputGuidMatches(right.Guid));
                            if (idx != -1)
                            {
                                Debug.Log($"Removed Left Index: {idx}");
                                left.OutEdges.RemoveAt(idx);
                            }
                        }
                    }

                    foreach (var node in graphViewChange.elementsToRemove.OfType<IGraphEditorNode>())
                    {
                        Debug.Log("Removing Node" + node);
                        EditorNodes.Remove(node);
                        GraphObject.Graph.Nodes.Remove(node.GetNode());
                    }
                }
            }
        }

        public void AddNode(NodeModel node)
        {
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintGraphEditorView), nameof(AddNode))} - {node.GetType().Name}");

            if (NParamNodeDrawerHelper.AddNodes(node, this, _edgeConnectorListener, EditorNodes, GraphObject.Graph.Nodes))
                return;
        }

        private void AddEdge(Edge edge)
        {
            var rightNode = (IGraphEditorNode)edge.input.node; // Right Node
            var leftNode = (IGraphEditorNode)edge.output.node; // Left Node

            if (rightNode != null && leftNode != null)
            {
                var right = rightNode.GetNode();
                PortSlot rightSlot = edge.input.GetSlot();
                var left = leftNode.GetNode();
                PortSlot leftSlot = edge.output.GetSlot();

                Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintGraphEditorView), nameof(AddEdge))} - In:{right.GetType().Name} | Out:{left.GetType().Name}");


                rightNode.OnConnectedInputEdge(rightSlot.UniqueName);

                left.OutEdges.Add(new EdgeConnection(new(rightSlot.UniqueName, rightSlot.Index, right.Guid), new(leftSlot.UniqueName, leftSlot.Index, left.Guid)));
                right.InEdges.Add(new EdgeConnection(new(rightSlot.UniqueName, rightSlot.Index, right.Guid), new(leftSlot.UniqueName, leftSlot.Index, left.Guid)));
            }
            else
            {
                Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintGraphEditorView), nameof(AddEdge))} - IO is invalid | In:{rightNode == null} | Out:{leftNode == null}");
            }
        }
        #endregion        
    }
}
