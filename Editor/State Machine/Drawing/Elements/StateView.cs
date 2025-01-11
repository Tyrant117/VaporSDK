using System;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.StateMachines;

namespace VaporEditor.StateMachines
{
    [UxmlElement]
    public partial class StateView : VisualElement
    {
        private TextField _nameField;
        public TextField NameField
        {
            get
            {
                _nameField ??= this.Q<TextField>("Name");
                return _nameField;
            }
        }
        
        private Toggle _exitInstantlyToggle;
        public Toggle ExitInstantlyToggle
        {
            get
            {
                _exitInstantlyToggle ??= this.Q<Toggle>("CanExitInstantly");
                return _exitInstantlyToggle;
            }
        }
        
        private Toggle _transitionToSelfToggle;
        public Toggle TransitionToSelfToggle
        {
            get
            {
                _transitionToSelfToggle ??= this.Q<Toggle>("CanTransitionToSelf");
                return _transitionToSelfToggle;
            }
        }
        
        private EnumField _typeField;
        public EnumField TypeField
        {
            get
            {
                _typeField ??= this.Q<EnumField>("Type");
                return _typeField;
            }
        }
        
        private StateMachineEditorWindow _window;
        private StateMachineEntry _entry;
        
        public StateView() : this("StateView")
        {

        }

        private StateView(string uxmlPath)
        {
            var uxml = Resources.Load<VisualTreeAsset>(uxmlPath);
            var ss = Resources.Load<StyleSheet>(uxmlPath);
            styleSheets.Add(ss);
            uxml.CloneTree(this);
        }

        public void Init(StateMachineEditorWindow window, StateMachineEntry entry)
        {
            _window = window;
            _entry = entry;
            
            NameField.value = entry.State.Name;
            NameField.RegisterValueChangedCallback(OnNameChanged);
            
            ExitInstantlyToggle.value = entry.State.CanExitInstantly;
            ExitInstantlyToggle.RegisterValueChangedCallback(OnExitInstantlyToggled);
            
            TransitionToSelfToggle.value = entry.State.CanTransitionToSelf;
            TransitionToSelfToggle.RegisterValueChangedCallback(OnTransitionToSelfToggled);

            TypeField.value = entry.State.StateType;
            TypeField.RegisterValueChangedCallback(OnTypeChanged);

            this.Q<Button>("Delete").clicked += OnDeleteClicked;
        }

        private void OnDeleteClicked()
        {
            _window.RemoveStateEntry(_entry);
        }

        private void OnNameChanged(ChangeEvent<string> evt)
        {
            _entry.UpdateName(evt.newValue);
            _window.UpdateAsset();
        }
        
        private void OnExitInstantlyToggled(ChangeEvent<bool> evt)
        {
            _entry.State.CanExitInstantly = evt.newValue;
            _window.UpdateAsset();
        }
        
        private void OnTransitionToSelfToggled(ChangeEvent<bool> evt)
        {
            _entry.State.CanTransitionToSelf = evt.newValue;
            _window.UpdateAsset();
        }
        
        private void OnTypeChanged(ChangeEvent<Enum> evt)
        {
            _entry.State.StateType = (SerializableState.Type)evt.newValue;
            _window.UpdateAsset();
        }
    }
}
