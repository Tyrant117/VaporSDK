using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
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
        private readonly BlueprintEditorView _view;
        private readonly BlueprintGraphSo _graph;
        private readonly BlueprintSearchWindowProvider _searchWindowProvider;
        private readonly EditorWindow _editorWindow;

        private GraphViewChange _graphViewChange;
        private List<Edge> _edgesToCreate;
        private List<GraphElement> _edgesToDelete;

        public EdgeConnectorListener(BlueprintEditorView blueprintEditorView, BlueprintGraphSo graph, BlueprintSearchWindowProvider searchWindowProvider, EditorWindow editorWindow)
        {
            _view = blueprintEditorView;
            _graph = graph;
            _searchWindowProvider = searchWindowProvider;
            _editorWindow = editorWindow;

            _edgesToCreate = new List<Edge>();
            _edgesToDelete = new List<GraphElement>();
            _graphViewChange.edgesToCreate = _edgesToCreate;
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(EdgeConnectorListener), nameof(OnDropOutsidePort))} - Edge [{edge}] - Pos [{position}])");

            var draggedPort = (edge.output != null ? edge.output.edgeConnector.edgeDragHelper.draggedPort : null) ?? (edge.input != null ? edge.input.edgeConnector.edgeDragHelper.draggedPort : null);
            _searchWindowProvider.Target = null;
            _searchWindowProvider.ConnectedPort = (BlueprintEditorPort)draggedPort;
            _searchWindowProvider.RegenerateEntries = true;//need to be sure the entries are relevant to the edge we are dragging
            SearcherWindow.Show(_editorWindow, _searchWindowProvider.LoadSearchWindow(false, out var items),
                item => (_searchWindowProvider).OnSearcherSelectEntry(item, position),
                _view.SearchClosed,
                position, null);
            _searchWindowProvider.RegenerateEntries = true;//entries no longer necessarily relevant, need to regenerate
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(EdgeConnectorListener), nameof(OnDropOutsidePort))} - ({graphView}, {edge})");

            _edgesToCreate.Clear();
            _edgesToCreate.Add(edge);
            _edgesToDelete.Clear();
            if (edge.input.capacity == Capacity.Single)
            {
                foreach (Edge connection in edge.input.connections)
                {
                    if (connection != edge)
                    {
                        _edgesToDelete.Add(connection);
                    }
                }
            }

            if (edge.output.capacity == Capacity.Single)
            {
                foreach (Edge connection2 in edge.output.connections)
                {
                    if (connection2 != edge)
                    {
                        _edgesToDelete.Add(connection2);
                    }
                }
            }

            if (_edgesToDelete.Count > 0)
            {
                graphView.DeleteElements(_edgesToDelete);
            }

            List<Edge> edgesToCreate = _edgesToCreate;
            if (graphView.graphViewChanged != null)
            {
                edgesToCreate = graphView.graphViewChanged(_graphViewChange).edgesToCreate;
            }

            foreach (Edge item in edgesToCreate)
            {
                if (edge.output.portType != edge.input.portType)
                {
                    if (edge.input.portType == typeof(object))
                    {
                        // Objects can always be connected
                        graphView.AddElement(item);
                        edge.input.Connect(item);
                        edge.output.Connect(item);
                    }
                    else
                    {
                        // Create Redirect
                        _view.GraphView.CreateConverterNode(item);
                    }
                }
                else
                {
                    graphView.AddElement(item);
                    edge.input.Connect(item);
                    edge.output.Connect(item);
                }
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
        private readonly Dictionary<Type, Dictionary<Type, MethodInfo>> _canConvertMap = new();
        
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
            
            var converters = TypeCache.GetMethodsWithAttribute<BlueprintPinConverterAttribute>();
            foreach (var converter in converters)
            {
                var atr = converter.GetCustomAttribute<BlueprintPinConverterAttribute>();
                if (!_canConvertMap.TryGetValue(atr.SourceType, out var map))
                {
                    map = new Dictionary<Type, MethodInfo>();
                    _canConvertMap.Add(atr.SourceType, map);
                }

                map.Add(atr.TargetType, converter);
            }

            CreateToolbar();
            var content = CreateContent();
            CreateSearchProvider();

            GraphView.nodeCreationRequest = NodeCreationRequest;
            //regenerate entries when graph view is refocused, to propogate subgraph changes
            GraphView.RegisterCallback<FocusInEvent>(evt => { m_SearchWindowProvider.RegenerateEntries = true; });

            _edgeConnectorListener = new EdgeConnectorListener(this, GraphObject, m_SearchWindowProvider, Window);

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
                item => m_SearchWindowProvider.OnSearcherSelectEntry(item, displayPosition), SearchClosed,
                displayPosition, null, new SearcherWindow.Alignment(SearcherWindow.Alignment.Vertical.Top, SearcherWindow.Alignment.Horizontal.Left));
        }

        public void SearchClosed()
        {
            if (m_SearchWindowProvider == null)
            {
                return;
            }
            m_SearchWindowProvider.ConnectedPort = null;
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
                        var mock = new BlueprintFunctionGraph(GraphObject, true);
                        mock.Invoke((from outPortsValue in en.OutPorts.Values where outPortsValue.HasInlineValue select outPortsValue.InlineValue.Get()).ToArray(), 
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

        void OnMouseDown(MouseDownEvent evt)
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
                    var leftNode = EditorNodes.FirstOrDefault(iNode => edge.LeftSidePin.NodeGuid == iNode.Node.Guid);
                    if (leftNode != null)
                    {
                        // Get Connected Pins
                        if(leftNode.OutPorts.TryGetValue(edge.LeftSidePin.PinName, out var leftPort) && rightNode.InPorts.TryGetValue(edge.RightSidePin.PinName, out var rightPort))
                        {
                            var leftPin = leftPort.GetPin();
                            var rightPin = rightPort.GetPin();

                            // Left Pin Is Already Connect
                            if (leftPort.connected)
                            {
                                // Break if it is an execute pin or doesn't allow multiple wires.
                                // Executes can only have one output but multiple inputs.
                                if(leftPin.IsExecutePin || !leftPin.AllowMultipleWires)
                                {
                                    Debug.LogWarning(
                                        $"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdges))} - A port already has a connection. {edge.LeftSidePin.PinName}: {leftPort.connected} -> {edge.RightSidePin.PinName} {rightPort.connected}");
                                    continue;
                                }
                            }
                            
                            if(rightPort.connected && !rightPin.IsExecutePin)
                            {
                                // Break if right port is connected and it is not an execute pin.
                                // Right ports can only have one input value wire.
                                Debug.LogWarning($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdges))} - A port already has a connection. {edge.LeftSidePin.PinName}: {leftPort.connected} -> {edge.RightSidePin.PinName} {rightPort.connected}");
                                continue;
                            }
                            
                            var e = leftPort.ConnectTo(rightPort);
                            e.RegisterCallback<MouseDownEvent>(OnMouseDown);
                            rightNode.OnConnectedInputEdge(edge.RightSidePin.PinName);
                            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdges))} - Connected {edge.LeftSidePin.PinName} -> {edge.RightSidePin.PinName}");
                            GraphView.AddElement(e);
                        }
                        else
                        {
                            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdges))} - Could Not Connect {edge.LeftSidePin.PinName} -> {edge.RightSidePin.PinName}");
                        }                        
                    }
                    else
                    {
                        Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdges))} - Could Not Find Output Node {edge.LeftSidePin.PinName} -> {edge.RightSidePin.PinName}");
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
                            var rightSlot = edge.input.GetPin();
                            var leftSlot = edge.output.GetPin();
                            BlueprintWireReference edgeToMatch = new BlueprintWireReference(
                                new BlueprintPinReference(leftSlot.PortName, left.Guid, false),
                                new BlueprintPinReference(rightSlot.PortName, right.Guid, false));
                            
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
                BlueprintPin rightSlot = edge.input.GetPin();
                var left = leftNode.Node;
                BlueprintPin leftSlot = edge.output.GetPin();

                Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdge))} - In:{right.GetType().Name} | Out:{left.GetType().Name}");


                rightNode.OnConnectedInputEdge(rightSlot.PortName);

                left.OutEdges.Add(new BlueprintWireReference(new(leftSlot.PortName, left.Guid, leftSlot.IsExecutePin), new(rightSlot.PortName, right.Guid, rightSlot.IsExecutePin)));
                right.InEdges.Add(new BlueprintWireReference(new(leftSlot.PortName, left.Guid, leftSlot.IsExecutePin), new(rightSlot.PortName, right.Guid, rightSlot.IsExecutePin)));
                
                edge.RegisterCallback<MouseDownEvent>(OnMouseDown);
            }
            else
            {
                Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintEditorView), nameof(AddEdge))} - IO is invalid | In:{rightNode == null} | Out:{leftNode == null}");
            }
        }

        public void Connect(BlueprintPinReference leftPin, BlueprintPinReference rightPin)
        {
            var leftNode = EditorNodes.FirstOrDefault(n => n.Node.Guid == leftPin.NodeGuid);
            var rightNode = EditorNodes.FirstOrDefault(n => n.Node.Guid == rightPin.NodeGuid);
            if (leftNode != null && rightNode != null)
            {
                bool leftValid = leftNode.OutPorts.TryGetValue(leftPin.PinName, out var leftOutPort);
                bool rightValid = rightNode.InPorts.TryGetValue(rightPin.PinName, out var rightInPort);
                if (leftValid && rightValid)
                {
                    var edge = leftOutPort.ConnectTo(rightInPort);
                    AddEdge(edge);
                    GraphView.AddElement(edge);
                }
            }
        }
        #endregion

        public void DeselectAll()
        {
            m_SearchWindowProvider.ConnectedPort = null;
        }

        public void Select(SearcherItem searcherItem)
        {
            if (searcherItem == null)
            {
                m_SearchWindowProvider.ConnectedPort = null;
                return;
            }
            
            var tuple = ((BlueprintSearchEntry entry, Vector2 pos))searcherItem.UserData;
            var graphMousePosition = GraphView.contentViewContainer.WorldToLocal(tuple.pos);

            BlueprintNodeDataModel node = null;
            switch (tuple.entry.NodeType)
            {
                case BlueprintNodeType.Method:
                    node = BlueprintNodeDataModelUtility.CreateOrUpdateMethodNode(null, 
                        tuple.entry.MethodInfo.DeclaringType?.AssemblyQualifiedName, 
                        tuple.entry.MethodInfo.Name, 
                        tuple.entry.MethodInfo.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName).ToArray());
                    node.Position = new Rect(graphMousePosition, Vector2.zero);
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
                    var tempData = GraphObject.TempData.FirstOrDefault(x => x.FieldName == tuple.entry.NameData[0]);
                    node = BlueprintNodeDataModelUtility.CreateOrUpdateGetterNode(null, tempData);
                    node.Position = new Rect(graphMousePosition, Vector2.zero);
                    break;
                }
                case BlueprintNodeType.Setter:
                {
                    var tempData = GraphObject.TempData.FirstOrDefault(x => x.FieldName == tuple.entry.NameData[0]);
                    node = BlueprintNodeDataModelUtility.CreateOrUpdateSetterNode(null, tempData);
                    node.Position = new Rect(graphMousePosition, Vector2.zero);
                    break;
                }
                case BlueprintNodeType.Reroute:
                {
                    var type = tuple.entry.TypeData[0];
                    if (m_SearchWindowProvider.ConnectedPort != null)
                    {
                        type = m_SearchWindowProvider.ConnectedPort.Pin.Type;
                    }
                    Debug.Log($"Reroute {type}");
                    node = BlueprintNodeDataModelUtility.CreateOrUpdateRerouteNode(null, type);
                    node.Position = new Rect(graphMousePosition, Vector2.zero);
                    break;
                }
                case BlueprintNodeType.Converter:
                {
                    var names = tuple.entry.NameData;
                    node = BlueprintNodeDataModelUtility.CreateOrUpdateConverterNode(null, names[0], names[1]);
                    node.Position = new Rect(graphMousePosition, Vector2.zero);
                    break;
                }
                case BlueprintNodeType.Graph:
                {
                    var names = tuple.entry.NameData;
                    node = BlueprintNodeDataModelUtility.CreateOrUpdateGraphNode(null, names[0]);
                    node.Position = new Rect(graphMousePosition, Vector2.zero);
                    break;
                }
                case BlueprintNodeType.FieldGetter:
                {
                    node = BlueprintNodeDataModelUtility.CreateOrUpdateFieldGetterNode(null,
                        tuple.entry.FieldInfo.DeclaringType?.AssemblyQualifiedName, 
                        tuple.entry.FieldInfo.Name);
                    node.Position = new Rect(graphMousePosition, Vector2.zero);
                    break;
                }
                case BlueprintNodeType.FieldSetter:
                {
                    node = BlueprintNodeDataModelUtility.CreateOrUpdateFieldSetterNode(null,
                        tuple.entry.FieldInfo.DeclaringType?.AssemblyQualifiedName, 
                        tuple.entry.FieldInfo.Name);
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
                    firstGoodPort = last.OutPorts.Values.FirstOrDefault(p => GraphView.IsCompatiblePort(m_SearchWindowProvider.ConnectedPort, p));
                }
                else
                {
                    firstGoodPort = last.InPorts.Values.FirstOrDefault(p => GraphView.IsCompatiblePort(m_SearchWindowProvider.ConnectedPort, p));
                }
                if (firstGoodPort == null)
                {
                    return;
                }
                
                var e = m_SearchWindowProvider.ConnectedPort.ConnectTo(firstGoodPort);
                AddEdge(e);
                GraphView.AddElement(e);
            }
            m_SearchWindowProvider.ConnectedPort = null;
        }

        public bool CanConvert(Type source, Type target)
        {
            return _canConvertMap.TryGetValue(source, out var map) && map.ContainsKey(target);
        }

        public bool TryGetConvertMethod(Type outputSlotType, Type inputSlotType, out MethodInfo methodInfo)
        {
            methodInfo = null;
            return _canConvertMap.TryGetValue(outputSlotType, out var map) && map.TryGetValue(inputSlotType, out methodInfo);
        }
    }
}
