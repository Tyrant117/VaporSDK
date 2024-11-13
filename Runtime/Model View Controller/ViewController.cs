using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Inspector;

namespace Vapor.ModelViewController
{
    public abstract class ViewController : VaporBehaviour
    {
        [BoxGroup("View Controller"), SerializeField]
        protected UIDocument Document;
        [BoxGroup("View Controller"), SerializeField]
        private bool _isUnique;
        [BoxGroup("View Controller"), SerializeField]
        private StyleSheet _styleSheet;

        public bool IsOpen => GetView().IsOpen();



        protected async void Start()
        {
            CreateView(_styleSheet);
            Document.rootVisualElement.Add(GetView());
            await Awaitable.NextFrameAsync();
            InitializeView();
        }

        private void OnEnable()
        {
            if (_isUnique)
            {
                SetUnique();
            }
        }

        private void OnDisable()
        {
            if (_isUnique)
            {
                RemoveUnique();
            }
        }

        protected abstract void SetUnique();
        protected abstract void RemoveUnique();

        protected abstract View GetView();
        protected abstract void CreateView(StyleSheet styleSheet);
        protected virtual void InitializeView() { GetView().InitializeView(); }

        public void Show() => GetView().Show();
        public void Hide() => GetView().Hide();
        public bool Toggle() 
        { 
            GetView().ToggleDisplay();
            var current = GetView().style.display.value;
            return current == DisplayStyle.Flex;
        }
    }
}
