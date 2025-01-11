using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Vapor.Inspector;
using Vapor.StateMachines;

namespace VaporEditor.StateMachines
{
    [UxmlElement]
    public partial class TransitionEntry : VisualElement, ISelectableEntry
    {
        
        private Label _label;
        public Label Label
        {
            get
            {
                _label ??= this.Q<Label>("TransitionLabel");
                return _label;
            }
        }
        
        public SerializableTransition Transition { get; private set; }
        public bool IsGlobalTransition => _stateEntry == null;
        private StateMachineEntry _stateEntry;
        public StateMachineEntry StateEntry => _stateEntry;

        private StateMachineEditorWindow _window;
        private readonly ButtonManipulator _button;
        

        public TransitionEntry() : this("TransitionEntry")
        {

        }

        public TransitionEntry(string uxmlPath)
        {
            var uxml = Resources.Load<VisualTreeAsset>(uxmlPath);
            var ss = Resources.Load<StyleSheet>(uxmlPath);
            styleSheets.Add(ss);
            uxml.CloneTree(this);

            _button = new ButtonManipulator("outline", this.Q("Hover"));
            _button.WithOnClick(ButtonManipulator.ClickType.ClickOnUp, OnSelectEntry);
            _button.WithActivator<ButtonManipulator>(EventModifiers.None, MouseButton.LeftMouse);
            this.AddManipulator(_button);
        }

        public void Init(StateMachineEditorWindow window, StateMachineEntry stateEntry, SerializableTransition transition)
        {
            Transition = transition;
            _stateEntry = stateEntry;
            _window = window;

            Label.text = transition.ToStateName.EmptyOrNull() ? "No Transition" : $"Transition To -> {transition.ToStateName}";
        }

        private void OnSelectEntry(EventBase obj)
        {
            _window.CurrentSelectedEntry = this;
        }

        public void Select()
        {
            _button.Select();
            _window.CreateTransitionView(this);
        }

        public void Deselect()
        {
            _button.Deselect();
            _window.RemoveView();
        }

        public void UpdateName(string newName)
        {
            Label.text = $"Transition To -> {newName}";
            Transition.ToStateName = newName;
        }
    }
}
