using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor;
using Vapor.Blueprints;
using Vapor.Inspector;
using VaporEditor.Inspector;
using Button = UnityEngine.UIElements.Button;

namespace VaporEditor.Blueprints
{
    public class BlueprintBlackboardView : VisualElement
    {
        public BlueprintEditorWindow Window { get; }
        public override VisualElement contentContainer => _scrollView.contentContainer;

        private readonly ScrollView _scrollView;
        private readonly VisualElement _inspectorContainer;
        private readonly BlueprintBlackboardMethodCategory _methodCategory;
        private readonly BlueprintBlackboardVariableCategory _variables;
        
        private readonly BlueprintBlackboardVariableCategory _inputArguments;
        private readonly BlueprintBlackboardVariableCategory _outputArguments;
        private readonly BlueprintBlackboardVariableCategory _localVariables;
        
        
        private SubclassOf _subclassInstance;
        private BlueprintBlackboardVariable _currentVariable;
        private int _counter;

        public BlueprintBlackboardView(BlueprintEditorWindow window)
        {
            Window = window;
            style.flexGrow = 1f;
            
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

            _methodCategory = new BlueprintBlackboardMethodCategory(this, "Methods", Type.GetType(Window.GraphObject.AssemblyQualifiedTypeName), OnAddMethod);
            _variables = new BlueprintBlackboardVariableCategory("Variables", OnAddVariable);
            
            _inputArguments = new BlueprintBlackboardVariableCategory("Input Arguments", OnAddInputArgument);
            _inputArguments.Hide();
            _outputArguments = new BlueprintBlackboardVariableCategory("Output Arguments", OnAddOutputArgument);
            _outputArguments.Hide();
            _localVariables = new BlueprintBlackboardVariableCategory("Local Variables", OnAddLocalVariable);
            _localVariables.Hide();
            
            Add(_methodCategory);
            Add(_variables);
            
            Add(_inputArguments);
            Add(_outputArguments);
            Add(_localVariables);
            
            foreach (var v in Window.DesignGraph.Methods)
            {
                _methodCategory.Add(v.IsOverride ? new BlueprintBlackboardMethod(v.MethodInfo) : new BlueprintBlackboardMethod(v.MethodName));
            }
            
            foreach (var v in Window.DesignGraph.Variables)
            {
                _variables.Add(new BlueprintBlackboardVariable(v));
            }
        }

        private void OnAddMethod()
        {
            var nm = GetNextName("Method");
            Window.DesignGraph.AddMethod(nm, null);
            var method = new BlueprintBlackboardMethod(nm);
            _methodCategory.Add(method);
            _methodCategory.SetState(true);
            method.StartRename();
        }

        public void OnOverrideMethod(MethodInfo method)
        {
            if (Window.DesignGraph.Methods.Exists(m => m.MethodName == method.Name))
            {
                return;
            }

            Window.DesignGraph.AddMethod(method.Name, method);
            _methodCategory.Add(new BlueprintBlackboardMethod(method));
        }

        private void OnAddVariable()
        {
            // var nm = GetNextName("Var");
            var variable = Window.DesignGraph.AddVariable(typeof(bool));
            var view = new BlueprintBlackboardVariable(variable);
            _variables.Add(view);
            _variables.SetState(true);
            view.StartRename();
        }
        
        private void OnAddInputArgument()
        {
            // var nm = GetNextName("InputArg");
            var variable = Window.DesignGraph.Current.AddInputArgument(typeof(bool));
            var view = new BlueprintBlackboardVariable(variable);
            _inputArguments.Add(view);
            _inputArguments.SetState(true);
            view.StartRename();
            Window.GraphEditorView.Invalidate(GraphInvalidationType.Graph);
        }

        private void OnAddOutputArgument()
        {
            // var nm = GetNextName("OutputArg");
            var variable = Window.DesignGraph.Current.AddOutputArgument(typeof(bool));
            var view = new BlueprintBlackboardVariable(variable);
            _outputArguments.Add(view);
            _outputArguments.SetState(true);
            view.StartRename();
            Window.GraphEditorView.Invalidate(GraphInvalidationType.Graph);
        }

        private void OnAddLocalVariable()
        {
            // var nm = GetNextName("LocalVar");
            var variable = Window.DesignGraph.Current.AddTemporaryVariable(typeof(bool));
            var view = new BlueprintBlackboardVariable(variable);
            _localVariables.Add(view);
            _localVariables.SetState(true);
            view.StartRename();
        }

        // public void DrawField(BlueprintBlackboardVariable blueprintBlackboardVariable, Type type)
        // {
        //     _currentVariable = blueprintBlackboardVariable;
        //     _inspectorContainer.Clear();
        //     _subclassInstance = new SubclassOf(type);
        //     var detail = SerializedDrawerUtility.DrawFieldFromObject(_subclassInstance, _subclassInstance.GetType());
        //     detail.RegisterCallback<TreePropertyChangedEvent>(_ =>
        //     {
        //         _currentVariable?.SetType(_subclassInstance.GetResolvedType());
        //     });
        //     _inspectorContainer.Add(detail);
        // }

        private string GetNextName(string prefix)
        {
            switch (prefix)
            {
                case "InputArg":
                {
                    while (Window.DesignGraph.Current.InputArguments.Exists(m => m.Name == $"{prefix}_{_counter}"))
                    {
                        _counter++;
                    }
                    break;
                }
                case "Output":
                {
                    while (Window.DesignGraph.Current.OutputArguments.Exists(m => m.Name == $"{prefix}_{_counter}"))
                    {
                        _counter++;
                    }
                    break;
                }
                case "LocalVar":
                {
                    while (Window.DesignGraph.Variables.Exists(m => m.Name == $"{prefix}_{_counter}"))
                    {
                        _counter++;
                    }
                    break;
                }
                case "Method":
                    _counter++;
                    break;
            }
            return $"{prefix}_{_counter}";
        }

        public void UpdateInputArguments()
        {
            _inputArguments.Clear();
            foreach (var v in Window.DesignGraph.Current.InputArguments)
            {
                _inputArguments.Add(new BlueprintBlackboardVariable(v));
            }
        }

        public void UpdateOutputArguments()
        {
            _outputArguments.Clear();
            foreach (var v in Window.DesignGraph.Current.OutputArguments)
            {
                _outputArguments.Add(new BlueprintBlackboardVariable(v));
            }
        }
        
        public void UpdateTemporaryVariables()
        {
            _localVariables.Clear();
            foreach (var v in Window.DesignGraph.Current.TemporaryVariables)
            {
                _localVariables.Add(new BlueprintBlackboardVariable(v));
            }
        }

        public void ShowConditionalMethodFoldouts()
        {
            var current  = Window.DesignGraph.Current;
            if (current == null)
            {
                _inputArguments.Hide();
                _outputArguments.Hide();
                _localVariables.Hide();
            }
            else
            {
                if (current.IsOverride)
                {
                    _inputArguments.Hide();
                    _outputArguments.Hide();
                }
                else
                {
                    _inputArguments.Show();
                    _outputArguments.Show();

                    UpdateInputArguments();
                    UpdateOutputArguments();
                }
                _localVariables.Show();
                UpdateTemporaryVariables();
            }
        }
    }

    public class BlueprintBlackboardMethodCategory : VisualElement
    {
        public override VisualElement contentContainer => _foldout.contentContainer;

        private readonly BlueprintBlackboardView _view;
        private readonly Foldout _foldout;
        private readonly Button _overrideSelector;

        private readonly List<MethodInfo> _overridableMethods;
        private readonly List<GenericDescriptor> _descriptors;
        private readonly List<GenericDescriptor> _filteredDescriptors;

        public BlueprintBlackboardMethodCategory(BlueprintBlackboardView view, string header, Type declaringType, Action addMethodCallback)
        {
            _view = view;
            Func<MethodInfo, bool> filter = mi => (mi.IsVirtual || mi.IsAbstract) && !mi.IsSpecialName;
            _overridableMethods = ReflectionUtility.GetAllMethodsThatMatch(declaringType, filter, false).ToList();
            foreach (var interfaceAqn in _view.Window.DesignGraph.ImplementedInterfaces)
            {
                if (interfaceAqn.EmptyOrNull())
                {
                    continue;
                }
                var type = Type.GetType(interfaceAqn);
                _overridableMethods.AddRange(ReflectionUtility.GetAllMethodsThatMatch(type, filter, false));
            }
            _descriptors = new List<GenericDescriptor>(_overridableMethods.Count);
            foreach (var mi in _overridableMethods.Where(mi => mi.Name != "Finalize"))
            {
                _descriptors.Add(new GenericDescriptor
                {
                    Category = string.Empty,
                    Name = mi.Name,
                    UserData = mi
                });
            }
            _filteredDescriptors = new List<GenericDescriptor>(_descriptors.Count);

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
            _overrideSelector = new Button(OnSelectType)
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
            _foldout.hierarchy[0].Add(new Button(addMethodCallback)
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
        }
        
        private void OnSelectType()
        {
            var screenPosition = GUIUtility.GUIToScreenPoint(_overrideSelector.worldBound.position) + new Vector2(0, _overrideSelector.worldBound.height + 16);
            
            _filteredDescriptors.Clear();
            foreach (var descriptor in _descriptors.Where(descriptor => !_view.Window.DesignGraph.Methods.Exists(m => m.MethodName == descriptor.Name)))
            {
                _filteredDescriptors.Add(descriptor);
            }
            BlueprintSearchWindow.Show(screenPosition, screenPosition, new GenericSearchProvider(OnTypeSelected, _filteredDescriptors), false, true);
        }

        private void OnTypeSelected(BlueprintSearchModel model, Vector2 position)
        {
            var mi = (MethodInfo)model.Parameters.FirstOrDefault(t => t.Item1 == GenericSearchProvider.PARAM_USER_DATA).Item2;
            if (!_overridableMethods.Contains(mi))
            {
                return;
            }
            
            _view.OnOverrideMethod(mi);
        }

        public void SetState(bool open) => _foldout.value = open;
    }

    public class BlueprintBlackboardMethod : VisualElement
    {
        public override bool focusable => true;
        

        private bool IsOverride { get; }


        private string _name;
        private readonly Label _label;
        private readonly TextField _renameTextField;

        public BlueprintBlackboardMethod(string name)
        {
            _name = name;
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
            _label = new Label(_name)
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
                    _label.text = trimmed;
                    var window = GetFirstAncestorOfType<BlueprintBlackboardView>().Window;
                    window.DesignGraph.Methods.First(v => v.MethodName == _name).MethodName = trimmed;
                    _name = trimmed;
                    _renameTextField.SetValueWithoutNotify(trimmed);
                    window.GraphEditorView.Invalidate(GraphInvalidationType.RenamedNode);
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
            
            var button = new Button(OnSelectMethodGraph)
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
            });
            RegisterCallback<FocusOutEvent>(_ =>
            {
                style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);
            });
            RegisterCallback<KeyDownEvent>(evt =>
            {
                switch (evt.keyCode)
                {
                    case KeyCode.F2 when !IsOverride:
                        StartRename();
                        break;
                    case KeyCode.Delete:
                        CTX_Delete(null);
                        break;
                }
            });
            
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        public BlueprintBlackboardMethod(MethodInfo method) : this(method.Name)
        {
            IsOverride = true;
            var displayName = method.IsGenericMethod ? $"{method.Name.Split('`')[0]}<{string.Join(",", method.GetGenericArguments().Select(a => a.Name))}>" : method.Name;
            displayName = method.IsSpecialName ? displayName.ToTitleCase() : displayName;
            var parameters = method.GetParameters();
            string paramNames = parameters.Length > 0
                ? parameters.Select(pi => pi.ParameterType.IsGenericType
                        ? $"{pi.ParameterType.Name.Split('`')[0]}<{string.Join(",", pi.ParameterType.GetGenericArguments().Select(a => a.Name))}>"
                        : pi.ParameterType.Name)
                    .Aggregate((a, b) => a + ", " + b)
                : string.Empty;

            displayName = $"{displayName}({paramNames})";
            
            _label.text = displayName;
            
            if(IsOverride)
            {
                var label = new Label("Override")
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
        }

        private void OnSelectMethodGraph()
        {
            var view = GetFirstAncestorOfType<BlueprintBlackboardView>();
            view.Window.DesignGraph.SelectMethod(_name);
            view.ShowConditionalMethodFoldouts();
            view.Window.GraphEditorView.Invalidate(GraphInvalidationType.Graph);
        }
        
        private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is not BlueprintBlackboardMethod)
            {
                return;
            }

            evt.menu.AppendAction("Rename", CTX_Rename, _ => IsOverride ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete", CTX_Delete);
        }

        private void CTX_Rename(DropdownMenuAction obj)
        {
            StartRename();
        }
        
        private void CTX_Delete(DropdownMenuAction obj)
        {
            var view = GetFirstAncestorOfType<BlueprintBlackboardView>();
            var window = view.Window;
            var current = window.DesignGraph.Current;
            window.DesignGraph.Methods.RemoveAll(m => m.MethodName == _name);
            
            RemoveFromHierarchy();
            if (current != null && current.MethodName == _name)
            {
                window.DesignGraph.Current = null;
                view.ShowConditionalMethodFoldouts();
                window.GraphEditorView.Invalidate(GraphInvalidationType.Graph);
            }
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
        public override VisualElement contentContainer => _foldout.contentContainer;

        private readonly Foldout _foldout;
        
        public BlueprintBlackboardVariableCategory(string header, Action addVariableCallback)
        {
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
            _foldout.hierarchy[0].Add(new Button(addVariableCallback)
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
        }

        public void SetState(bool open) => _foldout.value = open;
    }

    internal class GenericTypeBuilder
    {
        public bool Complete => _index >= _genericArguments.Length;
        
        private readonly Type _genericType;
        private readonly Type[] _genericArguments;
        private int _index;
        private GenericTypeBuilder _childBuilder;

        public GenericTypeBuilder(Type genericType, Type[] genericArguments)
        {
            _genericType = genericType;
            _genericArguments = genericArguments;
            _index = 0;
        }

        public void AddType(Type type)
        {
            if (_childBuilder is { Complete: false })
            {
                _childBuilder.AddType(type);
                if (_childBuilder.Complete)
                {
                    _genericArguments[_index] = _childBuilder.MakeType();
                    _index++;
                }
            }
            else
            {
                _genericArguments[_index] = type;
                _index++;
            }
        }

        public Type MakeType()
        {
            return _genericType.MakeGenericType(_genericArguments);
        }

        public void StackGenericType(Type cachedGenericType, Type[] genericTypeArguments)
        {
            if (_childBuilder != null)
            {
                _childBuilder.StackGenericType(cachedGenericType, genericTypeArguments);
            }
            else
            {
                _childBuilder = new GenericTypeBuilder(cachedGenericType, genericTypeArguments);
            }
        }
    }
    
    public class BlueprintBlackboardVariable : VisualElement
    {
        private readonly Label _label;
        private readonly TextField _renameTextField;
        private readonly TypeSelectorField _typeSelector;
        
        private BlueprintVariable _model;
        private GenericTypeBuilder _typeStack;

        public Type CurrentType => _typeSelector.CurrentType;

        public override bool focusable => true;

        public BlueprintBlackboardVariable(BlueprintVariable model)
        {
            style.flexGrow = 1f;
            style.flexDirection = FlexDirection.Row;
            style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);
            style.marginLeft = 0f;
            _model = model;

            var ve = new VisualElement()
            {
                style =
                {
                    flexGrow = 1f,
                }
            };
            _label = new Label(_model.Name)
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
                    _label.text = trimmed;
                    var window = GetFirstAncestorOfType<BlueprintBlackboardView>().Window;
                    _model.Name = trimmed;
                    _renameTextField.SetValueWithoutNotify(trimmed);
                    window.GraphEditorView.Invalidate(GraphInvalidationType.RenamedNode);
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

            _typeSelector = new TypeSelectorField(string.Empty, _model.Type, t => (t.IsPublic || t.IsNestedPublic) && !(t.IsAbstract && t.IsSealed));
            _typeSelector.TypeChanged += SetType;
            
            RegisterCallback<FocusInEvent>(_ =>
            {
                style.backgroundColor = new Color(0.16f, 0.16f, 0.2f);
                var view = GetFirstAncestorOfType<BlueprintBlackboardView>();
                view.Window.InspectorView.SetDrawTarget(new BlueprintInspectorVariableView(_model));
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
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
            
            Add(_typeSelector);
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
            var window = GetFirstAncestorOfType<BlueprintBlackboardView>().Window;
            switch (_model.VariableType)
            {
                case VariableType.Argument:
                    window.DesignGraph.Current.InputArguments.Remove(_model);
                    break;
                case VariableType.OutArgument:
                case VariableType.Return:
                    window.DesignGraph.Current.OutputArguments.Remove(_model);
                    break;
                case VariableType.Local:
                    window.DesignGraph.Current.TemporaryVariables.Remove(_model);
                    break;
                case VariableType.Global:
                    window.DesignGraph.Variables.Remove(_model);
                    break;
            }
            RemoveFromHierarchy();
            window.GraphEditorView.Invalidate(GraphInvalidationType.Topology);
        }

        public void SetType(VisualElement sender, Type newType, Type oldType)
        {
            _model.Type = newType;
            var window = GetFirstAncestorOfType<BlueprintBlackboardView>().Window;
            window.GraphEditorView.Invalidate(GraphInvalidationType.RetypedNode);
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
