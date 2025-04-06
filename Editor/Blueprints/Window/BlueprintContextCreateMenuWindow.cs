using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor;
using Vapor.Blueprints;
using Vapor.Inspector;

namespace VaporEditor.Blueprints
{
    public class BlueprintContextCreateMenuWindow : EditorWindow
    {
        [MenuItem("Assets/Create/Vapor/Blueprints/Create New Blueprint", false, 100)]
        private static void OpenWindowFromCreateMenu()
        {
            // Create and focus the editor window
            BlueprintContextCreateMenuWindow window = GetWindow<BlueprintContextCreateMenuWindow>("Create Blueprint");
            window.minSize = new Vector2(600, 400);
            window.ShowModal();
            window.Focus();
        }

        private ListView _listView;
        private List<Type> _typeCollection = new List<Type>(1000);
        private List<Type> _filteredTypeCollection = new List<Type>(1000);
        
        private void CreateGUI()
        {
            _typeCollection.AddRangeUnique(TypeCache.GetTypesWithAttribute<BlueprintableAttribute>());
            _filteredTypeCollection = new List<Type>(_typeCollection);
            
            var toolbar = new Toolbar();
            var search = new ToolbarSearchField();
            search.RegisterValueChangedCallback(OnSearchChanged);
            toolbar.Add(search);
            rootVisualElement.Add(toolbar);
            _listView = new ListView(_typeCollection, makeItem: () => new VisualElement(), bindItem: BindItem)
            {
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                style = { flexGrow = 1f }
            };
            
            rootVisualElement.Add(_listView);
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            string searchText = evt.newValue;
            _filteredTypeCollection.Clear();
            if (searchText.EmptyOrNull())
            {
                _filteredTypeCollection = new List<Type>(_typeCollection);
            }
            else
            {
                _filteredTypeCollection.AddRange(_typeCollection.Where(k => FuzzySearch.FuzzyMatch(searchText, k.FullName)));
            }

            _listView.itemsSource = new List<Type>(_filteredTypeCollection);
            _listView.Rebuild();
        }

        private void BindItem(VisualElement ve, int idx)
        {
            ve.Clear();
            ve.style.flexDirection = FlexDirection.Row;
            var label = new Label(_filteredTypeCollection[idx].FullName)
            {
                style = { flexGrow = 1f }
            };
            var btn = new Button(() => CreateBlueprintFromType(_filteredTypeCollection[idx]))
            {
                text = "+",
            };
            ve.Add(label);
            ve.Add(btn);
        }

        private void CreateBlueprintFromType(Type type)
        {
            Close();
            
            // If type derives from monobehaviour create an event graph
            // else create a function graph
            if (type.IsSubclassOf(typeof(MonoBehaviour)))
            {
                var path = ScriptableObjectUtility.Create<BlueprintGraphSo>(processAsset: (x) =>
                {
                    var so = (BlueprintGraphSo)x;
                    so.GraphType = BlueprintGraphSo.BlueprintGraphType.BehaviourGraph;
                    so.ParentType = type.AssemblyQualifiedName;
                });
            }
            else
            {
                var path = ScriptableObjectUtility.Create<BlueprintGraphSo>(processAsset: (x) =>
                {
                    var so = (BlueprintGraphSo)x;
                    so.GraphType = BlueprintGraphSo.BlueprintGraphType.ClassGraph;
                    so.ParentType = type.AssemblyQualifiedName;
                });
            }
        }
    }
}
