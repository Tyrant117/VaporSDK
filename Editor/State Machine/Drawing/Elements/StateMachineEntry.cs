using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Inspector;
using Vapor.StateMachines;

namespace VaporEditor.StateMachines
{
    [UxmlElement]
    public partial class StateMachineEntry : VisualElement, ISelectableEntry
    {
        private Label _label;
        public Label Label
        {
            get
            {
                _label ??= this.Q<Label>("StateName");
                return _label;
            }
        }
        private VisualElement _content;
        private ButtonManipulator _button;

        public VisualElement Content
        {
            get
            {
                _content ??= this.Q<VisualElement>("Content");
                return _content;
            }
        }

        public SerializableState State { get; set; }
        public StateMachineEditorWindow Window { get; set; }

        public override VisualElement contentContainer => Content;

        public StateMachineEntry() : this("StateMachineEntry")
        {

        }

        public StateMachineEntry(string uxmlPath)
        {
            var uxml = Resources.Load<VisualTreeAsset>(uxmlPath);
            var ss = Resources.Load<StyleSheet>(uxmlPath);
            styleSheets.Add(ss);
            uxml.CloneTree(this);

            _button = new ButtonManipulator("outline", hierarchy[0]);
            _button.WithOnClick(ButtonManipulator.ClickType.ClickOnUp, OnSelectEntry);
            _button.WithActivator<ButtonManipulator>(EventModifiers.None, MouseButton.LeftMouse);
            this.AddManipulator(_button);
        }

        public void Init(StateMachineEditorWindow window, SerializableState state)
        {
            State = state;
            Window = window;
            var btn = this.Q<Button>("AddTransition");
            btn.clicked += OnAddTransitionClicked;

            foreach (var transition in state.Transitions)
            {
                var entry = new TransitionEntry();
                entry.Init(Window, this, transition);
                Add(entry);
            }
        }

        private void OnAddTransitionClicked()
        {
            var transition = new SerializableTransition()
            {
                FromStateName = State.Name
            };
            State.Transitions.Add(transition);

            var entry = new TransitionEntry();
            entry.Init(Window, this, transition);
            Add(entry);
            Window.UpdateAsset();
        }
        
        private void OnSelectEntry(EventBase obj)
        {
            Window.CurrentSelectedEntry = this;
        }

        public void Select()
        {
            _button.Select();
            Window.CreateStateView(this);
        }

        public void Deselect()
        {
            _button.Deselect();
            Window.RemoveView();
        }

        public void UpdateName(string newName)
        {
            Label.text = newName;
            State.Name = newName;
        }

        public void RemoveTransitionEntry(TransitionEntry entry)
        {
            State.Transitions.Remove(entry.Transition);
            this.Remove(entry);
            Window.CurrentSelectedEntry = null;
            
            Window.UpdateAsset();
        }
    }
}
