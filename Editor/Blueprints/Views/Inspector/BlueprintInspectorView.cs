using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Blueprints;
using Vapor.Inspector;
using VaporEditor.Inspector;

namespace VaporEditor.Blueprints
{
    public class BlueprintInspectorView : VisualElement
    {
        public BlueprintEditorWindow Window { get; private set; }
        public object CurrentDrawTarget { get; private set; }
        
        private readonly VisualElement _drawContainer;
        private readonly ScrollView _scrollView;
        private readonly TabView _tabView;
        private VisualElement _inspectorElement;
        private VisualElement _settingsElement;

        public BlueprintInspectorView(BlueprintEditorWindow window)
        {
            Window = window;
            style.flexGrow = 1f;
            style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);
            
            _tabView = new TabView()
            {
                style =
                {
                    marginLeft = 8f,
                    marginTop = 4f,
                    marginBottom = 4f,
                }
            };
            Tab inspectorTab = new Tab("Inspector")
            {
                style =
                {
                    fontSize = 14f,
                }
            };
            Tab classSettingsTab = new Tab("Class Settings")
            {
                style =
                {
                    fontSize = 14f,
                }
            };
            _tabView.Add(inspectorTab);
            _tabView.Add(classSettingsTab);
            _tabView.activeTabChanged += OnTabChanged;
            
            Add(_tabView);
            Add(new VisualElement()
            {
                style =
                {
                    height = 1f,
                    maxHeight = 1f,
                    backgroundColor = Color.grey,
                    flexGrow = 1f,
                }
            });

            _scrollView = new ScrollView()
            {
                style =
                {
                    flexGrow = 1f,
                    backgroundColor = new Color(0.22f, 0.22f, 0.22f),
                }
            };
            Add(_scrollView);
            _drawContainer = new VisualElement()
            {
                style =
                {
                    flexGrow = 1f,
                }
            };
            _scrollView.Add(_drawContainer);
            
            CreateClassSettings();
        }

        private void OnTabChanged(Tab old, Tab current)
        {
            if (current == old)
            {
                return;
            }
            _drawContainer.Clear();
            switch (_tabView.selectedTabIndex)
            {
                case 0: // Draw Inspector
                    _drawContainer.Add(_inspectorElement);
                    break;
                case 1: // Draw Class Settings
                    _drawContainer.Add(_settingsElement);
                    break;
            }
        }

        public void SetDrawTarget(object drawTarget)
        {
            CurrentDrawTarget = drawTarget;
            if (CurrentDrawTarget != null)
            {
                _drawContainer.Clear();
                if (drawTarget is VisualElement v)
                {
                    _inspectorElement = v;
                }
                else
                {
                    _inspectorElement = SerializedDrawerUtility.DrawAny(CurrentDrawTarget);
                }

                if (_tabView.selectedTabIndex != 0)
                {
                    _tabView.selectedTabIndex = 0;
                }
                else
                {
                    _drawContainer.Add(_inspectorElement);
                }
            }
            else
            {
                if(_tabView.selectedTabIndex == 0)
                {
                    _drawContainer.Clear();
                }
            }
        }

        private void CreateClassSettings()
        {
            _settingsElement = new VisualElement();
            var labelStyle = Resources.Load<StyleSheet>("LabelStyle");

            var parentField = new ObjectField("Parent")
            {
                allowSceneObjects = false,
                objectType = typeof(BlueprintGraphSo),
            };
            parentField.styleSheets.Add(labelStyle);
            parentField.Q<Label>().AddToClassList("flex-label");
            _settingsElement.Add(parentField);
            
            // List<VariableAccessModifier> accessChoices = new() { VariableAccessModifier.Public, VariableAccessModifier.Protected, VariableAccessModifier.Private };
            // var popup = new PopupField<VariableAccessModifier>("Access", accessChoices, Window.DesignGraph.AccessModifier,
            //     v => v.ToString(), v => v.ToString());
            // popup.RegisterValueChangedCallback(OnAccessModifierChanged);
            // popup.styleSheets.Add(labelStyle);
            // popup.Q<Label>().AddToClassList("flex-label");
            // _settingsElement.Add(popup);
            
            var deprecatedField = new Toggle("Deprecated")
            {
                toggleOnLabelClick = false,
            };
            deprecatedField.SetValueWithoutNotify(Window.DesignGraph.IsDeprecated);
            deprecatedField.RegisterValueChangedCallback(evt => Window.DesignGraph.IsDeprecated = evt.newValue);
            deprecatedField.styleSheets.Add(labelStyle);
            deprecatedField.AddToClassList("toggle-fix");
            deprecatedField.Q<Label>().AddToClassList("flex-label");
            _settingsElement.Add(deprecatedField);
            
            var abstractField = new Toggle("Abstract")
            {
                toggleOnLabelClick = false
            };
            abstractField.SetValueWithoutNotify(Window.DesignGraph.IsAbstract);
            abstractField.RegisterValueChangedCallback(evt => Window.DesignGraph.IsAbstract = evt.newValue);
            abstractField.styleSheets.Add(labelStyle);
            abstractField.AddToClassList("toggle-fix");
            abstractField.Q<Label>().AddToClassList("flex-label");
            _settingsElement.Add(abstractField);
            
            var namespaceField = new TextField("Namespace");
            namespaceField.SetValueWithoutNotify(Window.DesignGraph.Namespace ?? string.Empty);
            namespaceField.RegisterValueChangedCallback(evt => Window.DesignGraph.Namespace = evt.newValue);
            namespaceField.styleSheets.Add(labelStyle);
            namespaceField.Q<Label>().AddToClassList("flex-label");
            _settingsElement.Add(namespaceField);
            
            
            var interfacesList = new ListView(Window.DesignGraph.ImplementedInterfaces, makeItem: () => new VisualElement(), bindItem: (element, i) =>
            {
                element.Clear();
                int idx = i;
                var aqn = Window.DesignGraph.ImplementedInterfaces[idx];
                var currentType = aqn.EmptyOrNull() ? null : Type.GetType(aqn);
                var typeSelector = new TypeSelectorField(string.Empty, currentType, t => (t.IsPublic || t.IsNestedPublic) && t.IsInterface);
                typeSelector.AssemblyQualifiedTypeChanged += (_, cur, _) => Window.DesignGraph.ImplementedInterfaces[idx] = cur;
                element.Add(typeSelector);
            })
            {
                showFoldoutHeader = true,
                headerTitle = "Implemented Interfaces",
                showAddRemoveFooter = true,
                showBorder = true,
                showBoundCollectionSize = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                style = 
                {
                    marginLeft = 3f,
                    marginRight = 3f,
                }
            };
            _settingsElement.Add(interfacesList);
        }

        // private void OnAccessModifierChanged(ChangeEvent<VariableAccessModifier> evt)
        // {
        //     Window.DesignGraph.AccessModifier = evt.newValue;
        // }
    }
}
