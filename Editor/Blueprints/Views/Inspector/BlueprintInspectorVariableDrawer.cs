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
using Vapor.Inspector;
using VaporEditor.Inspector;

namespace VaporEditor
{
    [CustomPropertyDrawer(typeof(BlueprintInspectorVariable))]
    public class BlueprintInspectorVariableDrawer : VaporPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var ve = new VisualElement()
            {
                style =
                {
                    flexGrow = 1f,
                }
            };
            ve.Add(new TextField("This Worked Too!"));
            var prop = SerializedDrawerUtility.DrawFieldFromObject(property.objectReferenceValue, property.objectReferenceValue.GetType());
            ve.Add(prop);
            return ve;
        }

        public override VisualElement CreateVaporPropertyGUI(TreePropertyField field)
        {
            var ve = new VisualElement()
            {
                style =
                {
                    flexGrow = 1f,
                }
            };
            ve.Add(new TextField("This Worked Too!"));
            var prop = SerializedDrawerUtility.DrawFieldFromObject(field.Property.GetValue() , field.Property.GetValue().GetType());
            ve.Add(prop);
            return ve;
        }
    }
    
    [System.Serializable]
    public class BlueprintInspectorVariable
    {
        [ReadOnly]
        public string VariableName;

        [ValueDropdown("@GetConstructors"), OnValueChanged("ConstructorChanged", true)]
        public string Constructor;
        
        private Type _type;
        
        public BlueprintInspectorVariable(string variableName, Type type)
        {
            VariableName = variableName;
            _type = type;
        }

        private void ConstructorChanged(string newValue)
        {
            Debug.Log($"{VariableName}: {newValue}");
            var constructor = GetConstructor(_type, newValue);
            // Fields.Clear();
            // if (constructor != null)
            // {
            //     foreach (var param in constructor.GetParameters())
            //     {
            //         var genType = typeof(FieldWrapper<>).MakeGenericType(param.ParameterType);
            //         Fields.Add((FieldWrapper)Activator.CreateInstance(genType));
            //     }
            // }
            // else
            // {
            //     var genType = typeof(FieldWrapper<>).MakeGenericType(_type);
            //     Fields.Add((FieldWrapper)Activator.CreateInstance(genType));
            // }
        }
        
        public IEnumerable<(string, string)> GetConstructors()
        {
            var constructors = _type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length == 0)
            {
                yield return ("Default Constructor", string.Empty);
            }
            foreach (var c in constructors)
            {
                var constructorSignature = FormatConstructorSignature(c);
                yield return (constructorSignature, constructorSignature);
            }
        }

        public static string FormatConstructorSignature(ConstructorInfo c)
        {
            // Get the parameter list as "Type paramName"
            string parameters = string.Join(", ", c.GetParameters()
                .Select(p => $"{TypeSelectorField.GetReadableTypeName(p.ParameterType)} {p.Name}"));

            // Format the constructor signature nicely
            string constructorSignature = $"{c.DeclaringType!.Name}({parameters})";
            return constructorSignature;
        }

        public static ConstructorInfo GetConstructor(Type type, string constructorSignature)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            return (from constructor in constructors let signature = FormatConstructorSignature(constructor) where signature.Equals(constructorSignature) select constructor).FirstOrDefault();
        }
        
        public static object InstantiateTypeByConstructor(Type type, string constructorSignature, params object[] args)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            foreach (var constructor in constructors)
            {
                // Get a formatted signature for comparison
                string signature = FormatConstructorSignature(constructor);
                if (signature.Equals(constructorSignature))
                {
                    return constructor.Invoke(args); // Instantiate the object
                }
            }

            throw new ArgumentException($"No constructor found for {type.Name} with signature: {constructorSignature}");
        }
    }

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

            Add(new Label(_model.Name)
            {
                style =
                {
                    marginLeft = 3,
                    marginTop = 3,
                    marginBottom = 3,
                    flexGrow = 1,
                }
            });
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
                    var wrapper = Activator.CreateInstance(wrappedType) as FieldWrapper;
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
                var wrapper = Activator.CreateInstance(wrappedType) as FieldWrapper;
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
}
