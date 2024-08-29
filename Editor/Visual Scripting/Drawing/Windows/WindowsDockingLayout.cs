using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace VaporEditor.VisualScripting
{
    [Serializable]
    public class WindowDockingLayout
    {
        [SerializeField]
        bool _dockingLeft;

        public bool DockingLeft
        {
            get => _dockingLeft;
            set => _dockingLeft = value;
        }

        [SerializeField]
        bool _dockingTop;

        public bool DockingTop
        {
            get => _dockingTop;
            set => _dockingTop = value;
        }

        [SerializeField]
        float _verticalOffset;

        public float VerticalOffset
        {
            get => _verticalOffset;
            set => _verticalOffset = value;
        }

        [SerializeField]
        float _horizontalOffset;

        public float HorizontalOffset
        {
            get => _horizontalOffset;
            set => _horizontalOffset = value;
        }

        [SerializeField]
        Vector2 _size;

        public Vector2 Size
        {
            get => _size;
            set => _size = value;
        }

        public void CalculateDockingCornerAndOffset(Rect layout, Rect parentLayout)
        {
            Vector2 layoutCenter = new(layout.x + layout.width * .5f, layout.y + layout.height * .5f);
            layoutCenter /= parentLayout.size;

            _dockingLeft = layoutCenter.x < .5f;
            _dockingTop = layoutCenter.y < .5f;

            _horizontalOffset = _dockingLeft ? layout.x : parentLayout.width - layout.x - layout.width;

            _verticalOffset = _dockingTop ? layout.y : parentLayout.height - layout.y - layout.height;

            _size = layout.size;
        }

        public void ClampToParentWindow()
        {
            _horizontalOffset = Mathf.Max(0f, _horizontalOffset);
            _verticalOffset = Mathf.Max(0f, _verticalOffset);
        }

        public void ApplyPosition(VisualElement target)
        {
            if (DockingLeft)
            {
                target.style.right = float.NaN;
                target.style.left = HorizontalOffset;
            }
            else
            {
                target.style.right = HorizontalOffset;
                target.style.left = float.NaN;
            }

            if (DockingTop)
            {
                target.style.bottom = float.NaN;
                target.style.top = VerticalOffset;
            }
            else
            {
                target.style.top = float.NaN;
                target.style.bottom = VerticalOffset;
            }
        }

        public void ApplySize(VisualElement target)
        {
            target.style.width = Size.x;
            target.style.height = Size.y;
        }

        public Rect GetLayout(Rect parentLayout) => new()
        {
            size = Size,
            x = DockingLeft ? HorizontalOffset : parentLayout.width - Size.x - HorizontalOffset,

            y = DockingTop ? VerticalOffset : parentLayout.height - Size.y - VerticalOffset
        };
    }
}
