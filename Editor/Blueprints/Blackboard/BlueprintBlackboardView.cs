using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor;
using Vapor.Blueprints;
using Vapor.Inspector;
using VaporEditor.Inspector;
using Button = UnityEngine.UIElements.Button;
using Cursor = UnityEngine.UIElements.Cursor;

namespace VaporEditor.Blueprints
{
    public class BlueprintBlackboardView : VisualElement
    {
        public BlueprintEditorWindow Window { get; }
        public DragElementManipulator DragElementManipulator { get; private set; }
        public override VisualElement contentContainer => _scrollView.contentContainer;

        private readonly ScrollView _scrollView;
        private readonly VisualElement _inspectorContainer;
        private readonly BlueprintBlackboardMethodCategory _methodCategory;
        private readonly BlueprintBlackboardVariableCategory _classVariables;
        private readonly BlueprintBlackboardVariableCategory _methodVariables;
        
        
        private SubclassOf _subclassInstance;
        private BlueprintBlackboardVariable _currentVariable;
        private int _counter;
        private VisualElement _dragElement;

        public BlueprintBlackboardView(BlueprintEditorWindow window)
        {
            Window = window;
            style.flexGrow = 1f;
            DragElementManipulator = new DragElementManipulator(Window.rootVisualElement).WithKeyListeners(KeyCode.G, KeyCode.S);
            _dragElement = new VisualElement()
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    backgroundColor = new Color(0.16f, 0.16f, 0.16f),
                    borderBottomColor = new Color(0.12f, 0.12f, 0.12f),
                    borderTopColor = new Color(0.12f, 0.12f, 0.12f),
                    borderLeftColor = new Color(0.12f, 0.12f, 0.12f),
                    borderRightColor = new Color(0.12f, 0.12f, 0.12f),
                    borderTopWidth = 2f,
                    borderLeftWidth = 2f,
                    borderRightWidth = 2f,
                    borderBottomWidth = 2f,
                    borderBottomLeftRadius = 6f,
                    borderBottomRightRadius = 6f,
                    borderTopLeftRadius = 6f,
                    borderTopRightRadius = 6f,
                    maxWidth = 120,
                    height = 22,
                    maxHeight = 22,
                    flexGrow = 1f,
                }
            }.WithName("DragElement").WithManipulator(DragElementManipulator);
            var label = new Label()
            {
                style =
                {
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis,
                    flexGrow = 1f,
                    marginBottom = 0f,
                    marginLeft = 8f,
                    marginRight = 8f,
                    marginTop = 0f,
                    paddingLeft = 0f,
                    paddingRight = 0f,
                    paddingTop = 0f,
                    paddingBottom = 0f,
                    unityTextAlign = TextAnchor.MiddleCenter,
                }
            };
            _dragElement.Add(label);
            DragElementManipulator.BeginDrag += OnBeginVariableDragDrop;
            Window.rootVisualElement.Add(_dragElement);

            _scrollView = new ScrollView()
            {
                style =
                {
                    flexGrow = 1f,
                    backgroundColor = new Color(0.16f, 0.16f, 0.16f),
                }
            };
            _inspectorContainer = new VisualElement()
            {
                style =
                {
                    height = 200,
                    minHeight = 200,
                    backgroundColor = new Color(0.16f, 0.16f, 0.16f),
                }
            };
            hierarchy.Add(_scrollView);
            hierarchy.Add(new VisualElement()
            {
                style =
                {
                    height = 1f,
                    backgroundColor = Color.grey,
                }
            });
            hierarchy.Add(_inspectorContainer);

            Add(new Label("Blackboard")
            {
                style =
                {
                    marginLeft = 8f,
                    marginTop = 4f,
                    marginBottom = 4f,
                    fontSize = 14f
                }
            });
            Add(new VisualElement()
            {
                style =
                {
                    height = 1f,
                    backgroundColor = Color.grey,
                    flexGrow = 1f,
                }
            });

            _methodCategory = new BlueprintBlackboardMethodCategory(this, "Methods");
            _classVariables = new BlueprintBlackboardVariableCategory(this, "Variables", false);
            _methodVariables = new BlueprintBlackboardVariableCategory(this, "Local Variables", true);

            Add(_methodCategory);
            Add(_classVariables);
            Add(_methodVariables);

            Window.ClassGraphModel.MethodOpened += OnMethodOpened;
            Window.ClassGraphModel.MethodClosed += OnMethodClosed;
        }

        private void OnBeginVariableDragDrop(EventBase evt, VisualElement source)
        {
            if (source is BlueprintBlackboardVariable v)
            {
                var label = (evt.target as VisualElement)?.Q<Label>();
                if (label != null)
                {
                    label.text = v.Name;
                }
            }
        }

        private void OnMethodOpened(BlueprintClassGraphModel classGraph, BlueprintMethodGraph methodGraph)
        {
            _methodVariables.Show();
        }
        
        private void OnMethodClosed(BlueprintClassGraphModel classGraph, BlueprintMethodGraph methodGraph)
        {
            _methodVariables.Hide();
        }
    }

    public class BlueprintBlackboardMethodCategory : VisualElement
    {
        public struct MethodOverrideDescriptor
        {
            public string Category;
            public string Name;
            public MethodInfo MethodInfo;
            public (Type, string)[] UnityMessageParameters;
        }
        
        public override VisualElement contentContainer => _foldout.contentContainer;

        private readonly BlueprintBlackboardView _view;
        private readonly Foldout _foldout;
        private readonly Button _overrideSelector;

        private readonly List<MethodInfo> _overridableMethods;
        private readonly List<MethodOverrideDescriptor> _descriptors;
        private readonly List<GenericSearchModel> _filteredDescriptors;

        public BlueprintBlackboardMethodCategory(BlueprintBlackboardView view, string header)
        {
            _view = view;
            Func<MethodInfo, bool> filter = mi => mi.IsVirtual && !mi.IsAbstract && !mi.IsSpecialName;
            _overridableMethods = ReflectionUtility.GetAllMethodsThatMatch(_view.Window.ClassGraphModel.ParentType, filter, false).Distinct().ToList();
            _descriptors = new List<MethodOverrideDescriptor>(_overridableMethods.Count);
            foreach (var mi in _overridableMethods.Where(mi => mi.Name != "Finalize" && mi.GetBaseDefinition() == mi))
            {
                _descriptors.Add(new MethodOverrideDescriptor
                {
                    Category = "Virtual",
                    Name = BlueprintEditorUtility.FormatMethodName(mi),
                    MethodInfo = mi
                });
            }

            if (_view.Window.ClassGraphModel.Graph.GraphType == BlueprintGraphSo.BlueprintGraphType.BehaviourGraph)
            {
                foreach (var message in BlueprintEditorUtility.UnityMessageMethods)
                {
                    _descriptors.Add(new MethodOverrideDescriptor
                    {
                        Category = "Unity",
                        Name = message.Key,
                        UnityMessageParameters = message.Value
                    });
                }
            }

            _filteredDescriptors = new List<GenericSearchModel>(_descriptors.Count);

            style.flexGrow = 1f;
            _foldout = new Foldout()
            {
                text = header,
                style =
                {
                    backgroundColor = new Color(0.2784314f, 0.2784314f, 0.2784314f),
                }
            };
            _foldout.SetValueWithoutNotify(false);
            _foldout.hierarchy[0].style.borderBottomColor = Color.grey;
            _foldout.hierarchy[0].style.borderBottomWidth = 1f;
            _foldout.hierarchy[0].style.marginLeft = 0f;
            _foldout.hierarchy[0].style.marginRight = 0f;
            _foldout.hierarchy[0].style.marginBottom = 0f;
            _foldout.hierarchy[0].style.paddingLeft = 3f;
            _foldout.hierarchy[0].style.paddingRight = 3f;
            _foldout.hierarchy[0].style.paddingBottom = 3f;
            _overrideSelector = new Button(OnSelectMethodOverride)
            {
                text = "Overrides",
                style =
                {
                    marginLeft = 2f,
                    marginTop = 2f,
                    marginBottom = 2f,
                    marginRight = 2f,
                    paddingLeft = 4f,
                    paddingTop = 4f,
                    paddingRight = 4f,
                    paddingBottom = 4f,
                    unityTextAlign = TextAnchor.MiddleCenter,
                }
            };
            _foldout.hierarchy[0].Add(_overrideSelector);
            _foldout.hierarchy[0].Add(new Button(OnAddMethod)
            {
                text = "+",
                style =
                {
                    marginLeft = 2f,
                    marginTop = 2f,
                    marginBottom = 2f,
                    marginRight = 2f,
                    paddingLeft = 4f,
                    paddingTop = 4f,
                    paddingRight = 4f,
                    paddingBottom = 4f,
                    unityTextAlign = TextAnchor.MiddleCenter,
                }
            });
            _foldout.contentContainer.style.marginLeft = 0f;
            hierarchy.Add(_foldout);

            _view.Window.ClassGraphModel.InterfaceTypeChanged += (_, _, _) => RefreshMethods();
            _view.Window.ClassGraphModel.MethodChanged += (_, _, ct, _) => 
            { 
                if(ct != ChangeType.Added)
                {
                    RefreshMethods();
                }
            };
            RegisterCallbackOnce<AttachToPanelEvent>(evt => RefreshMethods());
            
        }
        
        private void OnSelectMethodOverride()
        {
            var screenPosition = GUIUtility.GUIToScreenPoint(_overrideSelector.worldBound.position) + new Vector2(0, _overrideSelector.worldBound.height + 16);
            
            _filteredDescriptors.Clear();
            foreach (var descriptor in _descriptors.Where(descriptor => !_view.Window.ClassGraphModel.Methods.Exists(m => m.MethodInfo != null && m.MethodInfo == descriptor.MethodInfo)))
            {
                var gsm = new GenericSearchModel(descriptor.Category, descriptor.Name)
                    .WithUserData(descriptor.Category == "Unity" ? descriptor.UnityMessageParameters :  descriptor.MethodInfo) as GenericSearchModel;
                _filteredDescriptors.Add(gsm);
            }
            GenericSearchWindow.Show(screenPosition, screenPosition, new GenericSearchProvider(OnMethodOverrideSelected, _filteredDescriptors), false, true);
        }

        private void OnMethodOverrideSelected(GenericSearchModel[] model)
        {
            if (model[0].UserData is MethodInfo mi)
            {
                if (!_overridableMethods.Contains(mi))
                {
                    return;
                }
            
                if (_view.Window.ClassGraphModel.Methods.Exists(m => m.MethodInfo == mi))
                {
                    return;
                }

                var methodGraph = _view.Window.ClassGraphModel.AddMethod(mi);
                Add(new BlueprintBlackboardMethod(_view, methodGraph));
            }
            else
            {
                var breakIdx = model[0].Name.IndexOf('(');
                var methodName = model[0].Name[..breakIdx];
                var unityMessageParameters = model[0].UserData as (Type, string)[];
                if (_view.Window.ClassGraphModel.Methods.Exists(m => m.MethodName == methodName))
                {
                    return;
                }
                
                var methodGraph = _view.Window.ClassGraphModel.AddUnityMethod(methodName, unityMessageParameters);
                Add(new BlueprintBlackboardMethod(_view, methodGraph));
            }
            
        }

        private void SetState(bool open) => _foldout.value = open;

        private void OnAddMethod()
        {
            var methodGraph = _view.Window.ClassGraphModel.AddMethod(null);
            var method = new BlueprintBlackboardMethod(_view, methodGraph);
            Add(method);
            SetState(true);
            method.StartRename();
        }
        
        private void RefreshMethods()
        {
            Clear();
            foreach (var v in _view.Window.ClassGraphModel.Methods)
            {
                Add(new BlueprintBlackboardMethod(_view, v));
            }
        }
    }

    public class BlueprintBlackboardMethod : VisualElement
    {
        public override bool focusable => true;

        private readonly BlueprintBlackboardView _view;
        private readonly BlueprintMethodGraph _methodGraph;
        
        private readonly Label _label;
        private readonly TextField _renameTextField;

        public BlueprintBlackboardMethod(BlueprintBlackboardView view, BlueprintMethodGraph methodGraph)
        {
            _view = view;
            _methodGraph = methodGraph;
            style.flexGrow = 1f;
            style.flexDirection = FlexDirection.Row;
            style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);
            style.marginLeft = 0f;
            style.marginRight = 0f;

            var ve = new VisualElement()
            {
                style =
                {
                    flexGrow = 1f,
                }
            };
            _label = new Label(_methodGraph.IsOverride ? BlueprintEditorUtility.FormatMethodName(_methodGraph.MethodInfo) : _methodGraph.MethodName)
            {
                style =
                {
                    flexGrow = 1f,
                    marginLeft = 15f,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis,
                }
            };
            _renameTextField = new TextField()
            {
                isDelayed = true,
                style =
                {
                    flexGrow = 1f,
                    marginLeft = 15f,
                    display = DisplayStyle.None,
                }
            };
            _renameTextField.RegisterValueChangedCallback(evt =>
            {
                if (!evt.newValue.EmptyOrNull())
                {
                    var trimmed = evt.newValue.Replace(" ", "");
                    _methodGraph.SetName(trimmed);
                }

                _renameTextField.style.display = DisplayStyle.None;
                _label.style.display = DisplayStyle.Flex;
            });
            _renameTextField.RegisterCallback<FocusOutEvent>(_ =>
            {
                _renameTextField.style.display = DisplayStyle.None;
                _label.style.display = DisplayStyle.Flex;
            });
            ve.Add(_label);
            if(!_methodGraph.IsOverride && !_methodGraph.IsUnityOverride)
            {
                ve.Add(_renameTextField);
            }
            Add(ve);
            
            var button = new Button(OnEditMethodGraph)
            {
                style =
                {
                    width = 25,
                    height = 20,
                }
            };
            button.Add(new Image { image = EditorGUIUtility.IconContent("d_editicon.sml").image });
            Add(button);
            
            RegisterCallback<FocusInEvent>(_ =>
            {
                style.backgroundColor = new Color(0.16f, 0.16f, 0.2f);
                _view.Window.InspectorView.SetInspectorTarget(new BlueprintInspectorMethodView(_methodGraph));
            });
            RegisterCallback<FocusOutEvent>(_ =>
            {
                style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);
            });
            RegisterCallback<KeyDownEvent>(evt =>
            {
                switch (evt.keyCode)
                {
                    case KeyCode.F2 when !_methodGraph.IsOverride && !_methodGraph.IsUnityOverride:
                        StartRename();
                        break;
                    case KeyCode.Delete:
                        CTX_Delete(null);
                        break;
                }
            });
            
            if(_methodGraph.IsOverride)
            {
                var label = new Label(_methodGraph.MethodDeclaringType.Name)
                {
                    style =
                    {
                        flexGrow = 1f,
                        color = Color.grey,
                        fontSize = 10f,
                        unityTextAlign = TextAnchor.MiddleRight,
                        overflow = Overflow.Hidden,
                        textOverflow = TextOverflow.Ellipsis,
                    }
                };
                Insert(1, label);
            }

            if (_methodGraph.IsUnityOverride)
            {
                var label = new Label("Unity Event")
                {
                    style =
                    {
                        flexGrow = 1f,
                        color = Color.grey,
                        fontSize = 10f,
                        unityTextAlign = TextAnchor.MiddleRight,
                        overflow = Overflow.Hidden,
                        textOverflow = TextOverflow.Ellipsis,
                    }
                };
                Insert(1, label);
            }

            RegisterCallbackOnce<AttachToPanelEvent>(evt => _methodGraph.NameChanged += OnNameChanged);
            RegisterCallbackOnce<DetachFromPanelEvent>(evt => _methodGraph.NameChanged -= OnNameChanged);
            
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        private void OnNameChanged(BlueprintMethodGraph methodGraph)
        {
            _label.text = methodGraph.MethodName;
            _renameTextField.SetValueWithoutNotify(methodGraph.MethodName);
        }

        private void OnEditMethodGraph()
        {
            _methodGraph.Edit();
            // _view.ShowConditionalMethodFoldouts();
            // _view.Window.ClassGraphModel.OpenMethodForEdit(_methodGraph);
            // _view.Window.GraphEditorView.Invalidate(GraphInvalidationType.Graph);
        }
        
        private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is not BlueprintBlackboardMethod)
            {
                return;
            }

            evt.menu.AppendAction("Rename", CTX_Rename, _ => _methodGraph.IsOverride || _methodGraph.IsUnityOverride ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete", CTX_Delete);
        }

        private void CTX_Rename(DropdownMenuAction obj)
        {
            StartRename();
        }
        
        private void CTX_Delete(DropdownMenuAction obj)
        {
            _methodGraph.Delete();
            // var window = _view.Window;
            // var current = window.ClassGraphModel.Current;
            // window.ClassGraphModel.Methods.RemoveAll(m => m.MethodName == _name);
            
            // RemoveFromHierarchy();
            // if (current != null && current.MethodName == _name)
            // {
                // window.ClassGraphModel.Current = null;
                // _view.ShowConditionalMethodFoldouts();
                // window.GraphEditorView.Invalidate(GraphInvalidationType.Graph);
            // }
        }

        public void StartRename()
        {
            _label.style.display = DisplayStyle.None;
            _renameTextField.style.display = DisplayStyle.Flex;
            _renameTextField.SetValueWithoutNotify(_label.text);
            _renameTextField.Focus();
            _renameTextField.SelectAll();
        }
    }
    
    public class BlueprintBlackboardVariableCategory : VisualElement
    {
        private readonly BlueprintBlackboardView _view;
        private readonly bool _isMethodVariable;
        public override VisualElement contentContainer => _foldout.contentContainer;

        private readonly Foldout _foldout;
        
        public BlueprintBlackboardVariableCategory(BlueprintBlackboardView view, string header, bool isMethodVariable)
        {
            _view = view;
            _isMethodVariable = isMethodVariable;
            style.flexGrow = 1f;
            _foldout = new Foldout()
            {
                text = header,
                style =
                {
                    backgroundColor = new Color(0.2784314f, 0.2784314f, 0.2784314f),
                }
            };
            _foldout.SetValueWithoutNotify(false);
            _foldout.hierarchy[0].style.borderBottomColor = Color.grey;
            _foldout.hierarchy[0].style.borderBottomWidth = 1f;
            _foldout.hierarchy[0].style.marginLeft = 0f;
            _foldout.hierarchy[0].style.marginRight = 0f;
            _foldout.hierarchy[0].style.marginBottom = 0f;
            _foldout.hierarchy[0].style.paddingLeft = 3f;
            _foldout.hierarchy[0].style.paddingRight = 3f;
            _foldout.hierarchy[0].style.paddingBottom = 3f;
            _foldout.hierarchy[0].Add(new Button(OnAddVariable)
            {
                text = "+",
                style =
                {
                    marginLeft = 2f,
                    marginTop = 2f,
                    marginBottom = 2f,
                    marginRight = 2f,
                    paddingLeft = 4f,
                    paddingTop = 4f,
                    paddingRight = 4f,
                    paddingBottom = 4f,
                    unityTextAlign = TextAnchor.MiddleCenter,
                }
            });
            _foldout.contentContainer.style.marginLeft = 0f;
            
            hierarchy.Add(_foldout);
            
            if (_isMethodVariable)
            {
                _view.Window.ClassGraphModel.MethodOpened += OnMethodOpened;
                _view.Window.ClassGraphModel.MethodClosed += OnMethodClosed;
                
            }
            else
            {
                _view.Window.ClassGraphModel.VariableChanged += (_, v, ct, ignoreUndo) =>
                {
                    if (ct != ChangeType.Added)
                    {
                        RefreshVariables();
                    }
                    else
                    {
                        var vw = new BlueprintBlackboardVariable(_view, v);
                        Add(vw);
                        SetState(true);
                        vw.StartRename();
                    }
                };
            }

            RegisterCallbackOnce<AttachToPanelEvent>(evt => RefreshVariables());
        }

        private void SetState(bool open) => _foldout.value = open;
        
        private void OnAddVariable()
        {
            if (_isMethodVariable)
            {
                _view.Window.ClassGraphModel.Current.AddVariable(typeof(bool));
            }
            else
            {
                _view.Window.ClassGraphModel.AddVariable(typeof(bool));
            }
        }

        private void RefreshVariables()
        {
            Clear();
            if (_isMethodVariable)
            {
                if (_view.Window.ClassGraphModel.Current == null)
                {
                    this.Hide();
                    return;
                }
                
                foreach (var v in _view.Window.ClassGraphModel.Current.Variables.Values)
                {
                    Add(new BlueprintBlackboardVariable(_view, v));
                }
                this.Show();
            }
            else
            {
                foreach (var v in _view.Window.ClassGraphModel.Variables.Values)
                {
                    Add(new BlueprintBlackboardVariable(_view, v));
                }
            }
        }
        
        private void OnMethodOpened(BlueprintClassGraphModel classGraph, BlueprintMethodGraph methodGraph)
        {
            methodGraph.VariableChanged -= OnMethodVariableChanged;
            methodGraph.VariableChanged += OnMethodVariableChanged;
            RefreshVariables();
        }
        
        private void OnMethodClosed(BlueprintClassGraphModel classGraph, BlueprintMethodGraph methodGraph)
        {
            methodGraph.VariableChanged -= OnMethodVariableChanged;
        }

        private void OnMethodVariableChanged(BlueprintMethodGraph methodGraph, BlueprintVariable variable, ChangeType changeType, bool ignoreUndo)
        {
            if (changeType != ChangeType.Added)
            {
                RefreshVariables();
            }
            else
            {
                var vw = new BlueprintBlackboardVariable(_view, variable);
                Add(vw);
                SetState(true);
                vw.StartRename();
            }
        }
    }
    
    public class BlueprintBlackboardVariable : VisualElement
    {
        private readonly Label _label;
        private readonly TextField _renameTextField;
        private readonly TypeSelectorField _typeSelector;

        private readonly BlueprintVariable _model;
        public override bool focusable => true;
        public string Name => _model.DisplayName;
        public BlueprintVariable Model => _model;

        public BlueprintBlackboardVariable(BlueprintBlackboardView view, BlueprintVariable model)
        {
            _model = model;
            
            style.flexGrow = 1f;
            style.flexDirection = FlexDirection.Row;
            style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);
            style.marginLeft = 0f;
            

            var ve = new VisualElement()
            {
                style =
                {
                    flexGrow = 1f,
                    flexBasis = Length.Percent(50),
                }
            };
            _label = new Label(_model.DisplayName)
            {
                style =
                {
                    flexGrow = 1f,
                    marginLeft = 15f,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis,
                }
            };
            _renameTextField = new TextField()
            {
                isDelayed = true,
                style =
                {
                    flexGrow = 1f,
                    marginLeft = 15f,
                    display = DisplayStyle.None,
                }
            };
            _renameTextField.RegisterValueChangedCallback(evt =>
            {
                if (!evt.newValue.EmptyOrNull())
                {
                    _model.DisplayName = evt.newValue;
                }

                _renameTextField.style.display = DisplayStyle.None;
                _label.style.display = DisplayStyle.Flex;
            });
            _renameTextField.RegisterCallback<FocusOutEvent>(_ =>
            {
                _renameTextField.style.display = DisplayStyle.None;
                _label.style.display = DisplayStyle.Flex;
            });
            ve.Add(_label);
            ve.Add(_renameTextField);
            Add(ve);

            _typeSelector = new TypeSelectorField(string.Empty, _model.Type, t => (t.IsPublic || t.IsNestedPublic) && !(t.IsAbstract && t.IsSealed))
            {
                style =
                {
                    flexBasis = Length.Percent(50f)
                }
            };
            _typeSelector.TypeChanged += SetType;
            Add(_typeSelector);
            
            RegisterCallback<FocusInEvent>(_ =>
            {
                style.backgroundColor = new Color(0.16f, 0.16f, 0.2f);
                view.Window.InspectorView.SetInspectorTarget(new BlueprintInspectorVariableView(_model));
            });
            RegisterCallback<FocusOutEvent>(_ =>
            {
                style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);
            });
            RegisterCallback<KeyDownEvent>(evt =>
            {
                switch (evt.keyCode)
                {
                    case KeyCode.F2:
                        StartRename();
                        break;
                    case KeyCode.Delete:
                        CTX_Delete(null);
                        break;
                }
            });
            RegisterCallbackOnce<AttachToPanelEvent>(_ => _model.Changed += OnModelOnChanged);
            RegisterCallbackOnce<DetachFromPanelEvent>(_ => _model.Changed -= OnModelOnChanged);
            
            var drag = new DragDropManipulator("slot--action-drag", view.DragElementManipulator)
                .WithDragLocationMatch(DragElementManipulator.DragLocationMatch.Center)
                .WithActivator<DragDropManipulator>(EventModifiers.None, MouseButton.LeftMouse);
            
            this.AddManipulator(drag);
            
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        private void OnModelOnChanged(BlueprintVariable variable, BlueprintVariable.ChangeType type)
        {
            switch (type)
            {
                case BlueprintVariable.ChangeType.Name:
                    _label.text = variable.DisplayName;
                    _renameTextField.SetValueWithoutNotify(variable.DisplayName);
                    break;
                case BlueprintVariable.ChangeType.Type:
                    break;
                case BlueprintVariable.ChangeType.Delete:
                    RemoveFromHierarchy();
                    break;
            }
        }

        private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is not BlueprintBlackboardVariable)
            {
                return;
            }

            evt.menu.AppendAction("Rename", CTX_Rename);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete", CTX_Delete);
        }

        private void CTX_Rename(DropdownMenuAction obj)
        {
            StartRename();
        }
        
        private void CTX_Delete(DropdownMenuAction obj)
        {
            _model.Delete();
        }

        private void SetType(VisualElement sender, Type oldType, Type newType)
        {
            _model.Type = newType;
        }

        public void StartRename()
        {
            _label.style.display = DisplayStyle.None;
            _renameTextField.style.display = DisplayStyle.Flex;
            _renameTextField.SetValueWithoutNotify(_label.text);
            _renameTextField.Focus();
            _renameTextField.SelectAll();
        }
    }
}
