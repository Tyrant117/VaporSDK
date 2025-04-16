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
        public IBlueprintInspectable Inspectable { get; private set; }
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

        public void SetInspectorTarget(object drawTarget)
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
            
            var deprecatedField = new Toggle("Deprecated")
            {
                toggleOnLabelClick = false,
            };
            deprecatedField.SetValueWithoutNotify(Window.ClassGraphModel.IsObsolete);
            deprecatedField.RegisterValueChangedCallback(evt => Window.ClassGraphModel.IsObsolete = evt.newValue);
            deprecatedField.styleSheets.Add(labelStyle);
            deprecatedField.AddToClassList("toggle-fix");
            deprecatedField.Q<Label>().AddToClassList("flex-label");
            _settingsElement.Add(deprecatedField);
            
            var namespaceField = new TextField("Namespace");
            namespaceField.SetValueWithoutNotify(Window.ClassGraphModel.Namespace ?? string.Empty);
            namespaceField.RegisterValueChangedCallback(evt => Window.ClassGraphModel.Namespace = evt.newValue);
            namespaceField.styleSheets.Add(labelStyle);
            namespaceField.Q<Label>().AddToClassList("flex-label");
            _settingsElement.Add(namespaceField);

            var usingList = new ListView(Window.ClassGraphModel.Usings, makeItem: () => new VisualElement(), bindItem: (element, i) =>
            {
                element.Clear();
                int idx = i;
                var usingName = Window.ClassGraphModel.Usings[i];
                var usingField = new TextField("Namespace");
                usingField.SetValueWithoutNotify(usingName ?? string.Empty);
                usingField.RegisterValueChangedCallback(evt => 
                {
                    Window.ClassGraphModel.Usings[idx] = evt.newValue;
                    Window.UpdateSearchModels();
                });
                usingField.styleSheets.Add(labelStyle);
                usingField.Q<Label>().AddToClassList("flex-label");
                element.Add(usingField);
            })
            {
                showFoldoutHeader = true,
                headerTitle = "Additional Namespaces",
                showAddRemoveFooter = true,
                showBorder = true,
                showBoundCollectionSize = false,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                onRemove = lv =>
                {
                    Window.ClassGraphModel.Usings.RemoveAt(Window.ClassGraphModel.Usings.Count - 1);
                    Window.UpdateSearchModels();
                    lv.Rebuild();
                },
                style =
                {
                    marginLeft = 3f,
                    marginRight = 3f,
                }
            };
            _settingsElement.Add(usingList);
            
            
            var interfacesList = new ListView(Window.ClassGraphModel.ImplementedInterfaceTypes, makeItem: () => new VisualElement(), bindItem: (element, i) =>
            {
                element.Clear();
                int idx = i;
                var interfaceType = Window.ClassGraphModel.ImplementedInterfaceTypes[idx];
                var typeSelector = new TypeSelectorField(string.Empty, interfaceType, t => (t.IsPublic || t.IsNestedPublic) && t.IsInterface);
                typeSelector.TypeChanged += (_, old, cur) => Window.ClassGraphModel.OnInterfaceUpdated(old, cur);
                element.Add(typeSelector);
            })
            {
                showFoldoutHeader = true,
                headerTitle = "Implemented Interfaces",
                showAddRemoveFooter = true,
                showBorder = true,
                showBoundCollectionSize = false,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                onAdd = OnAddInterface,
                onRemove = OnRemoveInterface,
                style = 
                {
                    marginLeft = 3f,
                    marginRight = 3f,
                }
            };
            _settingsElement.Add(interfacesList);
        }

        private void OnAddInterface(BaseListView obj)
        {
            Window.ClassGraphModel.AddInterface(null);
        }
        
        private void OnRemoveInterface(BaseListView obj)
        {
            Window.ClassGraphModel.RemoveInterfaceAt(Window.ClassGraphModel.ImplementedInterfaceTypes.Count - 1);
            obj.Rebuild();
        }
    }
}
