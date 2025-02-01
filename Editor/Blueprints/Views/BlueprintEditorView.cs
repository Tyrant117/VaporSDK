using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Blueprints;
using Vapor.Inspector;
using VaporEditor.Inspector;
using static UnityEditor.Experimental.GraphView.Port;
using GraphElement = UnityEditor.Experimental.GraphView.GraphElement;

namespace VaporEditor.Blueprints
{
    public class EdgeConnectorListener : IEdgeConnectorListener
    {
        private readonly BlueprintGraphSo m_Graph;
        private readonly BlueprintSearchWindowProvider m_SearchWindowProvider;
        private readonly EditorWindow m_editorWindow;

        private GraphViewChange m_GraphViewChange;
        private List<Edge> m_EdgesToCreate;
        private List<GraphElement> m_EdgesToDelete;

        public EdgeConnectorListener(BlueprintGraphSo graph, BlueprintSearchWindowProvider searchWindowProvider, EditorWindow editorWindow)
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
            m_SearchWindowProvider.Target = null;
            m_SearchWindowProvider.ConnectedPort = (BlueprintEditorPort)draggedPort;
            m_SearchWindowProvider.RegenerateEntries = true;//need to be sure the entires are relevant to the edge we are dragging
            SearcherWindow.Show(m_editorWindow, m_SearchWindowProvider.LoadSearchWindow(false, out var items),
                item => (m_SearchWindowProvider).OnSearcherSelectEntry(item, position),
                position, null);
            m_SearchWindowProvider.RegenerateEntries = true;//entries no longer necessarily relevant, need to regenerate
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
    
    public class BlueprintEditorView : VisualElement, IDisposable, ISearchEntrySelected
    {
        public BlueprintEditorWindow Window { get; set; }
        public BlueprintGraphSo GraphObject { get; set; }
        public string AssetName { get; set; }
        public BlueprintGraphView GraphView { get; private set; }
        public List<IBlueprintEditorNode> EditorNodes { get; } = new();
        
        private BlueprintSearchWindowProvider m_SearchWindowProvider;
        private EdgeConnectorListener _edgeConnectorListener;
        private bool _maximized;
        private bool _debugMode;
        
        // Events
        public event Action SaveRequested = delegate { };
        public event Action ShowInProjectRequested = delegate { };
        public event Action CloseRequested = delegate { };
        
        public Action<Group, string> m_GraphViewGroupTitleChanged;
        public Action<Group, IEnumerable<GraphElement>> m_GraphViewElementsAddedToGroup;
        public Action<Group, IEnumerable<GraphElement>> m_GraphViewElementsRemovedFromGroup;

        public BlueprintEditorView(BlueprintEditorWindow graphEditorWindow, BlueprintGraphSo graphObject, string graphName)
        {
            Window = graphEditorWindow;
            GraphObject = graphObject;
            AssetName = graphName;
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/BlueprintEditorView"));
            ColorUtility.TryParseHtmlString("#07070D", out var bgColor);
            style.backgroundColor = bgColor;

            CreateToolbar();
            var content = CreateContent();
            CreateSearchProvider();

            GraphView.nodeCreationRequest = NodeCreationRequest;
            //regenerate entries when graph view is refocused, to propogate subgraph changes
            GraphView.RegisterCallback<FocusInEvent>(evt => { m_SearchWindowProvider.RegenerateEntries = true; });

            _edgeConnectorListener = new EdgeConnectorListener(GraphObject, m_SearchWindowProvider, Window);

            AddNodes();
            AddEdges();
            Add(content);
        }
        
        private void OnRedrawNode(BlueprintNodeDataModel model)
        {
            if (model == null)
            {
                Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(OnRedrawNode))} - Moduel Null");
                return;
            }
            var editorNode = EditorNodes.FirstOrDefault(n => n.Node == model);
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(OnRedrawNode))} - Redrawing [{editorNode}]");
            editorNode?.RedrawPorts(_edgeConnectorListener);
            AddEdges();
        }
        private void NodeCreationRequest(NodeCreationContext context)
        {
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(NodeCreationRequest))} - Target [{context.target}]");
            if (EditorWindow.focusedWindow != Window)
            {
                return;
            }

            //only display the search window when current graph view is focused
            m_SearchWindowProvider.ConnectedPort = null;
            m_SearchWindowProvider.Target = context.target;
            //var displayPosition = RuntimePanelUtils.ScreenToPanel(this.panel, context.screenMousePosition);
            var displayPosition = context.screenMousePosition - Window.position.position;
            Debug.Log(displayPosition);
            Debug.Log(Window.position);

            SearcherWindow.Show(Window, m_SearchWindowProvider.LoadSearchWindow(false, out var items),
                item => m_SearchWindowProvider.OnSearcherSelectEntry(item, displayPosition),
                displayPosition, null, new SearcherWindow.Alignment(SearcherWindow.Alignment.Vertical.Top, SearcherWindow.Alignment.Horizontal.Left));
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
                    var en = GraphObject.BlueprintNodes.FirstOrDefault(x => x.NodeType == BlueprintNodeType.Entry);
                    if (en != null)
                    {
                        var mock = new BlueprintFunctionGraph(GraphObject);
                        mock.Invoke((from outPortsValue in en.OutPorts.Values where outPortsValue.HasContent select outPortsValue.Content).ToArray(), 
                            x =>
                            {
                                Debug.Log("Mock Evaluated");
                                foreach (var pair in x.GetResults())
                                {
                                    Debug.Log($"{pair.Key} - {pair.Value}");
                                }
                            });
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

            GraphView.graphViewChanged = OnGraphViewChanged;
            return content;
        }
        
        private void CreateSearchProvider()
        {
            m_SearchWindowProvider = new BlueprintSearchWindowProvider();
            m_SearchWindowProvider.Graph = GraphObject;
            m_SearchWindowProvider.Initialize(this);
        }
        
        public void Dispose()
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

            if (m_SearchWindowProvider != null)
            {
                m_SearchWindowProvider.Dispose();
                m_SearchWindowProvider = null;
            }
        }

        #region - Callbacks -

        private void RegisterGraphViewCallbacks()
        {
            GraphView.groupTitleChanged = m_GraphViewGroupTitleChanged;
            GraphView.elementsAddedToGroup = m_GraphViewElementsAddedToGroup;
            GraphView.elementsRemovedFromGroup = m_GraphViewElementsRemovedFromGroup;
        }

        #endregion
        
        #region - Setup Nodes -
        private void AddNodes()
        {
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddNodes))} - {GraphObject.BlueprintNodes.Count}");
            foreach (var node in GraphObject.BlueprintNodes)
            {
                BlueprintNodeDrawerUtility.AddNodes(node, this, _edgeConnectorListener, EditorNodes, null);
            }
        }

        private void AddEdges()
        {
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdges))} - EditorNodes:{EditorNodes.Count}");
            foreach (var rightNode in EditorNodes)
            {
                var edges = rightNode.Node.InEdges;
                foreach (var edge in edges)
                {
                    var leftNode = EditorNodes.FirstOrDefault(iNode => edge.LeftGuidMatches(iNode.Node.Guid));
                    if (leftNode != null)
                    {
                        if(leftNode.OutPorts.TryGetValue(edge.LeftSidePort.PortName, out var outPort) && rightNode.InPorts.TryGetValue(edge.RightSidePort.PortName, out var inPort))
                        {
                            if (rightNode.Node.InPorts.TryGetValue(edge.RightSidePort.PortName, out var bpInPort))
                            {
                                if(!bpInPort.IsTransitionPort && outPort.connected)
                                {
                                    Debug.LogWarning($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdges))} - A port already has a connection. {edge.LeftSidePort.PortName}: {outPort.connected} -> {edge.RightSidePort.PortName} {inPort.connected}");
                                    continue;
                                }
                            }
                            else if(inPort.connected)
                            {
                                Debug.LogWarning($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdges))} - A port already has a connection. {edge.LeftSidePort.PortName}: {outPort.connected} -> {edge.RightSidePort.PortName} {inPort.connected}");
                                continue;
                            }

                            if (leftNode.Node.OutPorts.TryGetValue(edge.LeftSidePort.PortName, out var bpOutPort))
                            {
                                if(!bpOutPort.AllowMultiple && outPort.connected)
                                {
                                    Debug.LogWarning($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdges))} - A port already has a connection. {edge.LeftSidePort.PortName}: {outPort.connected} -> {edge.RightSidePort.PortName} {inPort.connected}");
                                    continue;
                                }
                            }
                            else if(outPort.connected)
                            {
                                Debug.LogWarning($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdges))} - A port already has a connection. {edge.LeftSidePort.PortName}: {outPort.connected} -> {edge.RightSidePort.PortName} {inPort.connected}");
                                continue;
                            }
                            
                            var e = outPort.ConnectTo(inPort);
                            rightNode.OnConnectedInputEdge(edge.RightSidePort.PortName);
                            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdges))} - Connected {edge.LeftSidePort.PortName} -> {edge.RightSidePort.PortName}");
                            GraphView.AddElement(e);
                        }
                        else
                        {
                            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdges))} - Could Not Connect {edge.LeftSidePort.PortName} -> {edge.RightSidePort.PortName}");
                        }                        
                    }
                    else
                    {
                        Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdges))} - Could Not Find Output Node {edge.LeftSidePort.PortName} -> {edge.RightSidePort.PortName}");
                    }
                }
            }
        }
        #endregion
        
        #region - Edit Graph -
        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            _CreateEdges();
            _MoveElements();
            _RemoveElements();

            //UpdateEdgeColors(nodesToUpdate);

            Window.MarkDirty();
            return graphViewChange;

            void _CreateEdges()
            {
                if (graphViewChange.edgesToCreate != null)
                {
                    Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(OnGraphViewChanged))} - Edges To Create [{graphViewChange.edgesToCreate.Count}]");
                    foreach (var edge in graphViewChange.edgesToCreate)
                    {
                        AddEdge(edge);
                    }
                    // We don't clear the edges here because we let the EdgeConnectionListener handle the connecting.
                }
            }

            void _MoveElements()
            {
                if (graphViewChange.movedElements != null)
                {
                    Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(OnGraphViewChanged))} - Moved Elements [{graphViewChange.movedElements.Count}]");
                    foreach (var element in graphViewChange.movedElements)
                    {
                        if (element is IBlueprintEditorNode node)
                        {
                            node.Node.Position = element.parent.ChangeCoordinatesTo(GraphView.contentViewContainer, element.GetPosition());
                        }

                        if (element is StickyNote stickyNote)
                        {
                            //SetStickyNotePosition(stickyNote);
                        }
                    }
                }
            }

            void _RemoveElements()
            {
                if (graphViewChange.elementsToRemove != null)
                {
                    Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(OnGraphViewChanged))} - Removed Elements [{graphViewChange.elementsToRemove.Count}]");
                    foreach (var edge in graphViewChange.elementsToRemove.OfType<Edge>())
                    {
                        if (edge.input != null && edge.output != null && edge.input.node is IBlueprintEditorNode rightN && edge.output.node is IBlueprintEditorNode leftN)
                        {
                            var left = leftN.Node;
                            var right = rightN.Node;
                            
                            // NEW
                            var rightSlot = edge.input.GetSlot();
                            var leftSlot = edge.output.GetSlot();
                            BlueprintEdgeConnection edgeToMatch = new BlueprintEdgeConnection(
                                new BlueprintPortReference(leftSlot.PortName, left.Guid, false),
                                new BlueprintPortReference(rightSlot.PortName, right.Guid, false));
                            
                            right.InEdges.Remove(edgeToMatch);
                            left.OutEdges.Remove(edgeToMatch);

                            // OLD
                            // Remove the InEdge link from the parent.
                            // var idx = right.InEdges.FindIndex(edgeCon => edgeCon.LeftGuidMatches(left.Guid));
                            // if (idx != -1)
                            // {
                            //     Debug.Log($"Removed Right Index: {idx}");
                            //     var rightSlot = edge.input.GetSlot();
                            //     rightN.OnDisconnectedInputEdge(rightSlot.PortName);
                            //     right.InEdges.RemoveAll(x => x.RightSidePort.PortName == rightSlot.PortName);
                            // }
                            //
                            // // Remove the OutEdge Link From The Child
                            // idx = left.OutEdges.FindIndex(e => e.RightGuidMatches(right.Guid));
                            // if (idx != -1)
                            // {
                            //     Debug.Log($"Removed Left Index: {idx}");
                            //     var leftSlot = edge.output.GetSlot();
                            //     left.OutEdges.RemoveAll(x => x.LeftSidePort.PortName == leftSlot.PortName);
                            // }
                        }
                    }

                    foreach (var node in graphViewChange.elementsToRemove.OfType<IBlueprintEditorNode>())
                    {
                        Debug.Log("Removing Node" + node);
                        EditorNodes.Remove(node);
                        GraphObject.BlueprintNodes.Remove(node.Node);
                    }
                }
            }
        }

        public void AddNode(BlueprintNodeDataModel node)
        {
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddNode))} - {node.GetType().Name}");
            BlueprintNodeDrawerUtility.AddNodes(node, this, _edgeConnectorListener, EditorNodes, GraphObject.BlueprintNodes);
        }

        public void AddEdge(Edge edge)
        {
            var rightNode = (IBlueprintEditorNode)edge.input.node; // Right Node
            var leftNode = (IBlueprintEditorNode)edge.output.node; // Left Node

            if (rightNode != null && leftNode != null)
            {
                var right = rightNode.Node;
                BlueprintPortSlot rightSlot = edge.input.GetSlot();
                var left = leftNode.Node;
                BlueprintPortSlot leftSlot = edge.output.GetSlot();

                Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdge))} - In:{right.GetType().Name} | Out:{left.GetType().Name}");


                rightNode.OnConnectedInputEdge(rightSlot.PortName);

                left.OutEdges.Add(new BlueprintEdgeConnection(new(leftSlot.PortName, left.Guid, leftSlot.IsTransitionPort), new(rightSlot.PortName, right.Guid, rightSlot.IsTransitionPort)));
                right.InEdges.Add(new BlueprintEdgeConnection(new(leftSlot.PortName, left.Guid, leftSlot.IsTransitionPort), new(rightSlot.PortName, right.Guid, rightSlot.IsTransitionPort)));
            }
            else
            {
                Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdge))} - IO is invalid | In:{rightNode == null} | Out:{leftNode == null}");
            }
        }
        #endregion

        public void DeselectAll()
        {
            
        }

        public void Select(SearcherItem searcherItem)
        {
            if (searcherItem == null)
            {
                return;
            }
            
            var tuple = ((BlueprintSearchEntry entry, Vector2 pos))searcherItem.UserData;
            var graphMousePosition = GraphView.contentViewContainer.WorldToLocal(tuple.pos);

            BlueprintNodeDataModel node = null;
            switch (tuple.entry.NodeType)
            {
                case BlueprintNodeType.Method:
                    node = new BlueprintNodeDataModel
                    {
                        Position = new Rect(graphMousePosition, Vector2.zero),
                        AssemblyQualifiedType = tuple.entry.MethodInfo.DeclaringType?.AssemblyQualifiedName,
                        MethodName = tuple.entry.MethodInfo.Name
                    };
                    break;
                case BlueprintNodeType.Entry:
                    break;
                case BlueprintNodeType.Return:
                    node = BlueprintNodeDataModelUtility.CreateOrUpdateReturnNode(null, GraphObject.OutputParameters);
                    node.Position = new Rect(graphMousePosition, Vector2.zero);
                    break;
                case BlueprintNodeType.IfElse:
                    node = BlueprintNodeDataModelUtility.CreateOrUpdateIfElseNode(null);
                    node.Position = new Rect(graphMousePosition, Vector2.zero);
                    break;
                case BlueprintNodeType.ForEach:
                    node = BlueprintNodeDataModelUtility.CreateOrUpdateForEachNode(null);
                    node.Position = new Rect(graphMousePosition, Vector2.zero);
                    break;
                case BlueprintNodeType.Getter:
                {
                    var tempData = GraphObject.TempData.FirstOrDefault(x => x.FieldName == tuple.entry.GetterSetterFieldName);
                    node = BlueprintNodeDataModelUtility.CreateOrUpdateGetterNode(null, tempData);
                    node.Position = new Rect(graphMousePosition, Vector2.zero);
                    break;
                }
                case BlueprintNodeType.Setter:
                {
                    var tempData = GraphObject.TempData.FirstOrDefault(x => x.FieldName == tuple.entry.GetterSetterFieldName);
                    node = BlueprintNodeDataModelUtility.CreateOrUpdateSetterNode(null, tempData);
                    node.Position = new Rect(graphMousePosition, Vector2.zero);
                    break;
                }
            }

            if (node != null)
            {
                node.Validate();

                AddNode(node);
            }


            if (m_SearchWindowProvider.ConnectedPort != null)
            {
                var last = EditorNodes[^1];
                Port firstGoodPort = null;
                if (m_SearchWindowProvider.ConnectedPort.direction == Direction.Input)
                {
                    firstGoodPort = last.OutPorts.Values.FirstOrDefault(x => x.portType == m_SearchWindowProvider.ConnectedPort.portType);
                }
                else
                {
                    firstGoodPort = last.InPorts.Values.FirstOrDefault(x => x.portType == m_SearchWindowProvider.ConnectedPort.portType);
                }
                if (firstGoodPort == null)
                {
                    return;
                }
                
                var e = m_SearchWindowProvider.ConnectedPort.ConnectTo(firstGoodPort);
                AddEdge(e);
                GraphView.AddElement(e);
            }
        }
    }
}
