using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Inspector;
using Vapor.Keys;
using VaporEditor.Inspector;

namespace VaporEditor.Keys
{
    [CustomPropertyDrawer(typeof(KeyDropdownValue))]
    public class KeyDropdownValueDrawer : VaporPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            List<string> keys = new();
            List<KeyDropdownValue> values = new();
            var atr = fieldInfo.GetCustomAttribute<ValueDropdownAttribute>();
            if (atr == null)
            {
                return new Label($"{property.displayName} must implement {TooltipMarkup.ClassMarkup(nameof(ValueDropdownAttribute))}");
            }
            IList convert;
            if (atr.AssemblyQualifiedType == null)
            {
                var mi = ReflectionUtility.GetMember(fieldInfo.DeclaringType, atr.Resolver);
                if (!ReflectionUtility.TryResolveMemberValue<IList>(null, mi, null, out convert))
                {
                    return new Label($"{property.displayName} must have a fully qualified type. " +
                        $"\nMust resolve to a Tuple<{TooltipMarkup.LangWordMarkup("string")},{TooltipMarkup.StructMarkup(nameof(KeyDropdownValue))}>");
                }
            }else
            {
                convert = GetKeysField(atr.AssemblyQualifiedType, atr.Resolver);
            }

            string name = property.displayName;
            float? fixedWidth = null;
            if (fieldInfo.FieldType == typeof(List<KeyDropdownValue>))
            {
                int index = property.propertyPath.IndexOf(".Array");

                // If ".Array" is found, return the substring before it; otherwise, return the original string
                var propName = index >= 0 ? property.propertyPath.Substring(0, index) : fieldInfo.Name;

                var outerProp = property.serializedObject.FindProperty(propName);
                for (int i = 0; i < outerProp.arraySize; i++)
                {
                    if (property.propertyPath == outerProp.GetArrayElementAtIndex(i).propertyPath)
                    {
                        name = $"Element {i}";
                        fixedWidth = 120f;
                        break;
                    }
                }
                
            }

            ConvertToTupleList(keys, values, convert);
            var foldout = new StyledFoldoutProperty(name);
            var tooltip = "";
            if (fieldInfo.IsDefined(typeof(RichTextTooltipAttribute), true))
            {
                var rtAtr = fieldInfo.GetCustomAttribute<RichTextTooltipAttribute>();
                tooltip = rtAtr.Tooltip;
            }
            if (atr.Searchable)
            {
                var indexOfCurrent = values.IndexOf((KeyDropdownValue)property.boxedValue);
                string current = (indexOfCurrent < 0 || indexOfCurrent > keys.Count - 1) ? "None" : keys[indexOfCurrent];
                var dropdown = new SearchableDropdown<string>("", current)
                {
                    name = fieldInfo.Name,
                    userData = (property, values),
                    tooltip = tooltip,
                    style =
                    {
                        flexGrow = 1
                    }
                };
                if (fixedWidth.HasValue)
                {
                    dropdown.style.minWidth = fixedWidth.Value;
                }
                else
                {
                    dropdown.AddToClassList("unity-base-field__aligned");
                }
                dropdown.SetChoices(keys);
                dropdown.ValueChanged += OnSearchableDropdownChanged; 
                foldout.SetHeaderProperty(dropdown);
            }
            else
            {
                Debug.Log((KeyDropdownValue)property.boxedValue);
                var indexOfCurrent = values.IndexOf((KeyDropdownValue)property.boxedValue);
                int defaultInex = (indexOfCurrent < 0 || indexOfCurrent > keys.Count - 1) ? 0 : indexOfCurrent;
                var dropdown = new DropdownField("", keys, defaultInex)
                {
                    name = fieldInfo.Name,
                    tooltip = tooltip,
                    userData = (property, values),
                    style =
                    {
                        flexGrow = 1
                    }
                };
                if (fixedWidth.HasValue)
                {
                    dropdown.style.minWidth = fixedWidth.Value;
                }
                else
                {
                    dropdown.AddToClassList("unity-base-field__aligned");
                }
                dropdown.RegisterValueChangedCallback(OnDropdownChanged);
                foldout.SetHeaderProperty(dropdown);
            }

            var guidBox = new StyledHorizontalGroup();
            var guidField = new PropertyField(property.FindPropertyRelative("Guid"))
            {
                style = { flexGrow = 1}
            };
            guidBox.Add(guidField);
            guidField.SetEnabled(false);
            guidBox.Add(new Button(() => OnSelectClicked(property))
            {
                text = "Select"
            });

            var keyBox = new StyledHorizontalGroup();
            var keyField = new PropertyField(property.FindPropertyRelative("Key"))
            {
                style = { flexGrow = 1}
            };
            keyField.SetEnabled(false);
            keyBox.Add(keyField);
            keyBox.Add(new Button(() => OnRemapClicked(property))
            {
                text = "Re-Map",
            });

            foldout.Add(guidBox);
            foldout.Add(keyBox);
            return foldout;
        }

        public override VisualElement CreateVaporPropertyGUI(InspectorTreeProperty property)
        {
            string displayName = property.DisplayName;
            List<string> keys = new();
            List<KeyDropdownValue> values = new();
            property.TryGetAttribute<ValueDropdownAttribute>(out var atr);
            if (atr == null)
            {
                return new Label($"{displayName} must implement {TooltipMarkup.ClassMarkup(nameof(ValueDropdownAttribute))}");
            }
            IList convert;
            if (atr.AssemblyQualifiedType == null)
            {
                var mi = ReflectionUtility.GetMember(property.GetParentObject().GetType(), atr.Resolver);
                if (!ReflectionUtility.TryResolveMemberValue<IList>(null, mi, null, out convert))
                {
                    return new Label($"{displayName} must have a fully qualified type. " +
                        $"\nMust resolve to a Tuple<{TooltipMarkup.LangWordMarkup("string")},{TooltipMarkup.StructMarkup(nameof(KeyDropdownValue))}>");
                }
            }
            else
            {
                convert = GetKeysField(atr.AssemblyQualifiedType, atr.Resolver);
            }

            string name = displayName;
            float? fixedWidth = null;
            if (property.PropertyType == typeof(List<KeyDropdownValue>))
            {
                int index = property.PropertyPath.IndexOf(".Array");

                // If ".Array" is found, return the substring before it; otherwise, return the original string
                var propName = index >= 0 ? property.PropertyPath.Substring(0, index) : property.FieldInfo.Name;

                var outerProp = property.InspectorObject.FindProperty(propName);
                for (int i = 0; i < outerProp.ArraySize; i++)
                {
                    if (property.PropertyPath == outerProp.ArrayData[i].PropertyPath)
                    {
                        name = $"Element {i}";
                        fixedWidth = 120f;
                        break;
                    }
                }

            }

            ConvertToTupleList(keys, values, convert);
            var foldout = new StyledFoldoutProperty(name);
            var tooltip = "";
            if (property.TryGetAttribute<RichTextTooltipAttribute>(out var rtAtr))
            {
                tooltip = rtAtr.Tooltip;
            }
            if (atr.Searchable)
            {
                var indexOfCurrent = values.IndexOf(property.GetValue<KeyDropdownValue>());
                string current = (indexOfCurrent < 0 || indexOfCurrent > keys.Count - 1) ? "None" : keys[indexOfCurrent];
                var dropdown = new SearchableDropdown<string>("", current)
                {
                    name = property.PropertyName,
                    userData = (property, values),
                    tooltip = tooltip,
                    style =
                    {
                        flexGrow = 1
                    }
                };
                if (fixedWidth.HasValue)
                {
                    dropdown.style.minWidth = fixedWidth.Value;
                }
                else
                {
                    dropdown.AddToClassList("unity-base-field__aligned");
                }
                dropdown.SetChoices(keys);
                dropdown.ValueChanged += OnSearchableDropdownChanged;
                foldout.SetHeaderProperty(dropdown);
            }
            else
            {
                Debug.Log(property.GetValue<KeyDropdownValue>());
                var indexOfCurrent = values.IndexOf(property.GetValue<KeyDropdownValue>());
                int defaultInex = (indexOfCurrent < 0 || indexOfCurrent > keys.Count - 1) ? 0 : indexOfCurrent;
                var dropdown = new DropdownField("", keys, defaultInex)
                {
                    name = property.PropertyName,
                    tooltip = tooltip,
                    userData = (property, values),
                    style =
                    {
                        flexGrow = 1
                    }
                };
                if (fixedWidth.HasValue)
                {
                    dropdown.style.minWidth = fixedWidth.Value;
                }
                else
                {
                    dropdown.AddToClassList("unity-base-field__aligned");
                }
                dropdown.RegisterValueChangedCallback(OnDropdownChanged);
                foldout.SetHeaderProperty(dropdown);
            }

            var guidBox = new StyledHorizontalGroup();
            var guidField = new TreePropertyField(property.FindPropertyRelative("Guid"))
            {
                style = { flexGrow = 1 }
            };
            guidBox.Add(guidField);
            guidField.SetEnabled(false);
            guidBox.Add(new Button(() => OnSelectClicked(property))
            {
                text = "Select"
            });

            var keyBox = new StyledHorizontalGroup();
            var keyField = new TreePropertyField(property.FindPropertyRelative("Key"))
            {
                style = { flexGrow = 1 }
            };
            keyField.SetEnabled(false);
            keyBox.Add(keyField);
            keyBox.Add(new Button(() => OnRemapClicked(property))
            {
                text = "Re-Map",
            });

            foldout.Add(guidBox);
            foldout.Add(keyBox);
            return foldout;
        }

        private static void OnSelectClicked(SerializedProperty property)
        {
            if (property is { boxedValue: KeyDropdownValue key })
            {
                key.Select();
            }
        }

        private static void OnRemapClicked(SerializedProperty property)
        {
            if (property is { boxedValue: KeyDropdownValue key })
            {
                key.Remap();
            }
        }

        private static void OnSelectClicked(InspectorTreeProperty property)
        {
            property.GetValue<KeyDropdownValue>().Select();
        }

        private static void OnRemapClicked(InspectorTreeProperty property)
        {
            property.GetValue<KeyDropdownValue>().Remap();
        }

        private static void OnSearchableDropdownChanged(VisualElement visualElement, string oldValue, string newValue)
        {
            if (visualElement is SearchableDropdown<string> dropdown)
            {
                var tuple = ((SerializedProperty, List<KeyDropdownValue>))dropdown.userData;
                var newVal = tuple.Item2[dropdown.Index];
                Debug.Log("Applied " + newVal);
                tuple.Item1.boxedValue = newVal;
                tuple.Item1.serializedObject.ApplyModifiedProperties();
            }
        }

        private static void OnDropdownChanged(ChangeEvent<string> evt)
        {
            if (evt.target is DropdownField dropdown)
            {
                var tuple = ((SerializedProperty, List<KeyDropdownValue>))dropdown.userData;
                var newVal = tuple.Item2[dropdown.index];
                Debug.Log("Applied " + newVal);
                tuple.Item1.boxedValue = newVal;
                tuple.Item1.serializedObject.ApplyModifiedProperties();
            }
        }

        private static void ConvertToTupleList(List<string> keys, List<KeyDropdownValue> values, IList convert)
        {
            foreach (var obj in convert)
            {
                var item1 = (string)obj.GetType().GetField("Item1", BindingFlags.Instance | BindingFlags.Public)
                    ?.GetValue(obj);
                var item2 = obj.GetType().GetField("Item2", BindingFlags.Instance | BindingFlags.Public)
                    ?.GetValue(obj);
                if (item1 == null || item2 == null)
                {
                    continue;
                }

                keys.Add(item1);
                values.Add((KeyDropdownValue)item2);
            }
        }

        private static IList GetKeysField(Type type, string valuesName)
        {
            var fieldInfo = type.GetField(valuesName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (fieldInfo == null)
            {
                var allTypes = ReflectionUtility.GetSelfAndBaseTypes(type);
                foreach (var t in allTypes)
                {
                    fieldInfo = t.GetField(valuesName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (fieldInfo != null)
                    {
                        break;
                    }
                }
            }

            if (fieldInfo == null) return null;

            var keys = fieldInfo.GetValue(null);
            if (keys is IList keyList)
            {
                return keyList;
            }

            return null;
        }

        
    }
}
