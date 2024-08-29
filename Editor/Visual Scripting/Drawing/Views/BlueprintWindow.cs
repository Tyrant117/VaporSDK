using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using IResizable = Vapor.Inspector.IResizable;
using ResizableElement = Vapor.Inspector.ResizableElement;

namespace VaporEditor.VisualScripting
{
    #region Types
    public interface ISelectionProvider
    {
        List<ISelectable> GetSelection { get; }
    }
    #endregion

    public class BlueprintWindow : GraphElement, IResizable
    {

        #region Properties
        IBPViewModel ViewModel { get; set; }

        // These are used as default values for styling and layout purposes
        // They can be overriden if a child class wants to roll its own style and layout behavior
        public virtual string LayoutKey => "VaporEditor.Graphs.BlueprintWindow";
        public virtual string StyleName => "BlueprintWindow";
        public virtual string UxmlName => "BlueprintWindow";
        
        // Each sub-window will override these if they need to
        public virtual string ElementName => "";
        public virtual string WindowTitle => "";

        private VisualElement _parentView;
        public VisualElement ParentView
        {
            get
            {
                if (!IsWindowed && _parentView == null)
                {
                    _parentView = GetFirstAncestorOfType<GraphView>();
                }
                return _parentView;
            }
            set
            {
                if (!IsWindowed)
                {
                    return;
                }
                _parentView = value;
            }
        }

        public List<ISelectable> Selection
        {
            get
            {
                if (ParentView is ISelectionProvider selectionProvider)
                {
                    return selectionProvider.GetSelection;
                }

                Assert.IsTrue(false, "GraphSubWindow was unable to find a selection provider. Please check if parent view of: " + name + " implements ISelectionProvider::GetSelection");
                return new List<ISelectable>();
            }
        }

        public override VisualElement contentContainer => ContentContainer;
        #endregion

        #region Fields
        #endregion

        #region Window Properties
        public override string title
        {
            get { return TitleLabel.text; }
            set { TitleLabel.text = value; }
        }

        public string SubTitle
        {
            get { return SubTitleLabel.text; }
            set { SubTitleLabel.text = value; }
        }

        // Intended for future handling of docking to sides of the shader graph window
        private bool _isWindowed;
        public bool IsWindowed
        {
            get { return _isWindowed; }
            set
            {
                if (_isWindowed == value) return;

                if (value)
                {
                    capabilities &= ~Capabilities.Movable;
                    AddToClassList("windowed");
                    this.RemoveManipulator(_dragger);
                }
                else
                {
                    capabilities |= Capabilities.Movable;
                    RemoveFromClassList("windowed");
                    this.AddManipulator(_dragger);
                }
                _isWindowed = value;
            }
        }

        private bool _isResizable = false;
        // Can be set by child classes as needed
        public bool IsWindowResizable
        {
            get => _isResizable;
            set
            {
                if (_isResizable != value)
                {
                    _isResizable = value;
                    HandleResizingBehavior(_isResizable);
                }
            }
        }

        private bool _isScrollable = false;
        // Can be set by child classes as needed
        public bool IsWindowScrollable
        {
            get => _isScrollable;
            set
            {
                if (_isScrollable != value)
                {
                    _isScrollable = value;
                    HandleScrollingBehavior(_isScrollable);
                }
            }
        }

        public float ScrollableWidth
        {
            get { return ScrollView.contentContainer.layout.width - ScrollView.contentViewport.layout.width; }
        }

        public float ScrollableHeight
        {
            get { return contentContainer.layout.height - ScrollView.contentViewport.layout.height; }
        }

        // This needs to be something that each subclass defines for itself at creation time
        // if they all use the same they'll be stacked on top of each other at SG window creation
        protected WindowDockingLayout WindowDockingLayout { get; private set; } = new WindowDockingLayout
        {
            DockingTop = true,
            DockingLeft = false,
            VerticalOffset = 8,
            HorizontalOffset = 8,
        };
        #endregion


        private Dragger _dragger;

        // Used to cache the window docking layout between resizing operations as it interferes with window resizing operations
        private IStyle _cachedWindowDockingStyle;

        protected VisualElement MainContainer;
        protected VisualElement Root;
        protected Label TitleLabel;
        protected Label SubTitleLabel;
        protected ScrollView ScrollView;
        protected VisualElement ContentContainer;
        protected VisualElement HeaderItem;        

        protected BlueprintWindow(IBPViewModel viewModel)
        {
            ViewModel = viewModel;
            _parentView = ViewModel.ParentView;
            ParentView.Add(this);

            Debug.Log($"Loading Stylesheet Styles/{StyleName}");
            Debug.Log($"Loading UXML UXML/{UxmlName}");

            var styleSheet = Resources.Load<StyleSheet>($"Styles/{StyleName}");
            // Setup VisualElement from Stylesheet and UXML file
            styleSheets.Add(styleSheet);
            var uxml = Resources.Load<VisualTreeAsset>($"UXML/{UxmlName}");
            Assert.IsNotNull(uxml, $"Invalid UXML File: UXML/{UxmlName}");
            MainContainer = uxml.Instantiate();
            MainContainer.AddToClassList("mainContainer");

            Root = MainContainer.Q("content");
            HeaderItem = MainContainer.Q("header");
            HeaderItem.AddToClassList("subWindowHeader");
            ScrollView = MainContainer.Q<ScrollView>("scrollView");
            TitleLabel = MainContainer.Q<Label>("titleLabel");
            SubTitleLabel = MainContainer.Q<Label>("subTitleLabel");
            ContentContainer = MainContainer.Q("contentContainer");

            hierarchy.Add(MainContainer);

            capabilities |= Capabilities.Movable | Capabilities.Resizable;
            style.overflow = Overflow.Hidden;
            focusable = false;

            name = ElementName;
            title = WindowTitle;

            ClearClassList();
            AddToClassList(name);

            BuildManipulators();

            // prevent Zoomer manipulator
            RegisterCallback<WheelEvent>(e =>
            {
                e.StopPropagation();
            });
        }

        private void BuildManipulators()
        {
            _dragger = new Dragger { clampToParentEdges = true };
            RegisterCallback<MouseUpEvent>(OnMoveEnd);
            this.AddManipulator(_dragger);
        }

        public virtual void Dispose()
        {
            MainContainer = null;
            Root = null;
            TitleLabel = null;
            SubTitleLabel = null;
            ScrollView = null;
            ContentContainer = null;
            HeaderItem = null;
            _parentView = null;
            _cachedWindowDockingStyle = null;
            styleSheets.Clear();
        }

        #region - Visibility -
        public void ShowWindow()
        {
            style.visibility = Visibility.Visible;
            ScrollView.style.display = DisplayStyle.Flex;
            MarkDirtyRepaint();
        }

        public void HideWindow()
        {
            style.visibility = Visibility.Hidden;
            ScrollView.style.display = DisplayStyle.None;
            MarkDirtyRepaint();
        }
        #endregion

        #region - Layout -
        public void ClampToParentLayout(Rect parentLayout)
        {
            WindowDockingLayout.CalculateDockingCornerAndOffset(layout, parentLayout);
            WindowDockingLayout.ClampToParentWindow();

            // If the parent shader graph window is being resized smaller than this window on either axis
            if (parentLayout.width < this.layout.width || parentLayout.height < this.layout.height)
            {
                // Don't adjust the sub window in this case as it causes flickering errors and looks broken
            }
            else
            {
                WindowDockingLayout.ApplyPosition(this);
            }

            SerializeLayout();
        }

        public void OnStartResize()
        {
            _cachedWindowDockingStyle = this.style;
        }

        public void OnResized()
        {
            if (_cachedWindowDockingStyle != null)
            {
                style.left = _cachedWindowDockingStyle.left;
                style.right = _cachedWindowDockingStyle.right;
                style.bottom = _cachedWindowDockingStyle.bottom;
                style.top = _cachedWindowDockingStyle.top;
            }
            WindowDockingLayout.Size = layout.size;
            SerializeLayout();
        }

        private void SerializeLayout()
        {
            WindowDockingLayout.Size = layout.size;
            var serializedLayout = JsonUtility.ToJson(WindowDockingLayout);
            EditorUserSettings.SetConfigValue(LayoutKey, serializedLayout);
        }

        public void DeserializeLayout()
        {
            var serializedLayout = EditorUserSettings.GetConfigValue(LayoutKey);
            if (!string.IsNullOrEmpty(serializedLayout))
            {
                WindowDockingLayout = JsonUtility.FromJson<WindowDockingLayout>(serializedLayout);
            }
            else
            {
                // The window size needs to come from the stylesheet or UXML as opposed to being defined in code
                WindowDockingLayout.Size = layout.size;
            }

            WindowDockingLayout.ApplySize(this);
            WindowDockingLayout.ApplyPosition(this);
        }

        private void OnMoveEnd(MouseUpEvent upEvent)
        {
            WindowDockingLayout.CalculateDockingCornerAndOffset(layout, ParentView.layout);
            WindowDockingLayout.ClampToParentWindow();

            SerializeLayout();
        }

        public bool CanResizePastParentBounds()
        {
            return false;
        }
        #endregion

        #region - Resizing -
        private void HandleResizingBehavior(bool isResizable)
        {
            if (isResizable)
            {
                var resizeElement = this.Q<ResizableElement>();
                resizeElement.BindOnResizeCallback(OnWindowResize);
                hierarchy.Add(resizeElement);
            }
            else
            {
                var resizeElement = this.Q<ResizableElement>();
                resizeElement.SetResizeRules(ResizableElement.Resizer.None);
                hierarchy.Remove(resizeElement);
            }
        }

        private void OnWindowResize(MouseUpEvent upEvent)
        {
        }
        #endregion

        #region - Scrolling -
        private void HandleScrollingBehavior(bool scrollable)
        {
            if (scrollable)
            {
                // Remove the categories container from the content item and add it to the scrollview
                ContentContainer.RemoveFromHierarchy();
                ScrollView.Add(ContentContainer);
                AddToClassList("scrollable");
            }
            else
            {
                // Remove the categories container from the scrollview and add it to the content item
                ContentContainer.RemoveFromHierarchy();
                Root.Add(ContentContainer);

                RemoveFromClassList("scrollable");
            }
        }
        #endregion
    }
}
