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
using ClipboardUtility = VaporEditor.Inspector.ClipboardUtility;
using Cursor = UnityEngine.UIElements.Cursor;
using DragEnterEvent = Vapor.Inspector.DragEnterEvent;

namespace VaporEditor.Blueprints
{
    public class BlueprintView : GraphView, IDisposable, IDragDropTarget
    {
        public BlueprintEditorWindow Window { get; set; }
        public BlueprintClassGraphModel GraphModelObject { get; }

        private BlueprintMethodGraph _method;
        public BlueprintMethodGraph Method
        {
            get => _method;
            set
            {
                if (_method == value) return;
                DestroyMethodListeners();
                _method = value;
                CreateMethodListeners();
            }
        }

        // public List<IBlueprintNodeView> EditorNodes { get; } = new();
        public Dictionary<string, IBlueprintNodeView> Nodes { get; } = new();
        public Dictionary<string, BlueprintWireView> Wires { get; } = new();
        public string AssetName { get; set; }
        public BlueprintBlackboardView Blackboard { get; set; }

        public override bool focusable => true;

        // Elements
        private VisualElement _toolbar;
        
        // Fields
        private readonly Dictionary<Type, Dictionary<Type, MethodInfo>> _canConvertMap = new();
        private Vector2 _lastMousePosition;

        public BlueprintView(BlueprintEditorWindow window, BlueprintClassGraphModel graphModelObject)
        {
            Window = window;
            GraphModelObject = graphModelObject;
            name = "BlueprintView";

            SetupZoom(0.125f, 8);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            this.AddStylesheetFromResourcePath("Styles/BlueprintView");
            this.AddStylesheetFromResourcePath(!EditorGUIUtility.isProSkin ? "Styles/BlueprintView-light" : "Styles/BlueprintView-dark");
            
            CreateToolbar();
            // Load();
            
            // Setup Callbacks
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            nodeCreationRequest = NodeCreationRequest;
            graphViewChanged = OnGraphViewChanged;
            
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);

            RegisterCallback<DragDropEvent>(OnDropped);
            RegisterCallback<DragEnterEvent>(OnDragEnter);
            RegisterCallback<DragExitEvent>(OnDragExit);
        }

        public void Dispose()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            nodeCreationRequest = null;
            graphViewChanged = null;
            UnregisterCallback<AttachToPanelEvent>(OnEnterPanel);
            UnregisterCallback<DetachFromPanelEvent>(OnLeavePanel);
            UnregisterCallback<ValidateCommandEvent>(OnValidateCommand);
            UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            UnregisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
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
            
            // var compileButton = new ToolbarButton(Window.CompileAsset)
            // {
            //     tooltip = "Compile"
            // };
            // compileButton.Add(new Image { image = Resources.Load<Sprite>("BlueprintIcons/d_compile").texture });
            // _toolbar.Add(compileButton);
            
            var compileToCodeButton = new ToolbarButton(Window.CompileToScript)
            {
                tooltip = "Compile",
            };
            compileToCodeButton.Add(new Image { image = Resources.Load<Sprite>("BlueprintIcons/d_compile").texture });
            _toolbar.Add(compileToCodeButton);
            
            var showButton = new ToolbarButton(Window.PingAsset)
            {
                tooltip = "Pings the asset in the folder hierarchy"
            };
            showButton.Add(new Image { image = EditorGUIUtility.IconContent("d_scenepicking_pickable_hover").image });
            _toolbar.Add(showButton);
            
            var blackBoardToggle = new ToolbarToggle()
            {
                tooltip = "Hide Blackboard"
            };
            blackBoardToggle.Add(new Image { image = Resources.Load<Sprite>("BlueprintIcons/d_variableswindow").texture });
            blackBoardToggle.RegisterValueChangedCallback(evt =>
            {
                blackBoardToggle.tooltip = evt.newValue ? "Hide Blackboard" : "Show Blackboard";
                Window.SetBlackboardVisibility(evt.newValue);
            });
            blackBoardToggle.SetValueWithoutNotify(true);
            _toolbar.Add(blackBoardToggle);
            
            
            _toolbar.Add(new ToolbarSpacer
            {
                style =
                {
                    flexGrow = 1f
                }
            });
            
            var inspectorToggle = new ToolbarToggle()
            {
                tooltip = "Hide Inspector"
            };
            inspectorToggle.Add(new Image { image = EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow").image });
            inspectorToggle.RegisterValueChangedCallback(evt =>
            {
                inspectorToggle.tooltip = evt.newValue ? "Hide Inspector" : "Show Inspector";
                Window.SetInspectorVisibility(evt.newValue);
            });
            inspectorToggle.SetValueWithoutNotify(true);
            _toolbar.Add(inspectorToggle);
            
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
        }

        private void Load()
        {
            _canConvertMap.Clear();
            foreach (var e in graphElements)
            {
                RemoveElement(e);
            }
            // EditorNodes.Clear();
            Wires.Clear();
            Nodes.Clear();
            if (Method == null)
            {
                return;
            }
            
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
            foreach (var node in Method.Nodes.Values)
            {
                var nodeView = BlueprintNodeDrawerUtility.CreateNodeView(this, node);
                Nodes.Add(node.Guid, nodeView);
                // AddElement((GraphElement)nodeView);
            }
            
            // Loading Edges
            foreach (var wire in Method.Wires.Values)
            {
                // Create The Edge
                if (!Nodes.TryGetValue(wire.LeftGuid, out var leftNode) || !Nodes.TryGetValue(wire.RightGuid, out var rightNode))
                {
                    Debug.LogError("Wire connected to invalid node views.");
                    continue;
                }
                if (!leftNode.OutPorts.TryGetValue(wire.LeftName, out var leftPinView) || !rightNode.InPorts.TryGetValue(wire.RightName, out var rightPinView))
                {
                    Debug.LogError("Wire connected to invalid port views.");
                    continue;
                }
                
                var edge = leftPinView.ConnectTo<BlueprintWireView>(rightPinView);
                edge.Init(this, wire);
                Wires.Add(wire.Guid, edge);
                AddElement(edge);
            }
            
            
            // foreach (var rightNode in EditorNodes)
            // {
            //     var wireReferences = rightNode.Controller.InputWires;
            //     foreach (var wireReference in wireReferences)
            //     {
            //         var leftNode = EditorNodes.FirstOrDefault(iNode => wireReference.LeftSidePin.NodeGuid == iNode.Controller.Guid);
            //         if (leftNode != null)
            //         {
            //             // Get Connected Pins
            //             if(leftNode.OutPorts.TryGetValue(wireReference.LeftSidePin.PinName, out var leftPort) && rightNode.InPorts.TryGetValue(wireReference.RightSidePin.PinName, out var rightPort))
            //             {
            //                 var leftPin = leftPort.GetPin();
            //                 var rightPin = rightPort.GetPin();
            //
            //                 // Left Pin Is Already Connect
            //                 if (leftPort.connected)
            //                 {
            //                     // Break if it is an execute pin or doesn't allow multiple wires.
            //                     // Executes can only have one output but multiple inputs.
            //                     if(leftPin.IsExecutePin || !leftPin.AllowMultipleWires)
            //                     {
            //                         Debug.LogWarning(
            //                             $"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(Load))} - A port already has a connection. {wireReference.LeftSidePin.PinName}: {leftPort.connected} -> {wireReference.RightSidePin.PinName} {rightPort.connected}");
            //                         continue;
            //                     }
            //                 }
            //                 
            //                 if(rightPort.connected && !rightPin.IsExecutePin)
            //                 {
            //                     // Break if right port is connected and it is not an execute pin.
            //                     // Right ports can only have one input value wire.
            //                     Debug.LogWarning($"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(Load))} - A port already has a connection. {wireReference.LeftSidePin.PinName}: {leftPort.connected} -> {wireReference.RightSidePin.PinName} {rightPort.connected}");
            //                     continue;
            //                 }
            //
            //                 CreateEdge(leftPort, rightPort, false);
            //             }
            //             else
            //             {
            //                 Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(Load))} - Could Not Connect {wireReference.LeftSidePin.PinName} -> {wireReference.RightSidePin.PinName}");
            //             }                        
            //         }
            //         else
            //         {
            //             Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(Load))} - Could Not Find Output Node {wireReference.LeftSidePin.PinName} -> {wireReference.RightSidePin.PinName}");
            //         }
            //     }
            // }
            
            schedule.Execute(_ => FrameAll()).ExecuteLater(100);
        }

        private void Reload(BlueprintClassGraphModel classGraph, BlueprintMethodGraph methodGraph)
        {
            Method = methodGraph;
            Load();
            Window.GraphObject.LastOpenedMethod = Method?.MethodName;
        }

        private void CreateMethodListeners()
        {
            if (Method == null)
            {
                return;
            }

            Method.VariableChanged += OnVariableChanged;
            Method.WireChanged += OnWireChanged;
            Method.NodeChanged += OnNodeChanged;
        }

        private void DestroyMethodListeners()
        {
            if (Method == null)
            {
                return;
            }

            Method.VariableChanged -= OnVariableChanged;
            Method.WireChanged -= OnWireChanged;
            Method.NodeChanged -= OnNodeChanged;
        }

        #endregion

        #region - Event Callbacks -
        private void OnVariableChanged(BlueprintMethodGraph method, BlueprintVariable variable, ChangeType changeType, bool ignoreUndo)
        {
            
        }

        private void OnWireChanged(BlueprintMethodGraph method, BlueprintWire wire, ChangeType changeType, bool ignoreUndo)
        {
            switch (changeType)
            {
                case ChangeType.Added:
                {
                    // Create The Edge
                    if (!Nodes.TryGetValue(wire.LeftGuid, out var leftNode) || !Nodes.TryGetValue(wire.RightGuid, out var rightNode))
                    {
                        Debug.LogError("Wire connected to invalid node views.");
                        return;
                    }
                    if (!leftNode.OutPorts.TryGetValue(wire.LeftName, out var leftPinView) || !rightNode.InPorts.TryGetValue(wire.RightName, out var rightPinView))
                    {
                        Debug.LogError("Wire connected to invalid port views.");
                        return;
                    }
                    
                    var edge = leftPinView.ConnectTo<BlueprintWireView>(rightPinView);
                    edge.Init(this, wire);
                    Wires.Add(wire.Guid, edge);
                    AddElement(edge);
                    break;
                }
                case ChangeType.Removed:
                {
                    // Delete The Edge
                    if (Wires.Remove(wire.Guid, out var wireView))
                    {
                        RemoveElement(wireView);
                    }

                    break;
                }
                case ChangeType.Modified:
                    // Check and Change Edge Connections
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null);
            }
            Window.MarkDirty();
        }

        private void OnNodeChanged(BlueprintMethodGraph method, NodeModelBase node, ChangeType changeType, bool ignoreUndo)
        {
            switch (changeType)
            {
                case ChangeType.Added:
                {
                    var nodeView = BlueprintNodeDrawerUtility.CreateNodeView(this, node);
                    Nodes.Add(node.Guid, nodeView);
                    // AddElement((GraphElement)nodeView);
                    break;
                }
                case ChangeType.Removed:
                {
                    if (Nodes.Remove(node.Guid, out var nodeView))
                    {
                        RemoveElement((GraphElement)nodeView);
                    }
                    break;
                }
                case ChangeType.Modified:
                {
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null);
            }
            Window.MarkDirty();
        }
        
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

            if (Method == null)
            {
                return;
            }

            var viewPosition = ctx.screenMousePosition - Window.position.position;
            BlueprintSearchWindow.Show(viewPosition, ctx.screenMousePosition, new DefaultSearchProvider(OnSpawnNode, Window.SearchModels).WithGraph(GraphModelObject), true, false);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            // Move
            if (graphViewChange.movedElements != null)
            {
                Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(OnGraphViewChanged))} - Moved Elements [{graphViewChange.movedElements.Count}]");
                foreach (var element in graphViewChange.movedElements)
                {
                    if (element is IBlueprintNodeView node)
                    {
                        node.Controller.Position = element.parent.ChangeCoordinatesTo(contentViewContainer, element.GetPosition());
                    }
                }
            }
            
            // Remove
            // if (graphViewChange.elementsToRemove != null)
            // {
            //     Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(OnGraphViewChanged))} - Removed Elements [{graphViewChange.elementsToRemove.Count}]");
            //     foreach (var edge in graphViewChange.elementsToRemove.OfType<Edge>())
            //     {
            //         RemoveEdge(edge);
            //     }
            //     
            //     foreach (var node in graphViewChange.elementsToRemove.OfType<IBlueprintNodeView>())
            //     {
            //         Nodes.Remove(node.Controller.Guid);
            //         GraphModelObject.Current.Nodes.Remove(node.Controller.Guid);
            //     }
            // }
            
            Window.MarkDirty();
            return graphViewChange;
        }
        
        private void OnEnterPanel(AttachToPanelEvent evt)
        {
            // First Parent Is Split View
            Window.SplitView.parent.Insert(0, _toolbar);
            GraphModelObject.MethodOpened += Reload;
        }

        private void OnLeavePanel(DetachFromPanelEvent evt)
        {
            GraphModelObject.MethodOpened -= Reload;
        }
        
        private void OnMouseMoveEvent(MouseMoveEvent evt)
        {
            _lastMousePosition = evt.mousePosition;
        }

        private void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
            {
                return;
            }

            if (evt.commandName == "Copy" && canCopySelection 
                || evt.commandName == "Paste" && BlueprintClipboardUtility.CanPaste 
                || evt.commandName == "Duplicate" && canDuplicateSelection 
                || evt.commandName == "Cut" && canCutSelection 
                || evt.commandName is "Delete" or "SoftDelete" && canDeleteSelection)
            {
                evt.StopPropagation();
                if (evt.imguiEvent == null)
                {
                    return;
                }

                evt.imguiEvent.Use();
            }
            else
            {
                if (evt.commandName != "FrameSelected")
                {
                    return;
                }

                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
        }

        private void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
            {
                return;
            }

            if (evt.commandName == "Copy")
            {
                var toCopy = selection.OfType<GraphElement>();
                BlueprintClipboardUtility.Copy(toCopy);
                evt.StopPropagation();
            }
            else if (evt.commandName == "Paste")
            {
                BlueprintClipboardUtility.Paste(this, contentViewContainer.WorldToLocal(_lastMousePosition));
                evt.StopPropagation();
            }
            else if (evt.commandName == "Duplicate")
            {
                var toCopy = selection.OfType<GraphElement>();
                BlueprintClipboardUtility.Duplicate(toCopy,this, contentViewContainer.WorldToLocal(_lastMousePosition));
                evt.StopPropagation();
            }
            else if (evt.commandName == "Cut")
            {
                var toCopy = selection.OfType<GraphElement>();
                BlueprintClipboardUtility.Cut(toCopy);
                evt.StopPropagation();
            }
            else if (evt.commandName == "Delete")
            {
                DeleteSelectionCallback(AskUser.DontAskUser);
                evt.StopPropagation();
            }
            else if (evt.commandName == "SoftDelete")
            {
                DeleteSelectionCallback(AskUser.AskUser);
                evt.StopPropagation();
            }
            else if (evt.commandName == "FrameSelected")
            {
                int num = (int) FrameSelection();
                evt.StopPropagation();
            }
            if (!evt.isPropagationStopped || evt.imguiEvent == null)
            {
                return;
            }

            evt.imguiEvent.Use();
        }

        private void OnDropped(DragDropEvent evt)
        {
            if (evt.source is BlueprintBlackboardVariable v)
            {
                if (evt.heldKeys.Contains(KeyCode.G))
                {
                    var msd = v.Model.GetMemberSearchData(VariableAccessType.Get);
                    OnSpawnNodeDirect(NodeType.MemberAccess, evt.dropWorldPosition, msd);
                }
                else if (evt.heldKeys.Contains(KeyCode.S))
                {
                    var msd = v.Model.GetMemberSearchData(VariableAccessType.Set);
                    OnSpawnNodeDirect(NodeType.MemberAccess, evt.dropWorldPosition, msd);
                }
                else
                {
                    // Get Set Window
                    var screenPosition = evt.dropWorldPosition + Window.position.position;
                    BlueprintSearchWindow.Show(evt.dropWorldPosition, screenPosition, new MemberOnlySearchProvider(v.Model, OnSpawnNode, GraphModelObject), false, true);
                }
            }
        }

        private void OnDragEnter(DragEnterEvent evt)
        {
            Debug.Log("OnDragEnter!");
        }

        private void OnDragExit(DragExitEvent evt)
        {
            Debug.Log("OnDragExit!");
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
            // model.TryGetParameter<NodeType>(SearchModelParams.NODE_TYPE_PARAM, out var nodeType);
            var node = Method.AddNode(model.NodeType, position, model.UserData);
            // var controller = NodeFactory.Build(nodeType, position, GraphModelObject.Current, model.Parameters.ToArray());
            
            // var type = (INodeType)Activator.CreateInstance(model.ModelType);
            // var node = type.CreateDesignNode(position, model.Parameters);

            // if (controller == null)
            // {
                // return;
            // }
            
            // Validate and Add The Node
            // node.Validate();
            // CreateModelForController(controller);

            // var portIdx = model.Parameters.FindIndex(t => t.Item1 == SearchModelParams.PORT_PARAM);
            if (model.PortView == null)
            {
                return;
            }
            
            // Auto-Create A Valid Edge If Possible
            var port = model.PortView;
            var last = Nodes[node.Guid];
            if (port.direction == Direction.Input)
            {
                var firstGoodPort = last.OutPorts.Values.FirstOrDefault(p => IsCompatiblePort(port, p));
                if (firstGoodPort == null)
                {
                    return;
                }

                Method.AddWire(firstGoodPort.GetPin(), port.GetPin());
                // CreateEdge(firstGoodPort, port, true);
            }
            else
            {
                var firstGoodPort = last.InPorts.Values.FirstOrDefault(p => IsCompatiblePort(port, p));
                if (firstGoodPort == null)
                {
                    return;
                }
                        
                Method.AddWire(port.GetPin(), firstGoodPort.GetPin());
                // CreateEdge(port, firstGoodPort, true);
            }
        }
        
        public NodeModelBase OnSpawnNodeDirect(NodeType nodeType, Vector2 position, object suppliedUserData)
        {
            position = contentViewContainer.WorldToLocal(position);
            return Method.AddNode(nodeType, position, suppliedUserData);
        }
        
        public void CreateRedirectNode(Vector2 pos, BlueprintWireView edgeTarget)
        {
            var outputPort = (BlueprintPortView)edgeTarget.output;
            var inputPort = (BlueprintPortView)edgeTarget.input;
            
            var outputSlot = edgeTarget.output.GetPin();
            var inputSlot = edgeTarget.input.GetPin();
            bool isExecute = outputSlot.IsExecutePin;
            
            // var sr = new SearcherItem("");
            // var bse = new BlueprintSearchEntry.Builder().WithFullName("Reroute").WithNodeType(BlueprintNodeType.Reroute).WithTypes(inputSlot.Type).Build();
            // sr.UserData = (bse, pos);
            // View.Select(sr);
            var node = OnSpawnNodeDirect(NodeType.Redirect, pos, inputSlot.Type);
            /*if (!)
            {
                // Exit Out Failed To Add
                DeleteElements(new[] { edgeTarget });
                return;
            }*/

            var reroute = Nodes[node.Guid];
            Method.AddWire(outputSlot, reroute.InPorts[isExecute ? PinNames.EXECUTE_IN : PinNames.SET_IN].GetPin());
            Method.AddWire(reroute.OutPorts[isExecute ? PinNames.EXECUTE_OUT : PinNames.GET_OUT].GetPin(), inputSlot);
            
            // var leftPortRef = new BlueprintPinReference(outputSlot.PortName, outputPort.NodeView.Controller.Guid, outputSlot.IsExecutePin);
            // var rightPortRef = new BlueprintPinReference(inputSlot.PortName, inputPort.NodeView.Controller.Guid, inputSlot.IsExecutePin);

            // var inRerouteRef = new BlueprintPinReference(outputSlot.IsExecutePin ? PinNames.EXECUTE_IN : PinNames.SET_IN, reroute.Controller.Guid, outputSlot.IsExecutePin);
            // var outRerouteRef = new BlueprintPinReference(outputSlot.IsExecutePin ? PinNames.EXECUTE_OUT : PinNames.GET_OUT, reroute.Controller.Guid, outputSlot.IsExecutePin);
            edgeTarget.Delete();
            // DeleteElements(new[] { edgeTarget });
            // ConnectPins(leftPortRef, inRerouteRef);
            // ConnectPins(outRerouteRef, rightPortRef);
        }

        public void CreateConverterNode(Edge edgeTarget)
        {
            var outputPort = (BlueprintPortView)edgeTarget.output;
            var inputPort = (BlueprintPortView)edgeTarget.input;
            
            var outputSlot = edgeTarget.output.GetPin();
            var inputSlot = edgeTarget.input.GetPin();

            // if (!TryGetConvertMethod(outputSlot.Type, inputSlot.Type, out MethodInfo convertMethod))
            // {
            //     DeleteElements(new[] { edgeTarget });
            //     return;
            // }

            // var assemblyName = convertMethod.DeclaringType?.AssemblyQualifiedName;
            // var methodName = convertMethod.Name;
            // var sr = new SearcherItem("");
            // var bse = new BlueprintSearchEntry.Builder().WithFullName("Converter").WithNodeType(BlueprintNodeType.Converter).WithNameData(assemblyName, methodName).Build();
            // sr.UserData = (bse, pos);
            // View.Select(sr);
            
            var pos = Vector2.Lerp(outputPort.worldBound.center, inputPort.worldBound.center, 0.5f) + new Vector2(0, -12);
            var node = OnSpawnNodeDirect(NodeType.Conversion, pos, (outputSlot.Type, inputSlot.Type));
            // if (!)
            // {
                // Exit Out Failed To Add
                // DeleteElements(new[] { edgeTarget });
                // return;
            // }
            

            var converter = Nodes[node.Guid];
            Method.AddWire(outputSlot, converter.InPorts[PinNames.SET_IN].GetPin());
            Method.AddWire(converter.OutPorts[PinNames.GET_OUT].GetPin(), inputSlot);
            edgeTarget.RemoveFromHierarchy();
            
            // var leftPortRef = new BlueprintPinReference(outputSlot.PortName, outputPort.NodeView.Controller.Guid, outputSlot.IsExecutePin);
            // var rightPortRef = new BlueprintPinReference(inputSlot.PortName, inputPort.NodeView.Controller.Guid, inputSlot.IsExecutePin);

            // var inRerouteRef = new BlueprintPinReference(PinNames.SET_IN, converter.Controller.Guid, outputSlot.IsExecutePin);
            // var outRerouteRef = new BlueprintPinReference(PinNames.GET_OUT, converter.Controller.Guid, outputSlot.IsExecutePin);
            // DeleteElements(new[] { edgeTarget });
            // ConnectPins(leftPortRef, inRerouteRef);
            // ConnectPins(outRerouteRef, rightPortRef);
        }
        
        #endregion

        #region - Pins -
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var allPorts = new List<BlueprintPortView>();
            var validPorts = new List<Port>();

            if (startPort is not BlueprintPortView startEditorPort)
            {
                return validPorts;
            }

            foreach (var node in Nodes.Values)
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

        public static bool IsCompatiblePort(Port startPort, Port endPort)
        {
            if (endPort.portType == startPort.portType)
            {
                return true;
            }

            if (endPort.portType.IsAssignableFrom(startPort.portType))
            {
                return true;
            }

            if (endPort.portType == typeof(object) && startPort.portType != typeof(ExecutePin))
            {
                return true;
            }

            if (endPort.portType == typeof(EnumPin) && startPort.portType.IsEnum)
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
        public void Reconnect(BlueprintPortView blueprintPortView)
        {
            var oldWires = new List<BlueprintWire>();
            foreach (var wire in Method.Wires.Values)
            {
                // Create The Edge
                if (!Nodes.TryGetValue(wire.LeftGuid, out var leftNode) || !Nodes.TryGetValue(wire.RightGuid, out var rightNode))
                {
                    oldWires.Add(wire);
                    Debug.LogError("Wire connected to invalid node views.");
                    continue;
                }
                if (!leftNode.OutPorts.TryGetValue(wire.LeftName, out var leftPinView) || !rightNode.InPorts.TryGetValue(wire.RightName, out var rightPinView))
                {
                    oldWires.Add(wire);
                    Debug.LogError($"Wire connected to invalid port views. {wire.LeftName} - {wire.RightName}");
                    continue;
                }

                if (leftPinView == blueprintPortView)
                {
                    var wireView = Wires[wire.Guid];
                    wireView.output = leftPinView;
                    leftPinView.Connect(wireView);
                }

                if (rightPinView == blueprintPortView)
                {
                    var wireView = Wires[wire.Guid];
                    wireView.input = rightPinView;
                    rightPinView.Connect(wireView);
                }
            }

            foreach (var ow in oldWires)
            {
                ow.Delete();
            }
        }
        
        public void RemoveEdge(Edge edge)
        {
            if (edge.input != null && edge.output != null && edge.input.node is IBlueprintNodeView rightNode && edge.output.node is IBlueprintNodeView leftNode)
            {
                var leftModel = leftNode.Controller;
                var rightModel = rightNode.Controller;

                // NEW
                var rightPin = edge.input.GetPin();
                var leftPin = edge.output.GetPin();
                BlueprintWireReference edgeToMatch = new BlueprintWireReference(
                    new BlueprintPinReference(leftPin.PortName, leftModel.Guid, false),
                    new BlueprintPinReference(rightPin.PortName, rightModel.Guid, false));

                // rightModel.InputWires.Remove(edgeToMatch);
                // leftModel.OutputWires.Remove(edgeToMatch);

                // rightNode.OnDisconnectedInputEdge(rightPin.PortName);
            }

            RemoveElement(edge);
        }

        public void ConnectPins(BlueprintPinReference leftPin, BlueprintPinReference rightPin)
        {
            var leftNode = Nodes.Values.FirstOrDefault(n => n.Controller.Guid == leftPin.NodeGuid);
            var rightNode = Nodes.Values.FirstOrDefault(n => n.Controller.Guid == rightPin.NodeGuid);
            if (leftNode != null && rightNode != null)
            {
                bool leftValid = leftNode.OutPorts.TryGetValue(leftPin.PinName, out var leftOutPort);
                bool rightValid = rightNode.InPorts.TryGetValue(rightPin.PinName, out var rightInPort);
                if (leftValid && rightValid)
                {
                    Method.AddWire(leftOutPort.GetPin(), rightInPort.GetPin());
                    // CreateEdge(leftOutPort, rightInPort, true);
                }
            }
        }

        public void RebuildEdgesOnNode(IBlueprintNodeView nodeToRebuild)
        {
            // Rebuild Inputs
            _CreateEdgesFromRightNode(nodeToRebuild);

            // Rebuild Outputs
            // foreach (var rightNode in nodeToRebuild.Controller.OutputWires.Select(wire => Nodes.Values.FirstOrDefault(iNode => wire.RightSidePin.NodeGuid == iNode.Controller.Guid)))
            // {
            //     _CreateEdgesFromRightNode(rightNode);
            // }

            void _CreateEdgesFromRightNode(IBlueprintNodeView nodeView)
            {
                // var wireReferences = nodeView.Controller.InputWires;
                // foreach (var wireReference in wireReferences)
                // {
                //     var leftNode = EditorNodes.FirstOrDefault(iNode => wireReference.LeftSidePin.NodeGuid == iNode.Controller.Guid);
                //     if (leftNode != null)
                //     {
                //         // Get Connected Pins
                //         if (leftNode.OutPorts.TryGetValue(wireReference.LeftSidePin.PinName, out var leftPort) && nodeView.InPorts.TryGetValue(wireReference.RightSidePin.PinName, out var rightPort))
                //         {
                //             var leftPin = leftPort.GetPin();
                //             var rightPin = rightPort.GetPin();
                //
                //             // Left Pin Is Already Connect
                //             if (leftPort.connected)
                //             {
                //                 // Break if it is an execute pin or doesn't allow multiple wires.
                //                 // Executes can only have one output but multiple inputs.
                //                 if (leftPin.IsExecutePin || !leftPin.AllowMultipleWires)
                //                 {
                //                     Debug.LogWarning(
                //                         $"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(Load))} - A port already has a connection. {wireReference.LeftSidePin.PinName}: {leftPort.connected} -> {wireReference.RightSidePin.PinName} {rightPort.connected}");
                //                     continue;
                //                 }
                //             }
                //
                //             if (rightPort.connected && !rightPin.IsExecutePin)
                //             {
                //                 // Break if right port is connected and it is not an execute pin.
                //                 // Right ports can only have one input value wire.
                //                 Debug.LogWarning(
                //                     $"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(Load))} - A port already has a connection. {wireReference.LeftSidePin.PinName}: {leftPort.connected} -> {wireReference.RightSidePin.PinName} {rightPort.connected}");
                //                 continue;
                //             }
                //
                //             CreateEdge(leftPort, rightPort, false);
                //         }
                //         else
                //         {
                //             Debug.Log($"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(Load))} - Could Not Connect {wireReference.LeftSidePin.PinName} -> {wireReference.RightSidePin.PinName}");
                //         }
                //     }
                //     else
                //     {
                //         Debug.Log(
                //             $"{TooltipMarkup.ClassMethod(nameof(BlueprintView), nameof(Load))} - Could Not Find Output Node {wireReference.LeftSidePin.PinName} -> {wireReference.RightSidePin.PinName}");
                //     }
                // }
            }
        }
        #endregion

        #region - Debug -

        #endregion

        #region - Contextual Menu -

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is GraphView && nodeCreationRequest != null)
            {
                evt.menu.AppendAction("Create Node", a =>
                {
                    NodeCreationRequest(new NodeCreationContext
                    {
                        screenMousePosition = a.eventInfo.mousePosition + Window.position.position,
                        index = -1,
                        target = null,
                    });
                });
                evt.menu.AppendSeparator();
            }

            if (evt.target is Node or Group)
            {
                var ge = evt.target as GraphElement;
                evt.menu.AppendAction("Cut", _ => BlueprintClipboardUtility.Cut(ge));
            }

            if (evt.target is Node or Group)
            {
                var ge = evt.target as GraphElement;
                evt.menu.AppendAction("Copy", _ => BlueprintClipboardUtility.Copy(ge));
            }

            if (evt.target is GraphView)
            {
                
                evt.menu.AppendAction("Paste", a => BlueprintClipboardUtility.Paste(this, contentViewContainer.WorldToLocal(a.eventInfo.mousePosition)),
                    _ => BlueprintClipboardUtility.CanPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            if (evt.target is GraphView or Node or Group or Edge)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Delete", _ => DeleteSelectionCallback(AskUser.DontAskUser),
                    _ => canDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            if (evt.target is GraphView or Node or Group)
            {
                evt.menu.AppendSeparator();
                var ge = evt.target as GraphElement;
                evt.menu.AppendAction("Duplicate", a => BlueprintClipboardUtility.Duplicate(ge, this, contentViewContainer.WorldToLocal(a.eventInfo.mousePosition)));
                evt.menu.AppendSeparator();

            }

            if (evt.target is BlueprintWireView edge)
            {
                var pos = evt.mousePosition;
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Create Redirect", _ => CreateRedirectNode(pos, edge));
            }
        }

        public override EventPropagation DeleteSelection()
        {
            var copy = selection.ToList();
            foreach (var selectable in copy)
            {
                switch (selectable)
                {
                    case BlueprintWireView wireView:
                        wireView.Delete();
                        break;
                    case BlueprintNodeView nodeView:
                        nodeView.Delete();
                        break;
                    case BlueprintRedirectNodeView redirectView:
                        redirectView.Delete();
                        break;
                }
            }

            return EventPropagation.Continue;
        }

        #endregion

        #region - Helper -
        public static bool CanConvert(Type from, Type to)
        {
            if (from == to) return false; // Same type
            if (to.IsAssignableFrom(from)) return true; // Inheritance check
            if (typeof(IConvertible).IsAssignableFrom(from) && typeof(IConvertible).IsAssignableFrom(to)) return true; // IConvertible
            if(to == typeof(string)) return true; // Implicit Convert To ToString();

            // Check for implicit or explicit conversion operators
            return HasConversionOperator(from, to, "op_Implicit") || HasConversionOperator(from, to, "op_Explicit");
        }

        private static bool HasConversionOperator(Type from, Type to, string opMethodName)
        {
            return from.GetMethods(BindingFlags.Static | BindingFlags.Public)
                       .Any(m => m.Name == opMethodName && m.ReturnType == to &&
                                 m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == from)
                   ||
                   to.GetMethods(BindingFlags.Static | BindingFlags.Public)
                       .Any(m => m.Name == opMethodName && m.ReturnType == to &&
                                 m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == from);
        }

        private bool TryGetConvertMethod(Type outputSlotType, Type inputSlotType, out MethodInfo methodInfo)
        {
            methodInfo = null;
            return _canConvertMap.TryGetValue(outputSlotType, out var map) && map.TryGetValue(inputSlotType, out methodInfo);
        }
        #endregion

        public void Invalidate(GraphInvalidationType invalidationType)
        {
            switch (invalidationType)
            {
                case GraphInvalidationType.RenamedNode:
                    foreach (var node in Nodes.Values)
                    {
                        node.InvalidateName();
                    }
                    break;
                case GraphInvalidationType.RetypedNode:
                    foreach (var node in Nodes.Values)
                    {
                        node.InvalidateType();
                    }
                    break;
                case GraphInvalidationType.Topology:
                    break;
                case GraphInvalidationType.Graph:
                    Window.SaveAsset();
                    Load();
                    break;
            }
        }
    }
}
