using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor;
using Vapor.Blueprints;
using VaporEditor.Blueprints;
using VaporEditor.Inspector;

namespace VaporEditor
{
    // [CustomPropertyDrawer(typeof(BlueprintInspectorVariable))]
    // public class BlueprintInspectorVariableDrawer : VaporPropertyDrawer
    // {
    //     public override VisualElement CreatePropertyGUI(SerializedProperty property)
    //     {
    //         var ve = new VisualElement()
    //         {
    //             style =
    //             {
    //                 flexGrow = 1f,
    //             }
    //         };
    //         ve.Add(new TextField("This Worked Too!"));
    //         var prop = SerializedDrawerUtility.DrawFieldFromObject(property.objectReferenceValue, property.objectReferenceValue.GetType());
    //         ve.Add(prop);
    //         return ve;
    //     }
    //
    //     public override VisualElement CreateVaporPropertyGUI(TreePropertyField field)
    //     {
    //         var ve = new VisualElement()
    //         {
    //             style =
    //             {
    //                 flexGrow = 1f,
    //             }
    //         };
    //         ve.Add(new TextField("This Worked Too!"));
    //         var prop = SerializedDrawerUtility.DrawFieldFromObject(field.Property.GetValue() , field.Property.GetValue().GetType());
    //         ve.Add(prop);
    //         return ve;
    //     }
    // }
    
    // [System.Serializable]
    // public class BlueprintInspectorVariable
    // {
    //     [ReadOnly]
    //     public string VariableName;
    //
    //     [ValueDropdown("@GetConstructors"), OnValueChanged("ConstructorChanged", true)]
    //     public string Constructor;
    //     
    //     private Type _type;
    //     
    //     public BlueprintInspectorVariable(string variableName, Type type)
    //     {
    //         VariableName = variableName;
    //         _type = type;
    //     }
    //
    //     private void ConstructorChanged(string newValue)
    //     {
    //         Debug.Log($"{VariableName}: {newValue}");
    //         var constructor = GetConstructor(_type, newValue);
    //         // Fields.Clear();
    //         // if (constructor != null)
    //         // {
    //         //     foreach (var param in constructor.GetParameters())
    //         //     {
    //         //         var genType = typeof(FieldWrapper<>).MakeGenericType(param.ParameterType);
    //         //         Fields.Add((FieldWrapper)Activator.CreateInstance(genType));
    //         //     }
    //         // }
    //         // else
    //         // {
    //         //     var genType = typeof(FieldWrapper<>).MakeGenericType(_type);
    //         //     Fields.Add((FieldWrapper)Activator.CreateInstance(genType));
    //         // }
    //     }
    //     
    //     public IEnumerable<(string, string)> GetConstructors()
    //     {
    //         var constructors = _type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
    //         if (constructors.Length == 0)
    //         {
    //             yield return ("Default Constructor", string.Empty);
    //         }
    //         foreach (var c in constructors)
    //         {
    //             var constructorSignature = FormatConstructorSignature(c);
    //             yield return (constructorSignature, constructorSignature);
    //         }
    //     }
    //
    //     public static string FormatConstructorSignature(ConstructorInfo c)
    //     {
    //         // Get the parameter list as "Type paramName"
    //         string parameters = string.Join(", ", c.GetParameters()
    //             .Select(p => $"{TypeSelectorField.GetReadableTypeName(p.ParameterType)} {p.Name}"));
    //
    //         // Format the constructor signature nicely
    //         string constructorSignature = $"{c.DeclaringType!.Name}({parameters})";
    //         return constructorSignature;
    //     }
    //
    //     public static ConstructorInfo GetConstructor(Type type, string constructorSignature)
    //     {
    //         var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
    //         return (from constructor in constructors let signature = FormatConstructorSignature(constructor) where signature.Equals(constructorSignature) select constructor).FirstOrDefault();
    //     }
    //     
    //     public static object InstantiateTypeByConstructor(Type type, string constructorSignature, params object[] args)
    //     {
    //         if (type == null) throw new ArgumentNullException(nameof(type));
    //
    //         var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
    //
    //         foreach (var constructor in constructors)
    //         {
    //             // Get a formatted signature for comparison
    //             string signature = FormatConstructorSignature(constructor);
    //             if (signature.Equals(constructorSignature))
    //             {
    //                 return constructor.Invoke(args); // Instantiate the object
    //             }
    //         }
    //
    //         throw new ArgumentException($"No constructor found for {type.Name} with signature: {constructorSignature}");
    //     }
    // }

    public class BlueprintInspectorVariableView : VisualElement
    {
        private readonly BlueprintVariable _model;
        private readonly VisualElement _parameterContainer;

        public BlueprintInspectorVariableView(BlueprintVariable model)
        {
            _model = model;
            
            var constructors = _model.Type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Where(c => !c.GetParameters().Any(p => p.ParameterType.IsInterface)).ToArray();
            List<string> choices = new(constructors.Length);
            List<object> values = new(constructors.Length);
            if (constructors.Length == 0)
            {
                choices.Add("Default(T)");
                values.Add(null);
            }
            foreach (var c in constructors)
            {
                var constructorSignature = FormatConstructorSignature(c);
                choices.Add(constructorSignature);
                values.Add(c);
            }

            List<VariableAccessModifier> accessChoices = new() { VariableAccessModifier.Public, VariableAccessModifier.Protected, VariableAccessModifier.Private };
            var popup = new PopupField<VariableAccessModifier>("Access", accessChoices, _model.AccessModifier,
                v => v.ToString(), v => v.ToString());
            popup.RegisterValueChangedCallback(OnAccessModifierChanged);
            popup.styleSheets.Add(Resources.Load<StyleSheet>("LabelStyle"));
            popup.Q<Label>().AddToClassList("flex-label");

            var idx = choices.IndexOf(_model.ConstructorName);
            idx = idx < 0 ? 0 : idx;
            var combo = new ComboBox("Constructor", idx, choices, values, false);
            combo.SelectionChanged += OnConstructorChanged;

            Add(popup);
            Add(combo);
            _parameterContainer = new VisualElement();
            Add(_parameterContainer);
        }

        private void OnAccessModifierChanged(ChangeEvent<VariableAccessModifier> evt)
        {
            _model.AccessModifier = evt.newValue;
        }

        private void OnConstructorChanged(ComboBox comboBox, List<int> indices)
        {
            if(!indices.IsValidIndex(0)) return;
            var constructor = comboBox.Values[indices[0]];
            var formattedName = comboBox.Choices[indices[0]];
            _model.ConstructorName = formattedName;
            _parameterContainer.Clear();
            if (constructor != null)
            {
                var c = (ConstructorInfo)constructor;
                var parameters = c.GetParameters();
                if (_model.ParameterValues.Count != parameters.Length)
                {
                    _model.ParameterValues.Clear();
                    foreach (var p in parameters)
                    {
                        if(p.ParameterType.IsSerializable)
                        {
                            _model.ParameterValues.Add(FormatterServices.GetUninitializedObject(p.ParameterType));
                        }
                        else if (_model.Type.IsValueType)
                        {
                            _model.ParameterValues.Add(Activator.CreateInstance(_model.Type));
                        }
                        else
                        {
                            _model.ParameterValues.Add(null);
                        }
                    }
                }
                else
                {
                    bool allMatch = !parameters.Where((t, i) => t.ParameterType != _model.ParameterValues[i].GetType()).Any();
                    if (!allMatch)
                    {
                        _model.ParameterValues.Clear();
                        foreach (var p in parameters)
                        {
                            if(p.ParameterType.IsSerializable)
                            {
                                _model.ParameterValues.Add(FormatterServices.GetUninitializedObject(p.ParameterType));
                            }
                            else if (_model.Type.IsValueType)
                            {
                                _model.ParameterValues.Add(Activator.CreateInstance(_model.Type));
                            }
                            else
                            {
                                _model.ParameterValues.Add(null);
                            }
                        }
                    }
                }
                
                int idx = 0;
                foreach (var param in parameters)
                {
                    int myIndex = idx;
                    var wrappedType = typeof(FieldWrapper<>).MakeGenericType(param.ParameterType);
                    var wrapper = (FieldWrapper)Activator.CreateInstance(wrappedType);
                    wrapper.Set(_model.ParameterValues[myIndex]);
                    wrapper.WrappedValueChanged += o => _model.ParameterValues[myIndex] = o;
                    var ve = SerializedDrawerUtility.DrawFieldFromObject(wrapper, wrapper.GetType());
                    ve.Q<Label>().text = ObjectNames.NicifyVariableName(param.Name);
                    _parameterContainer.Add(ve);
                    idx++;
                }
            }
            else
            {
                if (_model.ParameterValues.Count != 1 || _model.ParameterValues[0].GetType() != _model.Type)
                {
                    _model.ParameterValues.Clear();
                    if(_model.Type.IsSerializable)
                    {
                        _model.ParameterValues.Add(FormatterServices.GetUninitializedObject(_model.Type));
                    }
                    else if (_model.Type.IsValueType)
                    {
                        _model.ParameterValues.Add(Activator.CreateInstance(_model.Type));
                    }
                    else
                    {
                        _model.ParameterValues.Add(null);
                    }
                }

                var wrappedType = typeof(FieldWrapper<>).MakeGenericType(_model.Type);
                var wrapper = (FieldWrapper)Activator.CreateInstance(wrappedType);
                wrapper.Set(_model.ParameterValues[0]);
                wrapper.WrappedValueChanged += o => _model.ParameterValues[0] = o;
                var ve = SerializedDrawerUtility.DrawFieldFromObject(wrapper, wrapper.GetType());
                ve.Q<Label>().text = "Default";
                _parameterContainer.Add(ve);
            }
        }

        public static string FormatConstructorSignature(ConstructorInfo c)
        {
            // Get the parameter list as "Type paramName"
            string parameters = string.Join(", ", c.GetParameters()
                .Select(p => $"{TypeSelectorField.GetReadableTypeName(p.ParameterType)} {p.Name}"));

            // Format the constructor signature nicely
            string constructorSignature = $"{TypeSelectorField.GetReadableTypeName(c.DeclaringType)}({parameters})";
            return constructorSignature;
        }
    }

    public class BlueprintInspectorMethodView : VisualElement
    {
        private readonly BlueprintMethodGraph _model;
        private readonly List<BlueprintArgument> _inputArguments = new List<BlueprintArgument>();
        private readonly List<BlueprintArgument> _outputArguments = new List<BlueprintArgument>();

        public BlueprintInspectorMethodView(BlueprintMethodGraph model)
        {
            _model = model;
            var labelStyle = Resources.Load<StyleSheet>("LabelStyle");
            if (_model.IsOverride)
            {
                var lbl = new Label("Method is Override")
                {
                    style =
                    {
                        marginTop = 6f,
                        marginLeft = 6f,
                        fontSize = 16,
                        color = Color.grey
                    }
                };
                lbl.AddToClassList("flex-label");
                Add(lbl);
                return;
            }

            var abstractField = new Toggle("Abstract")
            {
                toggleOnLabelClick = false
            };
            abstractField.SetValueWithoutNotify(_model.IsAbstract);
            abstractField.RegisterValueChangedCallback(evt => _model.IsAbstract = evt.newValue);
            abstractField.styleSheets.Add(labelStyle);
            abstractField.AddToClassList("toggle-fix");
            abstractField.Q<Label>().AddToClassList("flex-label");
            
            var virtualField = new Toggle("Virtual")
            {
                toggleOnLabelClick = false
            };
            virtualField.SetValueWithoutNotify(_model.IsVirtual);
            virtualField.RegisterValueChangedCallback(evt => _model.IsVirtual = evt.newValue);
            virtualField.styleSheets.Add(labelStyle);
            virtualField.AddToClassList("toggle-fix");
            virtualField.Q<Label>().AddToClassList("flex-label");
            
            var pureField = new Toggle("Pure")
            {
                toggleOnLabelClick = false
            };
            pureField.SetValueWithoutNotify(_model.IsPure);
            pureField.RegisterValueChangedCallback(evt => _model.IsPure = evt.newValue);
            pureField.styleSheets.Add(labelStyle);
            pureField.AddToClassList("toggle-fix");
            pureField.Q<Label>().AddToClassList("flex-label");

            List<VariableAccessModifier> accessChoices = new() { VariableAccessModifier.Public, VariableAccessModifier.Protected, VariableAccessModifier.Private };
            var popup = new PopupField<VariableAccessModifier>("Access", accessChoices, _model.AccessModifier,
                v => v.ToString(), v => v.ToString());
            popup.RegisterValueChangedCallback(evt => _model.AccessModifier = evt.newValue);
            popup.styleSheets.Add(labelStyle);
            popup.Q<Label>().AddToClassList("flex-label");

            Add(abstractField);
            Add(virtualField);
            Add(pureField);
            Add(popup);

            _inputArguments.AddRange(_model.Arguments.Where(arg => !arg.IsOut && !arg.IsReturn));
            var inputArgView = new ListView(_inputArguments, makeItem: () => new VisualElement(), bindItem: (element, i) =>
            {
                element.Clear();
                var argument = _inputArguments[i];
                var ve = new VisualElement()
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        flexGrow = 1f,
                        flexShrink = 1f,
                    }
                };

                var nameField = new TextField
                {
                    style =
                    {
                        flexBasis = Length.Percent(50f)
                    }
                };
                nameField.SetValueWithoutNotify(argument.DisplayName);
                nameField.RegisterValueChangedCallback(evt => argument.SetName(evt.newValue));
                ve.Add(nameField);

                var typeSelector = new TypeSelectorField(string.Empty, argument.Type)
                {
                    style =
                    {
                        flexBasis = Length.Percent(50f),
                    }
                };
                typeSelector.TypeChanged += (_, cur, _) => argument.SetType(cur);
                ve.Add(typeSelector);
                element.Add(ve);

                var passByRef = new Toggle("Pass By Ref")
                {
                    toggleOnLabelClick = false
                };
                passByRef.SetValueWithoutNotify(argument.IsRef);
                passByRef.RegisterValueChangedCallback(evt => argument.IsRef = evt.newValue);
                element.Add(passByRef);
            })
            {
                showFoldoutHeader = true,
                headerTitle = "Input Arguments",
                showAddRemoveFooter = true,
                showBorder = true,
                showBoundCollectionSize = false,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                onAdd = OnAddInputArg,
                onRemove = OnRemoveInputArg,
                fixedItemHeight = 44,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                style =
                {
                    marginLeft = 3f,
                    marginRight = 3f,
                    flexGrow = 1f,
                    flexShrink = 1f,
                },
            };
            inputArgView.itemIndexChanged += OnReorderArguments;
            Add(inputArgView);

            _outputArguments.AddRange(_model.Arguments.Where(arg => arg.IsOut || arg.IsReturn));
            var outArgView = new ListView(_outputArguments, makeItem: () => new VisualElement(), bindItem: (element, i) =>
            {
                element.Clear();
                var argument = _outputArguments[i];
                var ve = new VisualElement()
                {
                    style =
                    {
                        marginLeft = 3f,
                        flexDirection = FlexDirection.Row,
                    }
                };

                var nameField = new TextField
                {
                    style =
                    {
                        flexBasis = Length.Percent(50f),
                    }
                };
                nameField.SetValueWithoutNotify(argument.DisplayName);
                nameField.RegisterValueChangedCallback(evt => argument.SetName(evt.newValue));
                ve.Add(nameField);

                var typeSelector = new TypeSelectorField(string.Empty, argument.Type)
                {
                    style =
                    {
                        flexBasis = Length.Percent(50f),
                    }
                };
                typeSelector.TypeChanged += (_, cur, _) => argument.SetType(cur);
                ve.Add(typeSelector);

                element.Add(ve);
            })
            {
                showFoldoutHeader = true,
                headerTitle = "Output Arguments",
                showAddRemoveFooter = true,
                showBorder = true,
                showBoundCollectionSize = false,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                onAdd = OnAddOutputArg,
                onRemove = OnRemoveOutputArg,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                style =
                {
                    marginLeft = 3f,
                    marginRight = 3f,
                }
            };
            outArgView.itemIndexChanged += (o, n) =>
            {
                if (n == 0)
                {
                    _outputArguments[n].IsReturn = true;
                    _outputArguments[n].IsOut = false;
                    if (_outputArguments.IsValidIndex(1))
                    {
                        _outputArguments[1].IsReturn = false;
                        _outputArguments[1].IsOut = true;
                    }
                }

                OnReorderArguments(o, n);
            };
            Add(outArgView);
        }

        private void OnAddInputArg(BaseListView obj)
        {
            Debug.Log("Add Input Argument");
            var arg = _model.AddInputArgument(typeof(bool));
            _inputArguments.Add(arg);
            OnReorderArguments(-1, -1);
        }

        private void OnRemoveInputArg(BaseListView obj)
        {
            if (_inputArguments.Count == 0)
            {
                return;
            }

            var argument = _inputArguments[^1];
            _inputArguments.RemoveAt(_inputArguments.Count - 1);
            _model.RemoveArgument(argument);
            OnReorderArguments(-1, -1);
            obj.Rebuild();
        }
        
        private void OnAddOutputArg(BaseListView obj)
        {
            Debug.Log("Add Output Argument");
            var arg = _model.AddOutputArgument(typeof(bool));
            _outputArguments.Add(arg);
            OnReorderArguments(-1, -1);
        }

        private void OnRemoveOutputArg(BaseListView obj)
        {
            if (_outputArguments.Count == 0)
            {
                return;
            }

            var argument = _outputArguments[^1];
            _outputArguments.RemoveAt(_outputArguments.Count - 1);
            _model.RemoveArgument(argument);
            OnReorderArguments(-1, -1);
            obj.Rebuild();
        }

        private void OnReorderArguments(int oldIndex, int newIndex)
        {
            for (int i = 0; i < _inputArguments.Count; i++)
            {
                _inputArguments[i].ParameterIndex = i;
            }

            for (int i = 0; i < _outputArguments.Count; i++)
            {
                int shiftedIndex = i + _inputArguments.Count;
                if (_outputArguments[i].IsReturn)
                {
                    _outputArguments[i].ParameterIndex = -1;
                }
                else
                {
                    _outputArguments[i].ParameterIndex = shiftedIndex;
                }
            }
            _model.OnArgumentsReordered();
        }
    }

    public class BlueprintInspectorSwitchView : VisualElement
    {
        public BlueprintInspectorSwitchView(BlueprintSwitchNodeView model)
        {
            var controller = model.Controller;
                
            var wire = controller.InputWires.FirstOrDefault(w => w.RightSidePin.PinName == PinNames.VALUE_IN);
            if (!wire.IsValid())
            {
                return;
            }
            
            var node = controller.Method.Nodes.FirstOrDefault(n => n.Value.Guid == wire.LeftSidePin.NodeGuid).Value;
            if (node == null)
            {
                return;
            }

            var pin = node.NodeType == NodeType.Conversion ? node.InputPins[PinNames.SET_IN] : node.OutputPins[wire.LeftSidePin.PinName];
            if (!pin.Type.IsEnum)
            {
                return;
            }

            var enumNames = pin.Type.GetEnumNames();
            foreach (var enumName in enumNames)
            {
                Add(new Label(enumName));
            }
        }
    }
}
