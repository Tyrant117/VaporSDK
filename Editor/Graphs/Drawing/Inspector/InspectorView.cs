using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Inspector;

namespace VaporEditor.Graphs
{
    public class InspectorView : BlueprintWindow
    {
        private const float s_K_InspectorUpdateInterval = 0.25f;
        private const int s_K_InspectorElementLimit = 20;

        public override string WindowTitle => "Blueprint Inspector";
        public override string ElementName => "InspectorView";
        public override string StyleName => "BlueprintInspectorView";
        public override string UxmlName => "BlueprintInspector";
        public override string LayoutKey => "VaporEditor.Graphs.InspectorView";

        public bool DoesInspectorNeedUpdate { get; set; }

        private readonly InspectorViewController _controller;

        private TabView _graphInspectorView;
        private VisualElement _graphSettingsContainer;
        private VisualElement _nodeSettingsContainer;

        private bool _graphSettingsTabFocused = false;
        private int _currentlyInspectedElementsCount = 0;
        private HashSet<IInspectableNode> _cachedInspectables = new();
        

        public InspectorView(InspectorViewModel viewModel, InspectorViewController inspectorViewController) : base(viewModel)
        {
            _controller = inspectorViewController;
            _graphInspectorView = MainContainer.Q<TabView>("GraphInspectorView");
            _graphSettingsContainer = _graphInspectorView.Q<VisualElement>("GraphSettingsContainer");
            _nodeSettingsContainer = _graphInspectorView.Q<VisualElement>("NodeSettingsContainer");
            ContentContainer.Add(_graphInspectorView);
            ScrollView = this.Q<ScrollView>();
            _graphInspectorView.Q<Tab>("GraphSettingsContainer").selected += GraphSettingsTabClicked;
            _graphInspectorView.Q<Tab>("NodeSettingsContainer").selected += NodeSettingsTabClicked;

            IsWindowScrollable = true;
            IsWindowResizable = true;

            // By default at startup, show graph settings
            _graphInspectorView.activeTab = _graphInspectorView.Q<Tab>("GraphSettingsContainer");
        }

        public void InitializeGraphSettings()
        {
            ShowGraphSettings(_graphSettingsContainer);
        }

        public override void Dispose()
        {
            _graphInspectorView.Q<Tab>("GraphSettingsContainer").selected -= GraphSettingsTabClicked;
            _graphInspectorView.Q<Tab>("NodeSettingsContainer").selected -= NodeSettingsTabClicked;
            _graphInspectorView = null;
            _graphSettingsContainer = null;
            _nodeSettingsContainer = null;
        }

        #region - Updating -
        protected virtual void ShowGraphSettings(VisualElement contentContainer)
        {
            contentContainer.Clear();

            var graphEditorView = ParentView.GetFirstAncestorOfType<GraphEditorView>();
            if (graphEditorView == null)
            {
                Debug.Log("InspectorView not attached to GraphEditorView");
                return;
            }

            DrawInspectable(contentContainer, (IDrawableElement)ParentView);
            contentContainer.MarkDirtyRepaint();
        }

        public void TriggerInspectorUpdate(IEnumerable<ISelectable> selectionList)
        {
            // An optimization that prevents inspector updates from getting triggered every time a selection event is issued in the event of large selections
            if (selectionList?.Count() > s_K_InspectorElementLimit)
            {
                return;
            }

            DoesInspectorNeedUpdate = true;
        }

        public void HandleGraphChanges()
        {
            float timePassed = (float)(EditorApplication.timeSinceStartup % s_K_InspectorUpdateInterval);

            int currentInspectablesCount = 0;
            foreach (var selectable in Selection)
            {
                if (selectable is IInspectableNode)
                {
                    currentInspectablesCount++;
                }
            }

            // Don't update for selections beyond a certain amount as they are no longer visible in the inspector past a certain point and only cost performance as the user performs operations
            if (timePassed < 0.01f && Selection.Count < s_K_InspectorElementLimit && currentInspectablesCount != _currentlyInspectedElementsCount)
            {
                _graphSettingsTabFocused = false;
                Update();
            }
        }

        public void Update()
        {
            //TODO: Tear down all existing active property drawers, everything is getting rebuilt

            ShowGraphSettings(_graphSettingsContainer);
            _nodeSettingsContainer.Clear();

            try
            {
                bool anySelectables = false;
                int currentInspectablesCount = 0;
                var currentInspectables = new HashSet<IInspectableNode>();
                foreach (var selectable in Selection)
                {
                    if (selectable is IInspectableNode inspectable)
                    {
                        DrawInspectable(_nodeSettingsContainer, inspectable);
                        currentInspectablesCount++;
                        anySelectables = true;
                        currentInspectables.Add(inspectable);
                        break;
                    }
                }

                // If we have changed our inspector selection while the graph settings tab was focused, we want to switch back to the node settings tab, so invalidate the flag
                foreach (var currentInspectable in currentInspectables)
                {
                    if (_cachedInspectables.Contains(currentInspectable) == false)
                    {
                        _graphSettingsTabFocused = false;
                    }
                }

                _cachedInspectables = currentInspectables;
                _currentlyInspectedElementsCount = currentInspectablesCount;

                if (anySelectables && !_graphSettingsTabFocused)
                {
                    // Anything selectable in the graph (GraphSettings not included) is only ever interacted with through the
                    // Node Settings tab so we can make the assumption they want to see that tab
                    _graphInspectorView.activeTab = _graphInspectorView.Q<Tab>("NodeSettingsContainer");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            DoesInspectorNeedUpdate = false;
            _nodeSettingsContainer.MarkDirtyRepaint();
        }

        private void DrawInspectable(VisualElement nodeSettingsContainer, IDrawableElement inspectable)
        {
            nodeSettingsContainer.Add(inspectable.DrawElement());
        }


        #endregion

        #region - Callbacks -
        void GraphSettingsTabClicked(Tab tab)
        {
            _graphSettingsTabFocused = true;
            ScrollView.mode = ScrollViewMode.Vertical;
        }

        void NodeSettingsTabClicked(Tab tab)
        {
            _graphSettingsTabFocused = false;
            ScrollView.mode = ScrollViewMode.VerticalAndHorizontal;
        }
        #endregion
    }
}
