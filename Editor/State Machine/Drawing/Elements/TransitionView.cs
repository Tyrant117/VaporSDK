using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Inspector;
using Vapor.StateMachines;

namespace VaporEditor.StateMachines
{
    [UxmlElement]
    public partial class TransitionView : VisualElement
    {
        private DropdownField _toStateField;
        public DropdownField ToStateField
        {
            get
            {
                _toStateField ??= this.Q<DropdownField>("ToState");
                return _toStateField;
            }
        }
        
        private IntegerField _desireField;
        public IntegerField DesireField
        {
            get
            {
                _desireField ??= this.Q<IntegerField>("Desire");
                return _desireField;
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
        
        private DropdownField _methodField;
        public DropdownField MethodField
        {
            get
            {
                _methodField ??= this.Q<DropdownField>("MethodEvaluator");
                return _methodField;
            }
        }
        
        private StateMachineEditorWindow _window;
        private TransitionEntry _entry;

        public TransitionView() : this("TransitionView")
        {

        }

        public TransitionView(string uxmlPath)
        {
            var uxml = Resources.Load<VisualTreeAsset>(uxmlPath);
            var ss = Resources.Load<StyleSheet>(uxmlPath);
            styleSheets.Add(ss);
            uxml.CloneTree(this);
        }
        
        public void Init(StateMachineEditorWindow window, TransitionEntry entry)
        {
            _window = window;
            _entry = entry;
            
            ToStateField.choices.Clear();
            foreach (var serializableState in _window.Asset.States)
            {
                ToStateField.choices.Add(serializableState.Name);
            }

            ToStateField.value = entry.Transition.ToStateName;
            ToStateField.RegisterValueChangedCallback(OnToStateChanged);

            DesireField.value = entry.Transition.Desire;
            DesireField.RegisterValueChangedCallback(OnDesireChanged);
            
            TypeField.value = entry.Transition.TransitionType;
            TypeField.RegisterValueChangedCallback(OnTypeChanged);
            
            MethodField.choices.Clear();
            FilterForEvaluator();

            MethodField.value = entry.Transition.MethodEvaluatorName;
            MethodField.RegisterValueChangedCallback(OnMethodChanged);
            
            this.Q<Button>("Delete").clicked += OnDeleteClicked;
        }

        private void OnToStateChanged(ChangeEvent<string> evt)
        {
            _entry.UpdateName(evt.newValue);
            _window.UpdateAsset();
        }
        
        private void OnDesireChanged(ChangeEvent<int> evt)
        {
            _entry.Transition.Desire = evt.newValue;
            _window.UpdateAsset();
        }
        
        private void OnTypeChanged(ChangeEvent<Enum> evt)
        {
            _entry.Transition.TransitionType = (SerializableTransition.Type)evt.newValue;

            if (!Equals(evt.newValue, evt.previousValue))
            {
                MethodField.choices.Clear();
                FilterForEvaluator();
                
                MethodField.value = string.Empty;
            }

            _window.UpdateAsset();
        }
        
        private void OnMethodChanged(ChangeEvent<string> evt)
        {
            _entry.Transition.MethodEvaluatorName = evt.newValue;
            _window.UpdateAsset();
        }
        
        private void OnDeleteClicked()
        {
            if (!_entry.IsGlobalTransition)
            {
                _entry.StateEntry.RemoveTransitionEntry(_entry);
            }
            else
            {
                _window.RemoveTransitionEntry(_entry);
            }
        }

        private void FilterForEvaluator()
        {
            MethodField.Hide();
            if (_window.Asset.OwnerType.EmptyOrNull()) return;
            
            var type = Type.GetType(_window.Asset.OwnerType);
            if (type == null) return;
            
            
            var methods = type.GetMethods();
            foreach (var method in methods)
            {
                switch (_entry.Transition.TransitionType)
                {
                    case SerializableTransition.Type.WaitForTrue:
                        MethodField.Show();
                        if (method.ReturnType == typeof(bool) && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(Transition))
                        {
                            MethodField.choices.Add(method.Name);
                        }
                        break;
                    case SerializableTransition.Type.TimedTransition:
                        MethodField.Show();
                        if (method.ReturnType == typeof(float) && method.GetParameters().Length == 0)
                        {
                            MethodField.choices.Add(method.Name);
                        }
                        break;
                    case SerializableTransition.Type.WaitForCoroutine:
                        break;
                }
                
                
            }
        }
    }
}
