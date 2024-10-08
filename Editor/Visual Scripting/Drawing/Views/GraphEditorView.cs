using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor;
using Vapor.VisualScripting;
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

        public EdgeConnectorListener(GraphObject graph, SearchWindowProvider searchWindowProvider, EditorWindow editorWindow)
        {
            m_Graph = graph;
            m_SearchWindowProvider = searchWindowProvider;
            m_editorWindow = editorWindow;
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            Debug.Log($"EdgeConnectorListener.OnDropOutsidePort({edge}, {position})");

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
            Debug.Log($"EdgeConnectorListener.OnDrop({graphView}, {edge})");

            var leftSlot = edge.output.GetSlot();
            var rightSlot = edge.input.GetSlot();
            if (leftSlot != null && rightSlot != null)
            {
                //m_Graph.owner.RegisterCompleteObjectUndo("Connect Edge");
                //m_Graph.Connect(leftSlot.slotReference, rightSlot.slotReference);
            }
        }
    }
    #endregion

    public class GraphEditorView : VisualElement, IDisposable
    {
        const string k_ClassTag = "<b>GraphEditorView</b>";

        const string k_UserViewSettings = "VaporEditor.Graphs.ToggleSettings";
        const string k_FloatingWindowsLayoutKey = "VaporEditor.Graphs.FloatingWindowsLayout";

        public GraphEditorWindow Window { get; set; }
        public GraphObject GraphObject { get; set; }
        public string AssetName { get; set; }

        public BlueprintGraphView GraphView { get; private set; }
        public List<IGraphEditorNode> EditorNodes { get; } = new();
        internal UserViewSettings ViewSettings { get; private set; }

        private InspectorViewController _inspectorController;
        private FloatingWindowsLayout _floatingWindowsLayout = new();
        private SearchWindowProvider m_SearchWindowProvider;
        //private BlackboardViewController _blackboardController;
        private EdgeConnectorListener _edgeConnectorListener;
        private bool _maximized;
        private bool _debugMode;

        public event Action SaveRequested = delegate { };
        public event Action ShowInProjectRequested = delegate { };

        public Action<Group, string> m_GraphViewGroupTitleChanged;
        public Action<Group, IEnumerable<GraphElement>> m_GraphViewElementsAddedToGroup;
        public Action<Group, IEnumerable<GraphElement>> m_GraphViewElementsRemovedFromGroup;

        public GraphEditorView(GraphEditorWindow graphEditorWindow, GraphObject graphObject, string graphName, List<string> searchIncludeFlags)
        {
            Window = graphEditorWindow;
            GraphObject = graphObject;
            AssetName = graphName;

            styleSheets.Add(Resources.Load<StyleSheet>("Styles/GraphEditorView"));
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

            AddNodes();
            AddEdges();
            Add(content);

            // Graph settings need to be initialized after the target setup
            _inspectorController.View.InitializeGraphSettings();
        }

        private void NodeCreationRequest(NodeCreationContext context)
        {
            Debug.Log($"GraphEditorView.NodeCreationRequest: target:{context.target}");
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
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_SaveAs"), EditorStyles.toolbarButton))
                {
                    SaveRequested.Invoke();
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_scenepicking_pickable_hover"), EditorStyles.toolbarButton))
                {
                    ShowInProjectRequested.Invoke();
                }

                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();
                _maximized = _maximized
                    ? GUILayout.Toggle(_maximized, EditorGUIUtility.IconContent("d_FullscreenOn"), EditorStyles.toolbarButton)
                    : GUILayout.Toggle(_maximized, EditorGUIUtility.IconContent("d_Fullscreen"), EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                {
                    Window.maximized = _maximized;
                    Debug.Log($"Window Maximized: {_maximized}");
                }


                EditorGUI.BeginChangeCheck();
                _debugMode = _debugMode
                    ? GUILayout.Toggle(_debugMode, EditorGUIUtility.IconContent("debug On"), EditorStyles.toolbarButton)
                    : GUILayout.Toggle(_debugMode, EditorGUIUtility.IconContent("debug"), EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                {
                    Debug.Log($"Debug Mode: {_debugMode}");
                }

                if (GUILayout.Button("Mock Evaluate", EditorStyles.toolbarButton))
                {
                    var mockGraph = GraphObject.Graph.Build(true);
                    if (mockGraph is IEvaluatorNode<double, IExternalValueSource> evalMock)
                    {
                        Debug.Log($"Mock Eval: {evalMock.Evaluate(null, null)}");
                    }
                }

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
            //parent.Insert(0, toolbar);
            //Insert(0, toolbar);
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

            //// Bugfix 1312222. Running 'ResetSelectedBlockNodes' on all mouse up interactions will break selection
            //// after changing tabs. This was originally added to fix a bug with middle-mouse clicking while dragging a block node.
            //GraphView.RegisterCallback<MouseUpEvent>(evt => { if (evt.button == (int)MouseButton.MiddleMouse) GraphView.ResetSelectedBlockNodes(); });
            // This takes care of when a property is dragged from BB and then the drag is ended by the Escape key, hides the scroll boundary regions and drag indicator if so
            GraphView.RegisterCallback<DragExitedEvent>(evt =>
            {
                //_blackboardController.Blackboard.OnDragExitedEvent(evt);
                //_blackboardController.Blackboard.hideDragIndicatorAction?.Invoke();
            });

            RegisterGraphViewCallbacks();
            content.Add(GraphView);

            string serializedWindowLayout = EditorUserSettings.GetConfigValue(k_FloatingWindowsLayoutKey);
            if (!string.IsNullOrEmpty(serializedWindowLayout))
            {
                _floatingWindowsLayout = JsonUtility.FromJson<FloatingWindowsLayout>(serializedWindowLayout);
            }

            CreateInspector();
            CreateBlackboard();

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

        private void CreateBlackboard()
        {
            //var blackboardViewModel = new BlackboardViewModel() { ParentView = GraphView, Model = GraphObject, Title = AssetName };
            //_blackboardController = new BlackboardViewController(GraphObject.Graph, blackboardViewModel, GraphObject);
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
                //_blackboardController = null;
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
            //_blackboardController.Blackboard.DeserializeLayout();

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
            Debug.Log($"{k_ClassTag}.AddNodes: {GraphObject.Graph.Nodes.Count}");
            foreach (var node in GraphObject.Graph.Nodes)
            {
                NParamNodeDrawerHelper.AddNodes(node, this, _edgeConnectorListener, EditorNodes, null);
            }
        }

        private void AddEdges()
        {
            Debug.Log($"{k_ClassTag}.AddEdges: EditorNodes:{EditorNodes.Count}");
            foreach (var inputNode in EditorNodes)
            {
                var edges = inputNode.GetNode().InEdges;
                foreach (var edge in edges)
                {
                    var outputNode = EditorNodes.FirstOrDefault(iNode => edge.OutputGuidMatches(iNode.GetNode().Guid));
                    if (outputNode != null)
                    {
                        //Debug.Log($"childOutPorts: {child.OutPorts.Count} Idx {edge.OutPortIndex} [Connect] rootInPorts: {root.InPorts.Count} Idx {edge.InPortIndex}");
                        //int outIndex = outputNode.GetNode().OutSlots.Values.ToList().FindIndex(x => x.UniqueName == edge.OutPortName);
                        //int inIndex = inputNode.GetNode().InSlots.Values.ToList().FindIndex(x => x.UniqueName == edge.InPortName);
                        var e = outputNode.OutPorts[edge.OutSlot.SlotName].ConnectTo(inputNode.InPorts[edge.InSlot.SlotName]);

                        inputNode.OnConnectedInputEdge(edge.OutSlot.SlotName);
                        GraphView.AddElement(e);
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
                    Debug.Log($"{k_ClassTag}.OnGraphViewChanged: edges:{graphViewChange.edgesToCreate.Count}");
                    foreach (var edge in graphViewChange.edgesToCreate)
                    {
                        AddEdge(edge);
                    }
                    graphViewChange.edgesToCreate.Clear();
                }
            }

            void _MoveElements(GraphViewChange graphViewChange)
            {
                if (graphViewChange.movedElements != null)
                {
                    Debug.Log($"{k_ClassTag}.OnGraphViewChanged: moved:{graphViewChange.movedElements.Count}");
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
                    Debug.Log($"{k_ClassTag}.OnGraphViewChanged: removed:{graphViewChange.elementsToRemove.Count}");
                    foreach (var edge in graphViewChange.elementsToRemove.OfType<Edge>())
                    {
                        if (edge.input != null && edge.output != null && edge.input.node is IGraphEditorNode rightN && edge.output.node is IGraphEditorNode leftN)
                        {
                            if (rightN.GetNode() is NodeModel rightNode && leftN.GetNode() is NodeModel leftNode)
                            {
                                // Remove the InEdge link from the parent.
                                var idx = rightNode.InEdges.FindIndex(edgeCon => edgeCon.OutputGuidMatches(leftNode.Guid));
                                if (idx != -1)
                                {
                                    //int inPortIndex = rightN.InPorts.IndexOf(edge.input);
                                    var rightSlot = (PortSlot)edge.input.userData;
                                    rightN.OnDisconnectedInputEdge(rightSlot.UniqueName);
                                    rightNode.InEdges.RemoveAt(idx);
                                }

                                // Remove the OutEdge Link From The Child
                                idx = leftNode.OutEdges.FindIndex(edge => edge.InputGuidMatches(rightNode.Guid));
                                if (idx != -1)
                                {
                                    leftNode.OutEdges.RemoveAt(idx);
                                }
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
            Debug.Log($"{k_ClassTag}.{nameof(AddNode)}: {node.GetType().Name}");

            if (NParamNodeDrawerHelper.AddNodes(node, this, _edgeConnectorListener, EditorNodes, GraphObject.Graph.Nodes))
                return;
        }

        private void AddEdge(Edge edge)
        {
            var inNode = (IGraphEditorNode)edge.input.node; // Right Node
            var outNode = (IGraphEditorNode)edge.output.node; // Left Node

            if (inNode != null && outNode != null)
            {
                //(string outPortName, string inPortName) edgeCon = ((string, string))edge.userData;
                var inNodeSo = inNode.GetNode();
                PortSlot inSlot = (PortSlot)edge.input.userData;
                var outNodeSo = outNode.GetNode();
                PortSlot outSlot = (PortSlot)edge.output.userData;

                Debug.Log($"{k_ClassTag}.{nameof(AddEdge)}: In:{inNodeSo.GetType().Name} | Out:{outNodeSo.GetType().Name}");

                //int inPortIndex = inNode.InPorts.Values.ToList().IndexOf(edge.input);
                //string inPortName = inNodeSo.InSlots.Values.ToArray()[inPortIndex].UniqueName;

                //int outPortIndex = outNode.OutPorts.Values.ToList().IndexOf(edge.output);
                //string outPortName = outNodeSo.OutSlots.Values.ToArray()[outPortIndex].UniqueName;


                inNode.OnConnectedInputEdge(inSlot.UniqueName);

                inNodeSo.InEdges.Add(new EdgeConnection(new(inSlot.UniqueName, inNodeSo.Guid), new(outSlot.UniqueName, outNodeSo.Guid)));
                outNodeSo.OutEdges.Add(new EdgeConnection(new(inSlot.UniqueName, inNodeSo.Guid), new(outSlot.UniqueName, inNodeSo.Guid)));
            }
            else
            {
                Debug.Log($"{k_ClassTag}.{nameof(AddEdge)}: IO is invalid | In:{inNode == null} | Out:{outNode == null}");
            }
        }
        #endregion        
    }
}
