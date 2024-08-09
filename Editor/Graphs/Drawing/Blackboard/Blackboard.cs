//using System;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEditor;
//using UnityEditor.Experimental.GraphView;
//using UnityEngine;
//using UnityEngine.Assertions;
//using UnityEngine.UIElements;
//using Vapor.UI;
//using VaporEditor.Graphs;

//namespace VaporEditor.Graphs
//{
//    public class Blackboard : BlueprintWindow, IUIControlledElement<BlackboardViewController>
//    {
//        VisualElement m_ScrollBoundaryTop;
//        VisualElement m_ScrollBoundaryBottom;
//        VisualElement m_BottomResizer;
//        TextField m_PathLabelTextField;
//        public VisualElement m_VariantExceededHelpBox;

//        // --- Begin ISGControlledElement implementation
//        public void OnControllerChanged(ref UIControllerChangedEvent e)
//        {
//        }

//        public void OnControllerEvent(UIControllerEvent e)
//        {
//        }

//        BlackboardViewController m_Controller;
//        public BlackboardViewController Controller
//        {
//            get => m_Controller;
//            set
//            {
//                if (m_Controller != value)
//                {
//                    if (m_Controller != null)
//                    {
//                        m_Controller.UnregisterHandler(this);
//                    }
//                    m_Controller = value;

//                    if (m_Controller != null)
//                    {
//                        m_Controller.RegisterHandler(this);
//                    }
//                }
//            }
//        }

//        UIController IUIControlledElement.Controller => m_Controller;

//        // --- ISGControlledElement implementation

        

//        BlackboardViewModel m_ViewModel;

//        BlackboardViewModel ViewModel
//        {
//            get => m_ViewModel;
//            set => m_ViewModel = value;
//        }

//        // List of user-made blackboard category views
//        IList<BlackboardCategory> m_BlackboardCategories = new List<BlackboardCategory>();

//        bool m_ScrollToTop = false;
//        bool m_ScrollToBottom = false;
//        bool m_EditPathCancelled = false;
//        bool m_IsUserDraggingItems = false;
//        int m_InsertIndex = -1;

//        const int k_DraggedPropertyScrollSpeed = 6;

//        public override string windowTitle => "Blackboard";
//        public override string elementName => "SGBlackboard";
//        public override string styleName => "SGBlackboard";
//        public override string UxmlName => "Blackboard/SGBlackboard";
//        public override string layoutKey => "UnityEditor.ShaderGraph.Blackboard";

//        Action addItemRequested { get; set; }

//        internal Action hideDragIndicatorAction { get; set; }
//        int scrollViewIndex { get; set; }

//        GenericMenu m_AddBlackboardItemMenu;
//        internal GenericMenu addBlackboardItemMenu => m_AddBlackboardItemMenu;

//        VisualElement m_DragIndicator;

//        public Blackboard(BlackboardViewModel viewModel, BlackboardViewController controller) : base(viewModel)
//        {
//            ViewModel = viewModel;
//            this.Controller = controller;

//            InitializeAddBlackboardItemMenu();

//            // By default dock blackboard to left of graph window
//            WindowDockingLayout.DockingLeft = true;

//            if (m_MainContainer.Q(name: "addButton") is Button addButton)
//                addButton.clickable.clicked += () =>
//                {
//                    InitializeAddBlackboardItemMenu();
//                    addItemRequested?.Invoke();
//                    ShowAddPropertyMenu();
//                };

//            ParentView.RegisterCallback<FocusOutEvent>(evt => OnDragExitedEvent(new DragExitedEvent()));

//            m_TitleLabel.text = ViewModel.Title;

//            m_SubTitleLabel.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
//            m_SubTitleLabel.text = ViewModel.Subtitle;

//            m_PathLabelTextField = this.Q<TextField>("subTitleTextField");
//            m_PathLabelTextField.value = ViewModel.Subtitle;
//            m_PathLabelTextField.visible = false;
//            m_PathLabelTextField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(e => { OnEditPathTextFinished(); }, TrickleDown.TrickleDown);
//            m_PathLabelTextField.Q("unity-text-input").RegisterCallback<KeyDownEvent>(OnPathTextFieldKeyPressed, TrickleDown.TrickleDown);

//            // These callbacks make sure the scroll boundary regions and drag indicator don't show up user is not dragging/dropping properties/categories
//            RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
//            RegisterCallback<DragExitedEvent>(OnDragExitedEvent);

//            // Register drag callbacks
//            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
//            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
//            RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
//            RegisterCallback<DragExitedEvent>(OnDragExitedEvent);

//            // This callback makes sure the drag indicator is shown again if user exits and then re-enters blackboard while dragging
//            RegisterCallback<MouseEnterEvent>(OnMouseEnterEvent);

//            m_ScrollBoundaryTop = m_MainContainer.Q(name: "scrollBoundaryTop");
//            m_ScrollBoundaryTop.RegisterCallback<MouseEnterEvent>(ScrollRegionTopEnter);
//            m_ScrollBoundaryTop.RegisterCallback<DragUpdatedEvent>(OnFieldDragUpdate);
//            m_ScrollBoundaryTop.RegisterCallback<MouseLeaveEvent>(ScrollRegionTopLeave);

//            m_ScrollBoundaryBottom = m_MainContainer.Q(name: "scrollBoundaryBottom");
//            m_ScrollBoundaryBottom.RegisterCallback<MouseEnterEvent>(ScrollRegionBottomEnter);
//            m_ScrollBoundaryBottom.RegisterCallback<DragUpdatedEvent>(OnFieldDragUpdate);
//            m_ScrollBoundaryBottom.RegisterCallback<MouseLeaveEvent>(ScrollRegionBottomLeave);

//            m_BottomResizer = m_MainContainer.Q("bottom-resize");

//            HideScrollBoundaryRegions();

//            // Sets delegate association so scroll boundary regions are hidden when a blackboard property is dropped into graph
//            if (ParentView is BlueprintGraphView bpGraphView)
//                bpGraphView.BlackboardFieldDropDelegate = HideScrollBoundaryRegions;

//            isWindowScrollable = true;
//            isWindowResizable = true;
//            focusable = true;

//            m_DragIndicator = new VisualElement();
//            m_DragIndicator.name = "categoryDragIndicator";
//            m_DragIndicator.style.position = Position.Absolute;
//            hierarchy.Add(m_DragIndicator);
//            SetCategoryDragIndicatorVisible(false);
//        }

//        void SetCategoryDragIndicatorVisible(bool visible)
//        {
//            if (visible && (m_DragIndicator.parent == null))
//            {
//                hierarchy.Add(m_DragIndicator);
//                m_DragIndicator.visible = true;
//            }
//            else if ((visible == false) && (m_DragIndicator.parent != null))
//            {
//                hierarchy.Remove(m_DragIndicator);
//            }
//        }

//        public void OnDragEnterEvent(DragEnterEvent evt)
//        {
//            if (!m_IsUserDraggingItems)
//            {
//                m_IsUserDraggingItems = true;

//                if (scrollableHeight > 0)
//                {
//                    // Interferes with scrolling functionality of properties with the bottom scroll boundary
//                    m_BottomResizer.style.visibility = Visibility.Hidden;

//                    var contentElement = m_MainContainer.Q(name: "content");
//                    scrollViewIndex = contentElement.IndexOf(m_ScrollView);
//                    contentElement.Insert(scrollViewIndex, m_ScrollBoundaryTop);
//                    scrollViewIndex = contentElement.IndexOf(m_ScrollView);
//                    contentElement.Insert(scrollViewIndex + 1, m_ScrollBoundaryBottom);
//                }

//                // If there are any categories in the selection, show drag indicator, otherwise hide
//                SetCategoryDragIndicatorVisible(Selection.OfType<BlackboardCategory>().Any());
//            }
//        }

//        public void OnDragExitedEvent(DragExitedEvent evt)
//        {
//            SetCategoryDragIndicatorVisible(false);
//            HideScrollBoundaryRegions();
//        }

//        void OnMouseEnterEvent(MouseEnterEvent evt)
//        {
//            if (m_IsUserDraggingItems && Selection.OfType<BlackboardCategory>().Any())
//                SetCategoryDragIndicatorVisible(true);
//        }

//        void HideScrollBoundaryRegions()
//        {
//            m_BottomResizer.style.visibility = Visibility.Visible;
//            m_IsUserDraggingItems = false;
//            m_ScrollBoundaryTop.RemoveFromHierarchy();
//            m_ScrollBoundaryBottom.RemoveFromHierarchy();
//        }

//        int InsertionIndex(Vector2 pos)
//        {
//            VisualElement owner = contentContainer != null ? contentContainer : this;
//            Vector2 localPos = this.ChangeCoordinatesTo(owner, pos);

//            int index = BlackboardUtils.GetInsertionIndex(owner, localPos, Children());

//            // Clamps the index between the min and max of the child indices based on the mouse position relative to the categories on the y-axis (up/down)
//            // Checking for at least 2 children to make sure Children.First() and Children.Last() don't throw an exception
//            if (index == -1 && childCount >= 2)
//            {
//                index = localPos.y < Children().First().layout.yMin ? 0 :
//                                   localPos.y > Children().Last().layout.yMax ? childCount : -1;
//            }

//            // Don't allow the default category to be displaced
//            return Mathf.Clamp(index, 1, index);
//        }        


//        #region - Scrolling -
//        void ScrollRegionTopEnter(MouseEnterEvent mouseEnterEvent)
//        {
//            if (m_IsUserDraggingItems)
//            {
//                SetCategoryDragIndicatorVisible(false);
//                m_ScrollToTop = true;
//                m_ScrollToBottom = false;
//            }
//        }

//        void ScrollRegionTopLeave(MouseLeaveEvent mouseLeaveEvent)
//        {
//            if (m_IsUserDraggingItems)
//            {
//                m_ScrollToTop = false;
//                // If there are any categories in the selection, show drag indicator, otherwise hide
//                SetCategoryDragIndicatorVisible(Selection.OfType<BlackboardCategory>().Any());
//            }
//        }

//        void ScrollRegionBottomEnter(MouseEnterEvent mouseEnterEvent)
//        {
//            if (m_IsUserDraggingItems)
//            {
//                SetCategoryDragIndicatorVisible(false);
//                m_ScrollToBottom = true;
//                m_ScrollToTop = false;
//            }
//        }

//        void ScrollRegionBottomLeave(MouseLeaveEvent mouseLeaveEvent)
//        {
//            if (m_IsUserDraggingItems)
//            {
//                m_ScrollToBottom = false;
//                // If there are any categories in the selection, show drag indicator, otherwise hide
//                SetCategoryDragIndicatorVisible(Selection.OfType<BlackboardCategory>().Any());
//            }
//        }
//        #endregion

//        #region - Dragging -
//        void OnDragUpdatedEvent(DragUpdatedEvent evt)
//        {
//            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
//            if (selection == null)
//            {
//                SetCategoryDragIndicatorVisible(false);
//                return;
//            }

//            foreach (ISelectable selectedElement in selection)
//            {
//                var sourceItem = selectedElement as VisualElement;
//                // Don't allow user to move the default category
//                if (sourceItem is BlackboardCategory blackboardCategory && blackboardCategory.controller.Model.IsNamedCategory() == false)
//                {
//                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
//                    return;
//                }
//            }

//            Vector2 localPosition = evt.localMousePosition;
//            m_InsertIndex = InsertionIndex(localPosition);

//            if (m_InsertIndex != -1)
//            {
//                float indicatorY = 0;
//                if (m_InsertIndex == childCount)
//                {
//                    if (childCount > 0)
//                    {
//                        VisualElement lastChild = this[childCount - 1];
//                        indicatorY = lastChild.ChangeCoordinatesTo(this, new Vector2(0, lastChild.layout.height + lastChild.resolvedStyle.marginBottom)).y;
//                    }
//                    else
//                    {
//                        indicatorY = this.contentRect.height;
//                    }
//                }
//                else
//                {
//                    VisualElement childAtInsertIndex = this[m_InsertIndex];
//                    indicatorY = childAtInsertIndex.ChangeCoordinatesTo(this, new Vector2(0, -childAtInsertIndex.resolvedStyle.marginTop)).y;
//                }

//                m_DragIndicator.style.top = indicatorY - m_DragIndicator.resolvedStyle.height * 0.5f;
//                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
//            }
//            else
//            {
//                SetCategoryDragIndicatorVisible(false);
//            }

//            evt.StopPropagation();
//        }

//        void OnDragPerformEvent(DragPerformEvent evt)
//        {
//            // Don't bubble up drop operations onto blackboard upto the graph view, as it leads to nodes being created without users knowledge behind the blackboard
//            evt.StopPropagation();

//            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
//            if (selection == null || selection.Count == 0)
//            {
//                SetCategoryDragIndicatorVisible(false);
//                return;
//            }

//            // Hide the category drag indicator if no categories in selection
//            if (!selection.OfType<BlackboardCategory>().Any())
//            {
//                SetCategoryDragIndicatorVisible(false);
//            }

//            Vector2 localPosition = evt.localMousePosition;
//            m_InsertIndex = InsertionIndex(localPosition);

//            // Any categories in the selection that are from other graphs, would have to be copied as opposed to moving the categories within the same graph
//            foreach (var item in selection.ToList())
//            {
//                if (item is BlackboardCategory category)
//                {
//                    var selectedCategoryData = category.controller.Model;
//                    bool doesCategoryExistInGraph = Controller.DataStore.ContainsCategory(selectedCategoryData);
//                    if (doesCategoryExistInGraph == false)
//                    {
//                        var copyCategoryAction = new CopyCategoryAction();
//                        copyCategoryAction.categoryToCopyReference = selectedCategoryData;
//                        ViewModel.RequestModelChangeAction(copyCategoryAction);
//                        selection.Remove(item);

//                        // Remove any child inputs that belong to this category from the selection, to prevent duplicates from being copied onto the graph
//                        foreach (var otherItem in selection.ToList())
//                        {
//                            if (otherItem is SGBlackboardField blackboardField && category.Contains(blackboardField))
//                                selection.Remove(otherItem);
//                        }
//                    }
//                }
//            }

//            // Same as above, but for blackboard items (properties, keywords, dropdowns)
//            foreach (var item in selection.ToList())
//            {
//                if (item is SGBlackboardField blackboardField)
//                {
//                    var selectedBlackboardItem = blackboardField.controller.Model;
//                    bool doesInputExistInGraph = Controller.DataStore.ContainsInput(selectedBlackboardItem);
//                    if (doesInputExistInGraph == false)
//                    {
//                        var copyShaderInputAction = new CopyShaderInputAction();
//                        copyShaderInputAction.shaderInputToCopy = selectedBlackboardItem;
//                        ViewModel.RequestModelChangeAction(copyShaderInputAction);
//                        selection.Remove(item);
//                    }
//                }
//            }

//            var moveCategoryAction = new MoveCategoryAction();
//            moveCategoryAction.newIndexValue = m_InsertIndex;
//            moveCategoryAction.categoryGuids = selection.OfType<BlackboardCategory>().OrderBy(sgcat => sgcat.GetPosition().y).Select(cat => cat.viewModel.associatedCategoryGuid).ToList();
//            ViewModel.RequestModelChangeAction(moveCategoryAction);

//            SetCategoryDragIndicatorVisible(false);
//        }

//        void OnDragLeaveEvent(DragLeaveEvent evt)
//        {
//            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
//            SetCategoryDragIndicatorVisible(false);
//            m_InsertIndex = -1;
//        }

//        void OnFieldDragUpdate(DragUpdatedEvent dragUpdatedEvent)
//        {
//            // how far is the mouse into the drag boundary.
//            float dragCoeff
//                = m_ScrollToTop ? 1 - dragUpdatedEvent.localMousePosition.y / m_ScrollBoundaryBottom.contentRect.height
//                : m_ScrollToBottom ? dragUpdatedEvent.localMousePosition.y / m_ScrollBoundaryBottom.contentRect.height
//                : 0;

//            dragCoeff = Mathf.Clamp(dragCoeff, .15f, .85f);

//            // factor in fixed base speed and relative to % of total scrollable height.
//            float dragSpeed = dragCoeff * k_DraggedPropertyScrollSpeed * (scrollableHeight / 100f);

//            // Lastly, make sure the drag speed can't ever get too slow.
//            dragSpeed = Mathf.Max(dragSpeed, k_DraggedPropertyScrollSpeed);

//            if (m_ScrollToTop)
//                m_ScrollView.scrollOffset = new Vector2(m_ScrollView.scrollOffset.x, Mathf.Clamp(m_ScrollView.scrollOffset.y - dragSpeed, 0, scrollableHeight));
//            else if (m_ScrollToBottom)
//                m_ScrollView.scrollOffset = new Vector2(m_ScrollView.scrollOffset.x, Mathf.Clamp(m_ScrollView.scrollOffset.y + dragSpeed, 0, scrollableHeight));
//        }
//        #endregion

//        void InitializeAddBlackboardItemMenu()
//        {
//            m_AddBlackboardItemMenu = new GenericMenu();

//            if (ViewModel == null)
//            {
//                Assert.IsTrue(false, "Blackboard: View Model is null.");
//                return;
//            }

//            // Add category at top, followed by separator
//            m_AddBlackboardItemMenu.AddItem(new GUIContent("Category"), false, () => ViewModel.RequestModelChangeAction(ViewModel.AddCategoryAction));
//            m_AddBlackboardItemMenu.AddSeparator($"/");

//            var selectedCategoryGuid = Controller.GetFirstSelectedCategoryGuid();
//            foreach (var nameToAddActionTuple in ViewModel.PropertyNameToAddActionMap)
//            {
//                string propertyName = nameToAddActionTuple.Key;
//                AddShaderInputAction addAction = nameToAddActionTuple.Value as AddShaderInputAction;
//                addAction.categoryToAddItemToGuid = selectedCategoryGuid;
//                m_AddBlackboardItemMenu.AddItem(new GUIContent(propertyName), false, () => ViewModel.RequestModelChangeAction(addAction));
//            }
//            m_AddBlackboardItemMenu.AddSeparator($"/");

//            foreach (var nameToAddActionTuple in ViewModel.DefaultKeywordNameToAddActionMap)
//            {
//                string defaultKeywordName = nameToAddActionTuple.Key;
//                AddShaderInputAction addAction = nameToAddActionTuple.Value as AddShaderInputAction;
//                addAction.categoryToAddItemToGuid = selectedCategoryGuid;
//                m_AddBlackboardItemMenu.AddItem(new GUIContent($"Keyword/{defaultKeywordName}"), false, () => ViewModel.RequestModelChangeAction(addAction));
//            }
//            m_AddBlackboardItemMenu.AddSeparator($"Keyword/");

//            foreach (var nameToAddActionTuple in ViewModel.BuiltInKeywordNameToAddActionMap)
//            {
//                string builtInKeywordName = nameToAddActionTuple.Key;
//                AddShaderInputAction addAction = nameToAddActionTuple.Value as AddShaderInputAction;
//                addAction.categoryToAddItemToGuid = selectedCategoryGuid;
//                m_AddBlackboardItemMenu.AddItem(new GUIContent($"Keyword/{builtInKeywordName}"), false, () => ViewModel.RequestModelChangeAction(addAction));
//            }

//            foreach (string disabledKeywordName in ViewModel.DisabledKeywordNameList)
//            {
//                m_AddBlackboardItemMenu.AddDisabledItem(new GUIContent($"Keyword/{disabledKeywordName}"));
//            }

//            if (ViewModel.defaultDropdownNameToAdd != null)
//            {
//                string defaultDropdownName = ViewModel.DefaultDropdownNameToAdd.Item1;
//                AddShaderInputAction addAction = ViewModel.DefaultDropdownNameToAdd.Item2 as AddShaderInputAction;
//                addAction.categoryToAddItemToGuid = selectedCategoryGuid;
//                m_AddBlackboardItemMenu.AddItem(new GUIContent($"{defaultDropdownName}"), false, () => ViewModel.RequestModelChangeAction(addAction));
//            }

//            foreach (string disabledDropdownName in ViewModel.DisabledDropdownNameList)
//            {
//                m_AddBlackboardItemMenu.AddDisabledItem(new GUIContent(disabledDropdownName));
//            }
//        }

//        void ShowAddPropertyMenu()
//        {
//            m_AddBlackboardItemMenu.ShowAsContext();
//        }

//        void OnMouseUpEvent(MouseUpEvent evt)
//        {
//            this.HideScrollBoundaryRegions();
//        }

//        void OnMouseDownEvent(MouseDownEvent evt)
//        {
//            if (evt.clickCount == 2 && evt.button == (int)MouseButton.LeftMouse)
//            {
//                StartEditingPath();
//                evt.StopPropagation();
//            }
//        }

//        void StartEditingPath()
//        {
//            m_SubTitleLabel.visible = false;
//            m_PathLabelTextField.visible = true;
//            m_PathLabelTextField.value = m_SubTitleLabel.text;
//            m_PathLabelTextField.Q("unity-text-input").Focus();
//            m_PathLabelTextField.SelectAll();
//        }

//        void OnPathTextFieldKeyPressed(KeyDownEvent evt)
//        {
//            switch (evt.keyCode)
//            {
//                case KeyCode.Escape:
//                    m_EditPathCancelled = true;
//                    m_PathLabelTextField.Q("unity-text-input").Blur();
//                    break;
//                case KeyCode.Return:
//                case KeyCode.KeypadEnter:
//                    m_PathLabelTextField.Q("unity-text-input").Blur();
//                    break;
//                default:
//                    break;
//            }
//        }

//        void OnEditPathTextFinished()
//        {
//            m_SubTitleLabel.visible = true;
//            m_PathLabelTextField.visible = false;

//            var newPath = m_PathLabelTextField.text;
//            if (!m_EditPathCancelled && (newPath != m_SubTitleLabel.text))
//            {
//                newPath = BlackboardUtils.SanitizePath(newPath);
//            }

//            // Request graph path change action
//            var pathChangeAction = new ChangeGraphPathAction();
//            pathChangeAction.NewGraphPath = newPath;
//            ViewModel.RequestModelChangeAction(pathChangeAction);

//            m_SubTitleLabel.text = BlackboardUtils.FormatPath(newPath);
//            m_EditPathCancelled = false;
//        }

//        public override void Dispose()
//        {
//            m_PathLabelTextField.Q("unity-text-input").UnregisterCallback<FocusOutEvent>(e => { OnEditPathTextFinished(); }, TrickleDown.TrickleDown);
//            m_PathLabelTextField.Q("unity-text-input").UnregisterCallback<KeyDownEvent>(OnPathTextFieldKeyPressed, TrickleDown.TrickleDown);
//            UnregisterCallback<MouseUpEvent>(OnMouseUpEvent);
//            UnregisterCallback<DragExitedEvent>(OnDragExitedEvent);
//            UnregisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
//            UnregisterCallback<DragPerformEvent>(OnDragPerformEvent);
//            UnregisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
//            UnregisterCallback<DragExitedEvent>(OnDragExitedEvent);
//            UnregisterCallback<MouseEnterEvent>(OnMouseEnterEvent);
//            m_ScrollBoundaryTop.UnregisterCallback<MouseEnterEvent>(ScrollRegionTopEnter);
//            m_ScrollBoundaryTop.UnregisterCallback<DragUpdatedEvent>(OnFieldDragUpdate);
//            m_ScrollBoundaryTop.UnregisterCallback<MouseLeaveEvent>(ScrollRegionTopLeave);
//            m_ScrollBoundaryBottom.UnregisterCallback<MouseEnterEvent>(ScrollRegionBottomEnter);
//            m_ScrollBoundaryBottom.UnregisterCallback<DragUpdatedEvent>(OnFieldDragUpdate);
//            m_ScrollBoundaryBottom.UnregisterCallback<MouseLeaveEvent>(ScrollRegionBottomLeave);

//            m_BlackboardCategories.Clear();
//            m_ViewModel = null;
//            m_DragIndicator = null;
//            m_Controller = null;
//            m_AddBlackboardItemMenu = null;
//            addItemRequested = null;
//            m_BottomResizer = null;
//            m_ScrollBoundaryBottom = null;
//            m_ScrollBoundaryTop = null;
//            m_PathLabelTextField = null;
//        }
//    }
//}
