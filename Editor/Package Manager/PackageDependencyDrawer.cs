using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Vapor.PackageManager;

namespace VaporEditor.PackageManager
{
    [CustomPropertyDrawer(typeof(PackageDependency))]
    public class PackageDependencyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var ve = new VisualElement();
            var horizontal = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1f,
                }
            };

            var url = new PropertyField(property.FindPropertyRelative("GitUrl"))
            {
                style =
                {
                    flexGrow = 1f,
                    marginRight = 4,
                }
            };
            url.RegisterCallback<GeometryChangedEvent>(OnBuiltProperty);

            var version = new PropertyField(property.FindPropertyRelative("Version"))
            {
                style =
                {
                    flexGrow = 1f,
                    minWidth = 140,
                    width = 140,
                    maxWidth = 140,
                }
            };
            version.RegisterCallback<GeometryChangedEvent>(OnBuiltProperty);

            horizontal.Add(url);
            horizontal.Add(version);

            ve.Add(horizontal);
            ve.Add(new PropertyField(property.FindPropertyRelative("Defines")));

            return ve;
        }

        private void OnBuiltProperty(GeometryChangedEvent evt)
        {
            var field = (PropertyField)evt.target;
            if (field is not { childCount: > 0 }) return;

            field.UnregisterCallback<GeometryChangedEvent>(OnBuiltProperty);

            field.hierarchy[0].RemoveFromClassList("unity-base-field__aligned");
            var label = field.Q<Label>();

            label.style.minWidth = 50;
            label.style.width = new StyleLength(StyleKeyword.Auto);
        }
    }
}
