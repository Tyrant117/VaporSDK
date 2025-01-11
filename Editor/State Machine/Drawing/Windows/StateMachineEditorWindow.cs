using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Inspector;
using Vapor.StateMachines;
using VaporEditor.VisualScripting;

namespace VaporEditor.StateMachines
{
    public class StateMachineEditorWindow : EditorWindow
    {
        private VisualElement _leftPane;
        private VisualElement _rightPane;
        private ScrollView _scrollView;
        public string SelectedGuid { get; private set; }
        public StateMachineSo Asset { get; set; }

        private ISelectableEntry _currentSelectedEntry;
        private Label _globalIndex;

        public ISelectableEntry CurrentSelectedEntry
        {
            get => _currentSelectedEntry;
            set
            {
                if (_currentSelectedEntry != value)
                {
                    _currentSelectedEntry?.Deselect();
                    _currentSelectedEntry = value;
                    _currentSelectedEntry?.Select();
                }
            }
        }
        
        public void Initialize(string assetGuid)
        {
            var asset = AssetDatabase.LoadAssetAtPath<StateMachineSo>(AssetDatabase.GUIDToAssetPath(assetGuid));
            if (asset == null)
            {
                Debug.Log("Initialized: Asset null");
                return;
            }

            if (!EditorUtility.IsPersistent(asset))
            {
                Debug.Log("Initialized: Asset not persistent");
                return;
            }

            if (SelectedGuid == assetGuid)
            {
                Debug.Log($"Initialized: Already initialized: {asset.name}");
                return;
            }

            Debug.Log($"Initialized: {asset.GetType()}");

            var path = AssetDatabase.GetAssetPath(asset);
            SelectedGuid = assetGuid;
            Asset = asset;
            string stateMachineName = Path.GetFileNameWithoutExtension(path);

            var view = new TwoPaneSplitView(0, 256, TwoPaneSplitViewOrientation.Horizontal);
            _leftPane = new VisualElement();
            _rightPane = new VisualElement();
            view.Add(_leftPane);
            view.Add(_rightPane);
            rootVisualElement.Add(view);

            var buttonMenu = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1f,
                    maxHeight = 20f,
                }
            };
            buttonMenu.Add(new Button(AddStateClicked)
            {
                text = "+",
                style =
                {
                    flexShrink = 1f,
                    flexGrow = 1f
                }
            });
            buttonMenu.Add(new Button(AddStateMachineClicked)
            {
                text = "+ FSM",
                style =
                {
                    flexShrink = 1f,
                    flexGrow = 1f
                }
            });
            buttonMenu.Add(new Button(AddGlobalTransitionClicked)
            {
                text = "+ ->",
                style =
                {
                    flexShrink = 1f,
                    flexGrow = 1f
                }
            });
            
            _leftPane.Add(buttonMenu);
            _scrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
            {
                style =
                {
                    marginLeft = 2f,
                    marginRight = 2f,
                    marginTop = 2f,
                    marginBottom = 2f
                }
            };
            _globalIndex = new Label("Global Transitions");
            _scrollView.Add(_globalIndex);
            _leftPane.Add(_scrollView);

            foreach (var assetState in Asset.States)
            {
                var entry = new StateMachineEntry
                {
                    Label =
                    {
                        text = assetState.Name
                    }
                };
                entry.Init(this, assetState);
                var idx = _scrollView.IndexOf(_globalIndex);
                _scrollView.Insert(idx, entry);
            }

            foreach (var globalTransition in Asset.GlobalTransitions)
            {
                var entry = new TransitionEntry();
                entry.Init(this, null, globalTransition);
                _scrollView.Add(entry);
            }
            
            Repaint();
        }


        private void AddStateClicked()
        {
            var newState = new SerializableState()
            {
                Name = "New State",
            };
            Asset.States.Add(newState);

            var entry = new StateMachineEntry
            {
                Label =
                {
                    text = newState.Name
                }
            };
            entry.Init(this, newState);

            var idx = _scrollView.IndexOf(_globalIndex);
            _scrollView.Insert(idx, entry);

            EditorUtility.SetDirty(Asset);
            AssetDatabase.SaveAssetIfDirty(Asset);
            
            Repaint();
        }
        
        private void AddStateMachineClicked()
        {
            
        }

        private void AddGlobalTransitionClicked()
        {
            var transition = new SerializableTransition()
            {
                FromStateName = "Any"
            };
            Asset.GlobalTransitions.Add(transition);

            var entry = new TransitionEntry();
            entry.Init(this, null, transition);
            _scrollView.Add(entry);
            UpdateAsset();
        }

        private void OnEnable()
        {
            this.SetAntiAliasing(4);
        }

        private void OnDisable()
        {
            Resources.UnloadUnusedAssets();
        }
        
        public void UpdateAsset()
        {
            EditorUtility.SetDirty(Asset);
            AssetDatabase.SaveAssetIfDirty(Asset);
            
            Repaint();
        }

        public void RemoveView()
        {
            _rightPane.DisconnectChildren();
        }

        public void CreateStateView(StateMachineEntry stateMachineEntry)
        {
            var view = new StateView();
            view.Init(this, stateMachineEntry);
            _rightPane.Add(view);
        }

        public void CreateTransitionView(TransitionEntry transitionEntry)
        {
            var view = new TransitionView();
            view.Init(this, transitionEntry);
            _rightPane.Add(view);
        }

        public void RemoveStateEntry(StateMachineEntry entry)
        {
            Asset.States.Remove(entry.State);
            _scrollView.Remove(entry);
            CurrentSelectedEntry = null;
            
            UpdateAsset();
            
            Repaint();
        }

        public void RemoveTransitionEntry(TransitionEntry entry)
        {
            Asset.GlobalTransitions.Remove(entry.Transition);
            _scrollView.Remove(entry);
            CurrentSelectedEntry = null;
            
            UpdateAsset();
            
            Repaint();
        }
    }
}
