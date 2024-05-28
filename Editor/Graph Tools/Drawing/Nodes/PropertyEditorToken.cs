using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.GraphTools;

namespace VaporEditor.GraphTools
{
    public class PropertyEditorToken<GraphArg> : NParamEditorToken<GraphArg> where GraphArg : ScriptableObject
    {

        protected PropertyNodeSo PropertyNode;

        public PropertyEditorToken(GraphEditorView<GraphArg> view, PropertyNodeSo node, EditorLabelVisualData visualData) : base(view, node, visualData)
        {
            PropertyNode = node;

            this.Q<Label>("title-label").RemoveFromHierarchy();
            topContainer.Insert(topContainer.childCount - 1, CreatePropertyDrawer());

            RefreshExpandedState();
        }

        protected virtual VisualElement CreatePropertyDrawer()
        {
            var type = PropertyNode.GetValueType();
            if (type.IsPrimitive)
            {
                VisualElement ve = type switch
                {
                    var t when t == typeof(bool) => ConfigureToggle(),
                    var t when t == typeof(byte) => ConfigureIntegerField(),
                    var t when t == typeof(sbyte) => ConfigureIntegerField(),
                    var t when t == typeof(short) => ConfigureIntegerField(),
                    var t when t == typeof(ushort) => ConfigureIntegerField(),
                    var t when t == typeof(int) => ConfigureIntegerField(),
                    var t when t == typeof(uint) => ConfigureUnsignedIntegerField(),
                    var t when t == typeof(float) => ConfigureFloatField(),
                    var t when t == typeof(long) => ConfigureLongField(),
                    var t when t == typeof(ulong) => ConfigureUnsignedLongField(),
                    var t when t == typeof(double) => ConfigureDoubleField(),
                    var t when t == typeof(char) => ConfigureTextField(1),
                    _ => new Label($"Undefined Type: {type}")
                };

                return ve;
            }
            else if (type.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                var ve = new ObjectField("X")
                {
                    style =
                    {
                        flexGrow = 1,
                        flexShrink = 1,
                        marginBottom = 6,
                        marginLeft = 6,
                        marginRight = 6,
                        marginTop = 6,
                        maxWidth = 130,
                    },
                    objectType = type,
                };
                return ve;
            }
            else if (type.IsEnum)
            {
                if (type.IsDefined(typeof(FlagsAttribute), false))
                {
                    var en = (Enum)Activator.CreateInstance(type);
                    EnumFlagsField enumField = new("X", en)
                    {
                        style =
                        {
                            flexGrow = 1,
                            flexShrink = 1,
                            marginBottom = 6,
                            marginLeft = 6,
                            marginRight = 6,
                            marginTop = 6,
                            maxWidth = 130,
                        },
                    };

                    return enumField;
                }
                else
                {
                    var en = (Enum)Activator.CreateInstance(type);
                    EnumField enumField = new("X", en)
                    {
                        style =
                        {
                            flexGrow = 1,
                            flexShrink = 1,
                            marginBottom = 6,
                            marginLeft = 6,
                            marginRight = 6,
                            marginTop = 6,
                            maxWidth = 130,
                        },
                    };

                    return enumField;
                }
            }
            else
            {

                VisualElement ve = type switch
                {
                    var t when t == typeof(string) => ConfigureTextField(),
                    var t when t == typeof(Vector2) => ConfigureVector2Field(),
                    var t when t == typeof(Vector2Int) => ConfigureVector2IntField(),
                    var t when t == typeof(Vector3) => ConfigureVector3Field(),
                    var t when t == typeof(Vector3Int) => ConfigureVector3IntField(),
                    var t when t == typeof(Vector4) => ConfigureVector4Field(),
                    var t when t == typeof(Rect) => ConfigureRectField(),
                    var t when t == typeof(RectInt) => ConfigureRectIntField(),
                    var t when t == typeof(Color) => ConfigureColorField(),
                    var t when t == typeof(Gradient) => ConfigureGradientField(),
                    var t when t == typeof(AnimationCurve) => ConfigureCurveField(),
                    var t when t == typeof(LayerMask) => ConfigureLayerMaskField(),
                    _ => new Label($"Undefined Type: {type}")
                };

                return ve;
            }
        }

        private VisualElement ConfigureToggle()
        {
            var field = new Toggle("X")
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    marginBottom = 6,
                    marginLeft = 6,
                    marginRight = 6,
                    marginTop = 6,
                    maxWidth = 60,
                },
                pickingMode = PickingMode.Ignore
            };
            field.RemoveFromClassList("unity-base-field__aligned");
            var label = field.Q<Label>();
            label.pickingMode = PickingMode.Ignore;
            label.style.maxWidth = 30;
            label.style.minWidth = 30;
            if (PropertyNode.TryGetValue<bool>(out var value))
            {
                field.SetValueWithoutNotify(value);
            }

            field.RegisterValueChangedCallback(OnValueChanged);
            return field;

            void OnValueChanged(ChangeEvent<bool> evt)
            {
                if (PropertyNode is ValueNodeSo<bool> node)
                {
                    node.Value = evt.newValue;
                }
            }
        }
        private VisualElement ConfigureIntegerField()
        {
            var field = new IntegerField("X")
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    marginBottom = 6,
                    marginLeft = 6,
                    marginRight = 6,
                    marginTop = 6,
                    maxWidth = 130,
                }
            };
            field.RemoveFromClassList("unity-base-field__aligned");
            var label = field.Q<Label>();
            label.pickingMode = PickingMode.Ignore;
            label.style.maxWidth = 30;
            label.style.minWidth = 30;
            field.Q<VisualElement>("unity-text-input").style.maxWidth = 100;
            if (PropertyNode.TryGetValue<int>(out var value))
            {
                field.SetValueWithoutNotify(value);
            }

            field.RegisterValueChangedCallback(OnValueChanged);
            return field;

            void OnValueChanged(ChangeEvent<int> evt)
            {
                if (PropertyNode is ValueNodeSo<int> node)
                    node.Value = evt.newValue;
            }
        }
        private VisualElement ConfigureUnsignedIntegerField()
        {
            throw new NotImplementedException();
        }
        private VisualElement ConfigureFloatField()
        {
            var field = new FloatField("X")
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    marginBottom = 6,
                    marginLeft = 6,
                    marginRight = 6,
                    marginTop = 6,
                    maxWidth = 130,
                }
            };
            field.RemoveFromClassList("unity-base-field__aligned");
            var label = field.Q<Label>();
            label.pickingMode = PickingMode.Ignore;
            label.style.maxWidth = 30;
            label.style.minWidth = 30;
            field.Q<VisualElement>("unity-text-input").style.maxWidth = 100;
            if (PropertyNode.TryGetValue<float>(out var value))
            {
                field.SetValueWithoutNotify(value);
            }

            field.RegisterValueChangedCallback(OnValueChanged);
            return field;

            void OnValueChanged(ChangeEvent<float> evt)
            {
                if (PropertyNode is ValueNodeSo<float> node)
                    node.Value = evt.newValue;
            }
        }
        private VisualElement ConfigureDoubleField()
        {
            throw new NotImplementedException();
        }
        private VisualElement ConfigureLongField()
        {
            throw new NotImplementedException();
        }
        private VisualElement ConfigureUnsignedLongField()
        {
            throw new NotImplementedException();
        }
        private VisualElement ConfigureTextField(int maxCharacters = -1)
        {
            throw new NotImplementedException();
        }
        private VisualElement ConfigureVector2Field()
        {
            throw new NotImplementedException();
        }
        private VisualElement ConfigureVector2IntField()
        {
            throw new NotImplementedException();
        }
        private VisualElement ConfigureVector3Field()
        {
            throw new NotImplementedException();
        }
        private VisualElement ConfigureVector3IntField()
        {
            throw new NotImplementedException();
        }
        private VisualElement ConfigureVector4Field()
        {
            throw new NotImplementedException();
        }
        private VisualElement ConfigureRectField()
        {
            throw new NotImplementedException();
        }
        private VisualElement ConfigureRectIntField()
        {
            throw new NotImplementedException();
        }
        private VisualElement ConfigureColorField()
        {
            throw new NotImplementedException();
        }
        private VisualElement ConfigureGradientField()
        {
            throw new NotImplementedException();
        }
        private VisualElement ConfigureCurveField()
        {
            throw new NotImplementedException();
        }
        private Label ConfigureLayerMaskField()
        {
            throw new NotImplementedException();
        }
    }
}
