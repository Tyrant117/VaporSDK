using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
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

            _methodCategory = new BlueprintBlackboardMethodCategory("Methods", Type.GetType(Window.GraphObject.AssemblyQualifiedTypeName), OnAddMethod);
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
            
            foreach (var v in Window.GraphObject.DesignGraph.Methods)
            {
                _methodCategory.Add(v.IsOverride ? new BlueprintBlackboardMethod(v.MethodInfo) : new BlueprintBlackboardMethod(v.MethodName));
            }
            
            foreach (var v in Window.GraphObject.DesignGraph.Variables)
            {
                _variables.Add(new BlueprintBlackboardVariable(v.Name, BlueprintBlackboardVariable.Group.Global, v.Type));
            }
        }

        private void OnAddMethod()
        {
            var nm = GetNextName("Method");
            Window.GraphObject.DesignGraph.AddMethod(nm, null);
            var method = new BlueprintBlackboardMethod(nm);
            _methodCategory.Add(method);
            _methodCategory.SetState(true);
            method.StartRename();
        }

        public void OnOverrideMethod(MethodInfo method)
        {
            if (Window.GraphObject.DesignGraph.Methods.Exists(m => m.MethodName == method.Name))
            {
                return;
            }

            Window.GraphObject.DesignGraph.AddMethod(method.Name, method);
            _methodCategory.Add(new BlueprintBlackboardMethod(method));
        }

        private void OnAddVariable()
        {
            var nm = GetNextName("Var");
            Window.GraphObject.DesignGraph.AddVariable(nm, typeof(bool));
            var variable = new BlueprintBlackboardVariable(nm, BlueprintBlackboardVariable.Group.Global);
            _variables.Add(variable);
            _variables.SetState(true);
            variable.StartRename();
        }
        
        private void OnAddInputArgument()
        {
            var nm = GetNextName("InputArg");
            Window.GraphObject.DesignGraph.Current.AddInputArgument(nm, typeof(bool));
            var variable = new BlueprintBlackboardVariable(nm, BlueprintBlackboardVariable.Group.Input);
            _inputArguments.Add(variable);
            _inputArguments.SetState(true);
            variable.StartRename();
            Window.GraphEditorView.Invalidate(GraphInvalidationType.Graph);
        }

        private void OnAddOutputArgument()
        {
            var nm = GetNextName("OutputArg");
            Window.GraphObject.DesignGraph.Current.AddOutputArgument(nm, typeof(bool));
            var variable = new BlueprintBlackboardVariable(nm, BlueprintBlackboardVariable.Group.Output);
            _outputArguments.Add(variable);
            _outputArguments.SetState(true);
            variable.StartRename();
            Window.GraphEditorView.Invalidate(GraphInvalidationType.Graph);
        }

        private void OnAddLocalVariable()
        {
            var nm = GetNextName("LocalVar");
            Window.GraphObject.DesignGraph.Current.AddTemporaryVariable(nm, typeof(bool));
            var variable = new BlueprintBlackboardVariable(nm, BlueprintBlackboardVariable.Group.Local);
            _localVariables.Add(variable);
            _localVariables.SetState(true);
            variable.StartRename();
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
                    while (Window.GraphObject.DesignGraph.Current.InputArguments.Exists(m => m.Name == $"{prefix}_{_counter}"))
                    {
                        _counter++;
                    }
                    break;
                }
                case "Output":
                {
                    while (Window.GraphObject.DesignGraph.Current.OutputArguments.Exists(m => m.Name == $"{prefix}_{_counter}"))
                    {
                        _counter++;
                    }
                    break;
                }
                case "LocalVar":
                {
                    while (Window.GraphObject.DesignGraph.Variables.Exists(m => m.Name == $"{prefix}_{_counter}"))
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
            foreach (var v in Window.GraphObject.DesignGraph.Current.InputArguments)
            {
                _inputArguments.Add(new BlueprintBlackboardVariable(v.Name, BlueprintBlackboardVariable.Group.Input, v.Type));
            }
        }

        public void UpdateOutputArguments()
        {
            _outputArguments.Clear();
            foreach (var v in Window.GraphObject.DesignGraph.Current.OutputArguments)
            {
                _outputArguments.Add(new BlueprintBlackboardVariable(v.Name, BlueprintBlackboardVariable.Group.Output, v.Type));
            }
        }
        
        public void UpdateTemporaryVariables()
        {
            _localVariables.Clear();
            foreach (var v in Window.GraphObject.DesignGraph.Current.TemporaryVariables)
            {
                _localVariables.Add(new BlueprintBlackboardVariable(v.Name, BlueprintBlackboardVariable.Group.Local, v.Type));
            }
        }

        public void ShowConditionalMethodFoldouts()
        {
            var current  = Window.GraphObject.DesignGraph.Current;
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

        private readonly Foldout _foldout;
        private readonly Button _overrideSelector;

        private readonly List<MethodInfo> _overridableMethods;
        private readonly List<GenericDescriptor> _descriptors;
        private readonly List<GenericDescriptor> _filteredDescriptors;

        public BlueprintBlackboardMethodCategory(string header, Type declaringType, Action addMethodCallback)
        {
            _overridableMethods = ReflectionUtility.GetAllMethodsThatMatch(declaringType, mi => mi.IsVirtual || mi.IsAbstract, false).ToList();
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
            var view = GetFirstAncestorOfType<BlueprintBlackboardView>();
            var screenPosition = _overrideSelector.worldBound.position + view.Window.position.position;
            
            _filteredDescriptors.Clear();
            foreach (var descriptor in _descriptors.Where(descriptor => !view.Window.GraphObject.DesignGraph.Methods.Exists(m => m.MethodName == descriptor.Name)))
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
            
            GetFirstAncestorOfType<BlueprintBlackboardView>().OnOverrideMethod(mi);
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
                    window.GraphObject.DesignGraph.Methods.First(v => v.MethodName == _name).MethodName = trimmed;
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
            displayName = method.IsSpecialName ? BlueprintNodeDataModelUtility.ToTitleCase(displayName) : displayName;
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
            view.Window.GraphObject.DesignGraph.SelectMethod(_name);
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
            var current = window.GraphObject.DesignGraph.Current;
            window.GraphObject.DesignGraph.Methods.RemoveAll(m => m.MethodName == _name);
            
            RemoveFromHierarchy();
            if (current != null && current.MethodName == _name)
            {
                window.GraphObject.DesignGraph.Current = null;
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
        public enum Group
        {
            Input,
            Output,
            Local,
            Global,
        }
        private readonly Label _label;
        private readonly TextField _renameTextField;
        private readonly Label _typeLabel;
        private readonly Button _typeSelector;
        private string _name;
        private readonly Group _group;
        private GenericTypeBuilder _typeStack;

        public Type CurrentType { get; private set; }

        public override bool focusable => true;

        public BlueprintBlackboardVariable(string name, Group group)
        {
            style.flexGrow = 1f;
            style.flexDirection = FlexDirection.Row;
            style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);
            style.marginLeft = 0f;
            _name = name;
            _group = group;

            var ve = new VisualElement()
            {
                style =
                {
                    flexGrow = 1f,
                }
            };
            _label = new Label(name)
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
                    switch (_group)
                    {
                        case Group.Input:
                            window.GraphObject.DesignGraph.Current.InputArguments.First(v => v.Name == _name).Name = trimmed;
                            break;
                        case Group.Output:
                            window.GraphObject.DesignGraph.Current.OutputArguments.First(v => v.Name == _name).Name = trimmed;
                            break;
                        case Group.Local:
                            window.GraphObject.DesignGraph.Current.TemporaryVariables.First(v => v.Name == _name).Name = trimmed;
                            break;
                        case Group.Global:
                            window.GraphObject.DesignGraph.Variables.First(v => v.Name == _name).Name = trimmed;
                            break;
                    }
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

            
            
            CurrentType = typeof(bool);
            _typeLabel = new Label(CurrentType.Name)
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
            _typeSelector = new Button(OnSelectType)
            {
                text = "T",
                style =
                {
                    width = 20,
                    height = 20,
                    unityFontStyleAndWeight = FontStyle.Bold,
                }
            };
            
            RegisterCallback<FocusInEvent>(_ =>
            {
                style.backgroundColor = new Color(0.16f, 0.16f, 0.2f);
                // var view = GetFirstAncestorOfType<BlueprintBlackboardView>();
                // view.DrawField(this, CurrentType);
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
            
            Add(_typeLabel);
            Add(_typeSelector);
        }

        public BlueprintBlackboardVariable(string name, Group group, Type type) : this(name, group)
        {
            _typeLabel.text = type.IsGenericType ? GetReadableTypeName(type) : type.Name;
            CurrentType = type;
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
            switch (_group)
            {
                case Group.Input:
                    window.GraphObject.DesignGraph.Current.InputArguments.RemoveAll(v => v.Name == _name);
                    break;
                case Group.Output:
                    window.GraphObject.DesignGraph.Current.OutputArguments.RemoveAll(v => v.Name == _name);
                    break;
                case Group.Local:
                    window.GraphObject.DesignGraph.Current.TemporaryVariables.RemoveAll(v => v.Name == _name);
                    break;
                case Group.Global:
                    window.GraphObject.DesignGraph.Variables.RemoveAll(v => v.Name == _name);
                    break;
            }
            RemoveFromHierarchy();
            window.GraphEditorView.Invalidate(GraphInvalidationType.Topology);
        }

        private void OnSelectType()
        {
            _typeStack = null;
            var view = GetFirstAncestorOfType<BlueprintBlackboardView>();
            var screenPosition = _typeSelector.worldBound.position + view.Window.position.position;
            BlueprintSearchWindow.Show(screenPosition, screenPosition, new TypeSearchProvider(OnTypeSelected), true, false);
        }

        private void OnTypeSelected(BlueprintSearchModel model, Vector2 position)
        {
            if (model.ModelType.IsGenericType)
            {
                var cachedGenericType = model.ModelType.GetGenericTypeDefinition();
                var genericTypeArguments = new Type[cachedGenericType.GetGenericArguments().Length];
                if (_typeStack == null)
                {
                    _typeStack = new GenericTypeBuilder(cachedGenericType, genericTypeArguments);
                }
                else
                {
                    _typeStack.StackGenericType(cachedGenericType, genericTypeArguments);
                }
                
                var view = GetFirstAncestorOfType<BlueprintBlackboardView>();
                var screenPosition = _typeSelector.worldBound.position + view.Window.position.position;
                BlueprintSearchWindow.Show(screenPosition, screenPosition, new TypeSearchProvider(OnTypeSelected), true, false);
            }
            else if(_typeStack != null)
            {
                _typeStack.AddType(model.ModelType);
                if (_typeStack.Complete)
                {
                    SetType(_typeStack.MakeType());
                }
                else
                {
                    var view = GetFirstAncestorOfType<BlueprintBlackboardView>();
                    var screenPosition = _typeSelector.worldBound.position + view.Window.position.position;
                    BlueprintSearchWindow.Show(screenPosition, screenPosition, new TypeSearchProvider(OnTypeSelected), true, false);
                }
            }
            else
            {
                SetType(model.ModelType);
            }
        }

        public void SetType(Type type)
        {
            CurrentType = type;
            _typeLabel.text = CurrentType.IsGenericType ? GetReadableTypeName(CurrentType) : CurrentType.Name;
            var window = GetFirstAncestorOfType<BlueprintBlackboardView>().Window;
            switch (_group)
            {
                case Group.Input:
                    window.GraphObject.DesignGraph.Current.InputArguments.First(v => v.Name == _name).Type = CurrentType;
                    break;
                case Group.Output:
                    window.GraphObject.DesignGraph.Current.OutputArguments.First(v => v.Name == _name).Type = CurrentType;
                    break;
                case Group.Local:
                    window.GraphObject.DesignGraph.Current.TemporaryVariables.First(v => v.Name == _name).Type = CurrentType;
                    break;
                case Group.Global:
                    window.GraphObject.DesignGraph.Variables.First(v => v.Name == _name).Type = CurrentType;
                    break;
            }
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
        
        public static string GetReadableTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                // Get the base type name without `1, `2, etc.
                string baseTypeName = type.Name.Split('`')[0];

                // Recursively resolve generic type arguments
                string genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetReadableTypeName));

                return $"{baseTypeName}<{genericArgs}>";
            }
            else
            {
                return type.Name;
            }
        }
    }
}
