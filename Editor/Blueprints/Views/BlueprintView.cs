using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Blueprints;
using Vapor.Inspector;

namespace VaporEditor.Blueprints
{
    public class BlueprintView : GraphView, IDisposable
    {
        // public BlueprintEditorView View { get; }
        public BlueprintEditorWindow Window { get; set; }
        public BlueprintGraphSo GraphObject { get; }
        public List<IBlueprintEditorNode> EditorNodes { get; } = new();
        public string AssetName { get; set; }

        // Elements
        private VisualElement _toolbar;
        
        // Fields
        private readonly Dictionary<Type, Dictionary<Type, MethodInfo>> _canConvertMap = new();
        
        public BlueprintView(BlueprintEditorWindow window, BlueprintGraphSo graphObject)
        {
            Window = window;
            GraphObject = graphObject;
            name = "BlueprintView";

            SetupZoom(0.125f, 8);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());
            
            focusable = true;

            this.AddStylesheetFromResourcePath("Styles/BlueprintView");
            this.AddStylesheetFromResourcePath(!EditorGUIUtility.isProSkin ? "Styles/BlueprintView-light" : "Styles/BlueprintView-dark");

            // AddLayer(-1);
            // AddLayer(1);
            // AddLayer(2);
            
            CreateToolbar();
            Load();
            
            
            // Setup Callbacks
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            nodeCreationRequest = NodeCreationRequest;
            graphViewChanged = OnGraphViewChanged;
            
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
        }

        public void Dispose()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            nodeCreationRequest = null;
            graphViewChanged = null;
            UnregisterCallback<AttachToPanelEvent>(OnEnterPanel);
            UnregisterCallback<DetachFromPanelEvent>(OnLeavePanel);
        }

        #region - Initialize -

        private void CreateToolbar()
        {
            _toolbar = new Toolbar();
            var saveButton = new ToolbarButton(Window.SaveAsset)
            {
                tooltip = "Save"
            };
            saveButton.Add(new Image { image = EditorGUIUtility.IconContent("SaveActive").image });
            _toolbar.Add(saveButton);
            
            var compileButton = new ToolbarButton(Window.CompileAsset)
            {
                tooltip = "Compile"
            };
            compileButton.Add(new Image { image = Resources.Load<Sprite>("BlueprintIcons/d_compile").texture });
            _toolbar.Add(compileButton);
            
            var showButton = new ToolbarButton(Window.PingAsset)
            {
                tooltip = "Pings the asset in the folder hierarchy"
            };
            showButton.Add(new Image { image = EditorGUIUtility.IconContent("d_scenepicking_pickable_hover").image });
            _toolbar.Add(showButton);
            
            _toolbar.Add(new ToolbarSpacer
            {
                style =
                {
                    flexGrow = 1f
                }
            });
            
            var fullscreenToggle = new ToolbarToggle()
            {
                tooltip = "Maximize Window (Must be docked)"
            };
            fullscreenToggle.Add(new Image { image = EditorGUIUtility.IconContent("d_Fullscreen").image });
            fullscreenToggle.RegisterValueChangedCallback(evt =>
            {
                fullscreenToggle.tooltip = evt.newValue ? "Minimize Window (Must be docked)" : "Maximize Window (Must be docked)";
                fullscreenToggle.Q<Image>().image = evt.newValue ? EditorGUIUtility.IconContent("d_FullscreenOn").image : EditorGUIUtility.IconContent("d_Fullscreen").image;
            });
            _toolbar.Add(fullscreenToggle);
            
            var debugToggle = new ToolbarToggle()
            {
                tooltip = "Toggle Debug On"
            };
            debugToggle.Add(new Image { image = EditorGUIUtility.IconContent("debug").image });
            debugToggle.RegisterValueChangedCallback(evt =>
            {
                debugToggle.tooltip = evt.newValue ? "Toggle Debug Off" : "Toggle Debug On";
                debugToggle.Q<Image>().image = evt.newValue ? EditorGUIUtility.IconContent("debug On").image : EditorGUIUtility.IconContent("debug").image;
            });
            _toolbar.Add(debugToggle);
            
            var mockEvalButton = new ToolbarButton(MockEvaluate)
            {
                tooltip = "Mock Evaluate"
            };
            mockEvalButton.Add(new Image { image = Resources.Load<Sprite>("BlueprintIcons/prompt").texture });
            _toolbar.Add(mockEvalButton);
        }


        private void Load()
        {
            // Setup Converters
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
            
            // Loading Nodes
            foreach (var node in GraphObject.BlueprintNodes)
            {
                BlueprintNodeDrawerUtility.AddNode(node, this, EditorNodes, null);
            }
            
            // Loading Edges
            foreach (var rightNode in EditorNodes)
            {
                var wireReferences = rightNode.Model.InEdges;
                foreach (var wireReference in wireReferences)
                {
                    var leftNode = EditorNodes.FirstOrDefault(iNode => wireReference.LeftSidePin.NodeGuid == iNode.Model.Guid);
                    if (leftNode != null)
                    {
                        // Get Connected Pins
                        if(leftNode.OutPorts.TryGetValue(wireReference.LeftSidePin.PinName, out var leftPort) && rightNode.InPorts.TryGetValue(wireReference.RightSidePin.PinName, out var rightPort))
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
                                        $"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(Load))} - A port already has a connection. {wireReference.LeftSidePin.PinName}: {leftPort.connected} -> {wireReference.RightSidePin.PinName} {rightPort.connected}");
                                    continue;
                                }
                            }
                            
                            if(rightPort.connected && !rightPin.IsExecutePin)
                            {
                                // Break if right port is connected and it is not an execute pin.
                                // Right ports can only have one input value wire.
                                Debug.LogWarning($"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(Load))} - A port already has a connection. {wireReference.LeftSidePin.PinName}: {leftPort.connected} -> {wireReference.RightSidePin.PinName} {rightPort.connected}");
                                continue;
                            }

                            CreateEdge(leftPort, rightPort, false);
                        }
                        else
                        {
                            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(Load))} - Could Not Connect {wireReference.LeftSidePin.PinName} -> {wireReference.RightSidePin.PinName}");
                        }                        
                    }
                    else
                    {
                        Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(Load))} - Could Not Find Output Node {wireReference.LeftSidePin.PinName} -> {wireReference.RightSidePin.PinName}");
                    }
                }
            }
        }
        #endregion

        #region - Event Callbacks -
        private void OnPlayModeStateChanged(PlayModeStateChange playModeState)
        {
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(OnPlayModeStateChanged))} - EnteredState:{playModeState}");
        }
        
        private void NodeCreationRequest(NodeCreationContext ctx)
        {
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(NodeCreationRequest))} - Target [{ctx.target}]");
            if (EditorWindow.focusedWindow != Window)
            {
                return;
            }
            
            var viewPosition = ctx.screenMousePosition - Window.position.position;
            BlueprintSearchWindow.Show(viewPosition, ctx.screenMousePosition, new DefaultSearchProvider(OnSpawnNode).WithGraph(GraphObject));
        }
        
        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            // Move
            if (graphViewChange.movedElements != null)
            {
                Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(OnGraphViewChanged))} - Moved Elements [{graphViewChange.movedElements.Count}]");
                foreach (var element in graphViewChange.movedElements)
                {
                    if (element is IBlueprintEditorNode node)
                    {
                        node.Model.Position = element.parent.ChangeCoordinatesTo(contentViewContainer, element.GetPosition());
                    }
                }
            }
            
            // Remove
            if (graphViewChange.elementsToRemove != null)
            {
                Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(OnGraphViewChanged))} - Removed Elements [{graphViewChange.elementsToRemove.Count}]");
                foreach (var edge in graphViewChange.elementsToRemove.OfType<Edge>())
                {
                    RemoveEdge(edge);
                }
                
                foreach (var node in graphViewChange.elementsToRemove.OfType<IBlueprintEditorNode>())
                {
                    EditorNodes.Remove(node);
                    GraphObject.BlueprintNodes.Remove(node.Model);
                }
            }
            
            Window.MarkDirty();
            return graphViewChange;
        }
        
        private void OnEnterPanel(AttachToPanelEvent evt)
        {
            parent.Insert(0, _toolbar);
            
        }

        private void OnLeavePanel(DetachFromPanelEvent evt)
        {
            
        }
        #endregion

        #region - Nodes -
        public void OnSpawnNode(BlueprintSearchModel model, Vector2 position)
        {
            if (model == null)
            {
                return;
            }

            position = contentViewContainer.WorldToLocal(position);
            var type = (INodeType)Activator.CreateInstance(model.ModelType);
            var node = type.CreateDataModel(position, model.Parameters);

            if (node == null)
            {
                return;
            }
            
            // Validate and Add The Node
            node.Validate();
            CreateNodeFromModel(node);

            var portIdx = model.Parameters.FindIndex(t => t.Item1 == INodeType.PORT_PARAM);
            if (portIdx == -1)
            {
                return;
            }
            
            // Auto-Create A Valid Edge If Possible
            var port = (BlueprintEditorPort)model.Parameters[portIdx].Item2;
            var last = EditorNodes[^1];
            if (port.direction == Direction.Input)
            {
                var firstGoodPort = last.OutPorts.Values.FirstOrDefault(p => IsCompatiblePort(port, p));
                if (firstGoodPort == null)
                {
                    return;
                }

                CreateEdge(firstGoodPort, port, true);
            }
            else
            {
                var firstGoodPort = last.InPorts.Values.FirstOrDefault(p => IsCompatiblePort(port, p));
                if (firstGoodPort == null)
                {
                    return;
                }
                        
                CreateEdge(port, firstGoodPort, true);
            }
        }
        
        public bool OnSpawnNodeDirect<T>(Vector2 position, params ValueTuple<string, object>[] parameters) where T : INodeType
        {
            position = contentViewContainer.WorldToLocal(position);
            var type = (INodeType)Activator.CreateInstance(typeof(T));
            var node = type.CreateDataModel(position, parameters.ToList());

            if (node != null)
            {
                node.Validate();
                CreateNodeFromModel(node);
                return true;
            }

            return false;
        }
        
        public void CreateNodeFromModel(BlueprintNodeDataModel node)
        {
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(CreateNodeFromModel))} - {node.GetType().Name}");
            BlueprintNodeDrawerUtility.AddNode(node, this, EditorNodes, GraphObject.BlueprintNodes);
        }
        
        public void CreateRedirectNode(Vector2 pos, Edge edgeTarget)
        {
            var outputPort = (BlueprintEditorPort)edgeTarget.output;
            var inputPort = (BlueprintEditorPort)edgeTarget.input;
            
            var outputSlot = edgeTarget.output.GetPin();
            var inputSlot = edgeTarget.input.GetPin();
            
            // var sr = new SearcherItem("");
            // var bse = new BlueprintSearchEntry.Builder().WithFullName("Reroute").WithNodeType(BlueprintNodeType.Reroute).WithTypes(inputSlot.Type).Build();
            // sr.UserData = (bse, pos);
            // View.Select(sr);

            if (!OnSpawnNodeDirect<RerouteNodeType>(pos, (INodeType.CONNECTION_TYPE_PARAM, inputSlot.Type)))
            {
                // Exit Out Failed To Add
                DeleteElements(new[] { edgeTarget });
                return;
            }

            var reroute = EditorNodes[^1];
            var leftPortRef = new BlueprintPinReference(outputSlot.PortName, outputPort.Node.Model.Guid, outputSlot.IsExecutePin);
            var rightPortRef = new BlueprintPinReference(inputSlot.PortName, inputPort.Node.Model.Guid, inputSlot.IsExecutePin);

            var inRerouteRef = new BlueprintPinReference(PinNames.EXECUTE_IN, reroute.Model.Guid, outputSlot.IsExecutePin);
            var outRerouteRef = new BlueprintPinReference(PinNames.EXECUTE_OUT, reroute.Model.Guid, outputSlot.IsExecutePin);
            DeleteElements(new[] { edgeTarget });
            ConnectPins(leftPortRef, inRerouteRef);
            ConnectPins(outRerouteRef, rightPortRef);
        }

        public void CreateConverterNode(Edge edgeTarget)
        {
            var outputPort = (BlueprintEditorPort)edgeTarget.output;
            var inputPort = (BlueprintEditorPort)edgeTarget.input;
            
            var outputSlot = edgeTarget.output.GetPin();
            var inputSlot = edgeTarget.input.GetPin();

            if (!TryGetConvertMethod(outputSlot.Type, inputSlot.Type, out MethodInfo convertMethod))
            {
                DeleteElements(new[] { edgeTarget });
                return;
            }

            // var assemblyName = convertMethod.DeclaringType?.AssemblyQualifiedName;
            // var methodName = convertMethod.Name;
            // var sr = new SearcherItem("");
            // var bse = new BlueprintSearchEntry.Builder().WithFullName("Converter").WithNodeType(BlueprintNodeType.Converter).WithNameData(assemblyName, methodName).Build();
            // sr.UserData = (bse, pos);
            // View.Select(sr);
            
            var pos = Vector2.Lerp(outputPort.worldBound.center, inputPort.worldBound.center, 0.5f) + new Vector2(0, -12);
            if (!OnSpawnNodeDirect<ConverterNodeType>(pos, (INodeType.METHOD_INFO_PARAM, convertMethod)))
            {
                // Exit Out Failed To Add
                DeleteElements(new[] { edgeTarget });
                return;
            }
            

            var converter = EditorNodes[^1];
            var leftPortRef = new BlueprintPinReference(outputSlot.PortName, outputPort.Node.Model.Guid, outputSlot.IsExecutePin);
            var rightPortRef = new BlueprintPinReference(inputSlot.PortName, inputPort.Node.Model.Guid, inputSlot.IsExecutePin);

            var inRerouteRef = new BlueprintPinReference(PinNames.EXECUTE_IN, converter.Model.Guid, outputSlot.IsExecutePin);
            var outRerouteRef = new BlueprintPinReference(PinNames.RETURN, converter.Model.Guid, outputSlot.IsExecutePin);
            DeleteElements(new[] { edgeTarget });
            ConnectPins(leftPortRef, inRerouteRef);
            ConnectPins(outRerouteRef, rightPortRef);
        }
        #endregion

        #region - Pins -
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var allPorts = new List<BlueprintEditorPort>();
            var validPorts = new List<Port>();

            if (startPort is not BlueprintEditorPort startEditorPort)
            {
                return validPorts;
            }

            foreach (var node in EditorNodes)
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

            foreach (var endPort in allPorts)
            {
                if (endPort == startPort) { continue; }
                if (endPort.node == startPort.node) { continue; }
                if(IsCompatiblePort(startEditorPort, endPort))
                {
                    validPorts.Add(endPort);
                }
            }

            return validPorts;
        }

        public bool IsCompatiblePort(Port startPort, Port endPort)
        {
            if (endPort.portType == startPort.portType)
            {
                return true;
            }

            if (endPort.portType ==  typeof(object) && startPort.portType != typeof(ExecutePin))
            {
                return true;
            }

            if (CanConvert(startPort.portType, endPort.portType))
            {
                return true;
            }
            return false;
        }

        #endregion

        #region - Edges -
        public void CreateEdge(BlueprintEditorPort leftPort, BlueprintEditorPort rightPort, bool shouldModifyDataModel)
        {
            var edge = leftPort.ConnectTo(rightPort);

            if (shouldModifyDataModel)
            {
                leftPort.Node.Model.OutEdges.Add(new BlueprintWireReference(
                    new BlueprintPinReference(leftPort.Pin.PortName, leftPort.Node.Model.Guid, leftPort.Pin.IsExecutePin),
                    new BlueprintPinReference(rightPort.Pin.PortName, rightPort.Node.Model.Guid, rightPort.Pin.IsExecutePin)));
                rightPort.Node.Model.InEdges.Add(new BlueprintWireReference(
                    new BlueprintPinReference(leftPort.Pin.PortName, leftPort.Node.Model.Guid, leftPort.Pin.IsExecutePin),
                    new BlueprintPinReference(rightPort.Pin.PortName, rightPort.Node.Model.Guid, rightPort.Pin.IsExecutePin)));
            }

            rightPort.Node.OnConnectedInputEdge(rightPort.Pin.PortName);
            edge.RegisterCallback<MouseDownEvent>(OnEdgeMouseDown);
            
            AddElement(edge);
        }

        public void RemoveEdge(Edge edge)
        {
            if (edge.input != null && edge.output != null && edge.input.node is IBlueprintEditorNode rightNode && edge.output.node is IBlueprintEditorNode leftNode)
            {
                var leftModel = leftNode.Model;
                var rightModel = rightNode.Model;

                // NEW
                var rightPin = edge.input.GetPin();
                var leftPin = edge.output.GetPin();
                BlueprintWireReference edgeToMatch = new BlueprintWireReference(
                    new BlueprintPinReference(leftPin.PortName, leftModel.Guid, false),
                    new BlueprintPinReference(rightPin.PortName, rightModel.Guid, false));

                rightModel.InEdges.Remove(edgeToMatch);
                leftModel.OutEdges.Remove(edgeToMatch);

                rightNode.OnDisconnectedInputEdge(rightPin.PortName);
            }

            RemoveElement(edge);
        }

        public void ConnectPins(BlueprintPinReference leftPin, BlueprintPinReference rightPin)
        {
            var leftNode = EditorNodes.FirstOrDefault(n => n.Model.Guid == leftPin.NodeGuid);
            var rightNode = EditorNodes.FirstOrDefault(n => n.Model.Guid == rightPin.NodeGuid);
            if (leftNode != null && rightNode != null)
            {
                bool leftValid = leftNode.OutPorts.TryGetValue(leftPin.PinName, out var leftOutPort);
                bool rightValid = rightNode.InPorts.TryGetValue(rightPin.PinName, out var rightInPort);
                if (leftValid && rightValid)
                {
                    CreateEdge(leftOutPort, rightInPort, true);
                }
            }
        }
        
        private void OnEdgeMouseDown(MouseDownEvent evt)
        {
            // Only Double Click
            if (evt.button != (int)MouseButton.LeftMouse || evt.clickCount != 2)
            {
                return;
            }

            // Only Edges
            if (evt.target is not Edge edgeTarget)
            {
                return;
            }

            Vector2 pos = evt.mousePosition;
            CreateRedirectNode(pos, edgeTarget);
        }
        #endregion

        #region - Debug -

        private void MockEvaluate()
        {
            
        }

        #endregion

        #region - Contextual Menu -
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            
            if (evt.target is Edge edge)
            {
                var pos = evt.mousePosition;
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Create Redirect", _ => CreateRedirectNode(pos, edge), _ => DropdownMenuAction.Status.Normal);
            }
        }
        #endregion

        #region - Helper -
        public bool CanConvert(Type source, Type target)
        {
            return _canConvertMap.TryGetValue(source, out var map) && map.ContainsKey(target);
        }

        private bool TryGetConvertMethod(Type outputSlotType, Type inputSlotType, out MethodInfo methodInfo)
        {
            methodInfo = null;
            return _canConvertMap.TryGetValue(outputSlotType, out var map) && map.TryGetValue(inputSlotType, out methodInfo);
        }
        #endregion
    }
}
