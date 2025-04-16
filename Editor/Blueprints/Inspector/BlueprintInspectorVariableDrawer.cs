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
            if (constructors.Length == 0 || model.Type == typeof(string) || model.Type.IsPrimitive)
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

    public class BlueprintInspectorSwitchView : VisualElement
    {
        private readonly BlueprintSwitchNodeView _model;
        private readonly SwitchNode _switchNode;
        private readonly List<int> _intCases = new();
        private readonly List<string> _stringCases = new();

        public BlueprintInspectorSwitchView(BlueprintSwitchNodeView model)
        {
            _model = model;
            _switchNode = (SwitchNode)model.Controller;
            
            if(!_switchNode.InputPins[PinNames.VALUE_IN].TryGetWire(out var wire) || !wire.IsConnected())
            {
                return;
            }
            
            if (!_switchNode.Method.Nodes.TryGetValue(wire.LeftGuid, out var node))
            {
                return;
            }

            var pin = node.OutputPins[wire.LeftName];
            var labelStyle = Resources.Load<StyleSheet>("LabelStyle");
            if (pin.Type.IsEnum)
            {
                var enumNames = pin.Type.GetEnumNames();
                foreach (var enumName in enumNames)
                {
                    var togEnum = new Toggle(ObjectNames.NicifyVariableName(enumName))
                    {
                        toggleOnLabelClick = false,
                    };
                    togEnum.SetValueWithoutNotify(_switchNode.Cases.Contains(enumName));
                    togEnum.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue)
                        {
                            _switchNode.AddCase(enumName);
                        }
                        else
                        {
                            _switchNode.RemoveCase(enumName);
                        }
                    });
                    togEnum.styleSheets.Add(labelStyle);
                    togEnum.AddToClassList("toggle-fix");
                    togEnum.Q<Label>().AddToClassList("flex-label");
                    Add(togEnum);
                }
            }
            else if (pin.Type == typeof(int))
            {
                _intCases.AddRange(_switchNode.Cases.Select(c => int.TryParse(c, out var result) ? result : 0));
                var intList = new ListView(_intCases, makeItem: () => new VisualElement(), bindItem: (element, i) =>
                {
                    element.Clear();
                    int idx = i;
                    var intField = new TextField("Case")
                    {
                        isDelayed = true,
                    };
                    intField.SetValueWithoutNotify(_intCases[i].ToString());
                    intField.RegisterValueChangedCallback(evt => 
                    {
                        if (int.TryParse(evt.newValue, out var caseInt))
                        {
                            var prev = int.Parse(evt.previousValue);
                            _switchNode.UpdateCase(prev.ToString(), caseInt.ToString());
                            _intCases[idx] = caseInt;
                        }
                        else
                        {
                            var prev = int.Parse(evt.previousValue);
                            intField.SetValueWithoutNotify(evt.previousValue);
                            _intCases[idx] = prev;
                        }
                    });
                    intField.styleSheets.Add(labelStyle);
                    intField.Q<Label>().AddToClassList("flex-label");
                    element.Add(intField);
                })
                {
                    showFoldoutHeader = true,
                    headerTitle = "Cases",
                    showAddRemoveFooter = true,
                    showBorder = true,
                    showBoundCollectionSize = false,
                    showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                    onRemove = lv =>
                    {
                        if (_intCases.Count <= 0)
                        {
                            lv.Rebuild();
                        }
                        var last = _intCases[^1];
                        _intCases.RemoveAt(_intCases.Count - 1);
                        _switchNode.RemoveCase(last.ToString());
                        lv.Rebuild();
                    },
                    onAdd = lv =>
                    {
                        int next = int.MinValue;
                        foreach (var t in _intCases)
                        {
                            if (t >= next)
                            {
                                next = t + 1;
                            }
                        }

                        if (next == int.MinValue)
                        {
                            next = 0;
                        }

                        _intCases.Add(next);
                        _switchNode.AddCase(_intCases[^1].ToString());
                    },
                    style =
                    {
                        marginLeft = 3f,
                        marginRight = 3f,
                    }
                };
                Add(intList);
            }
            else if (pin.Type == typeof(string))
            {
                _stringCases.AddRange(_switchNode.Cases);
                var stringList = new ListView(_stringCases, makeItem: () => new VisualElement(), bindItem: (element, i) =>
                {
                    element.Clear();
                    int idx = i;
                    var intField = new TextField("Case")
                    {
                        isDelayed = true,
                    };
                    intField.SetValueWithoutNotify(_stringCases[i]);
                    intField.RegisterValueChangedCallback(evt =>
                    {
                        _stringCases[idx] = evt.newValue;
                        _switchNode.UpdateCase(evt.previousValue, evt.newValue);
                    });
                    intField.styleSheets.Add(labelStyle);
                    intField.Q<Label>().AddToClassList("flex-label");
                    element.Add(intField);
                })
                {
                    showFoldoutHeader = true,
                    headerTitle = "Cases",
                    showAddRemoveFooter = true,
                    showBorder = true,
                    showBoundCollectionSize = false,
                    showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                    onRemove = lv =>
                    {
                        if (_stringCases.Count <= 0)
                        {
                            lv.Rebuild();
                        }
                        var last = _stringCases[^1];
                        _stringCases.RemoveAt(_stringCases.Count - 1);
                        _switchNode.RemoveCase(last);
                        lv.Rebuild();
                    },
                    onAdd = lv =>
                    {
                        _stringCases.Add(string.Empty);
                        _switchNode.AddCase(string.Empty);
                    },
                    style =
                    {
                        marginLeft = 3f,
                        marginRight = 3f,
                    }
                };
                Add(stringList);
            }
        }
    }

    public class BlueprintInspectorConstructorView : VisualElement
    {
        private readonly ConstructorNode _constructorNode;
        private readonly BlueprintConstructorNodeView _view;

        public BlueprintInspectorConstructorView(BlueprintConstructorNodeView model)
        {
            style.marginBottom = 4;
            style.marginTop = 4;
            style.marginLeft = 4;
            style.marginRight = 4;
            
            var labelStyle = Resources.Load<StyleSheet>("LabelStyle");
            _view = model;
            _constructorNode = (ConstructorNode)model.Controller;
            var toggleArray = new Toggle("Is Array")
            {
                toggleOnLabelClick = false,
            };
            toggleArray.SetValueWithoutNotify(_constructorNode.IsArray);
            toggleArray.RegisterValueChangedCallback(OnIsArrayChanged);
            toggleArray.styleSheets.Add(labelStyle);
            toggleArray.AddToClassList("toggle-fix");
            toggleArray.Q<Label>().AddToClassList("flex-label");
            Add(toggleArray);
        }

        private void OnIsArrayChanged(ChangeEvent<bool> evt)
        {
            _constructorNode.SetIsArray(evt.newValue);
            _view.UpdateIsArray();
        }
    }
}
