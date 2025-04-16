using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor;
using Vapor.Blueprints;
using VaporEditor.Inspector;

namespace VaporEditor.Blueprints
{
    public class BlueprintInspectorMethodView : VisualElement
    {
        private readonly BlueprintMethodGraph _model;
        private readonly List<BlueprintArgument> _inputArguments = new List<BlueprintArgument>();
        private readonly List<BlueprintArgument> _outputArguments = new List<BlueprintArgument>();

        public BlueprintInspectorMethodView(BlueprintMethodGraph model)
        {
            _model = model;
            var labelStyle = Resources.Load<StyleSheet>("LabelStyle");
            if (_model.IsOverride)
            {
                var lbl = new Label("Method is Override")
                {
                    style =
                    {
                        marginTop = 6f,
                        marginLeft = 6f,
                        fontSize = 16,
                        color = Color.grey
                    }
                };
                lbl.AddToClassList("flex-label");
                Add(lbl);
                return;
            }
            
            if (_model.IsUnityOverride)
            {
                var lbl = new Label("Unity Message Method")
                {
                    style =
                    {
                        marginTop = 6f,
                        marginLeft = 6f,
                        fontSize = 16,
                        color = Color.grey
                    }
                };
                lbl.AddToClassList("flex-label");
                Add(lbl);
                return;
            }

            var abstractField = new Toggle("Abstract")
            {
                toggleOnLabelClick = false
            };
            abstractField.SetValueWithoutNotify(_model.IsAbstract);
            abstractField.RegisterValueChangedCallback(evt => _model.IsAbstract = evt.newValue);
            abstractField.styleSheets.Add(labelStyle);
            abstractField.AddToClassList("toggle-fix");
            abstractField.Q<Label>().AddToClassList("flex-label");
            
            var virtualField = new Toggle("Virtual")
            {
                toggleOnLabelClick = false
            };
            virtualField.SetValueWithoutNotify(_model.IsVirtual);
            virtualField.RegisterValueChangedCallback(evt => _model.IsVirtual = evt.newValue);
            virtualField.styleSheets.Add(labelStyle);
            virtualField.AddToClassList("toggle-fix");
            virtualField.Q<Label>().AddToClassList("flex-label");
            
            var pureField = new Toggle("Pure")
            {
                toggleOnLabelClick = false
            };
            pureField.SetValueWithoutNotify(_model.IsPure);
            pureField.RegisterValueChangedCallback(evt => _model.IsPure = evt.newValue);
            pureField.styleSheets.Add(labelStyle);
            pureField.AddToClassList("toggle-fix");
            pureField.Q<Label>().AddToClassList("flex-label");

            List<VariableAccessModifier> accessChoices = new() { VariableAccessModifier.Public, VariableAccessModifier.Protected, VariableAccessModifier.Private };
            var popup = new PopupField<VariableAccessModifier>("Access", accessChoices, _model.AccessModifier,
                v => v.ToString(), v => v.ToString());
            popup.RegisterValueChangedCallback(evt => _model.AccessModifier = evt.newValue);
            popup.styleSheets.Add(labelStyle);
            popup.Q<Label>().AddToClassList("flex-label");

            Add(abstractField);
            Add(virtualField);
            Add(pureField);
            Add(popup);

            _inputArguments.AddRange(_model.Arguments.Where(arg => !arg.IsOut && !arg.IsReturn));
            var inputArgView = new ListView(_inputArguments, makeItem: () => new VisualElement(), bindItem: (element, i) =>
            {
                element.Clear();
                var argument = _inputArguments[i];
                var ve = new VisualElement()
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        flexGrow = 1f,
                        flexShrink = 1f,
                    }
                };

                var nameField = new TextField
                {
                    style =
                    {
                        flexBasis = Length.Percent(50f)
                    }
                };
                nameField.SetValueWithoutNotify(argument.DisplayName);
                nameField.RegisterValueChangedCallback(evt => argument.SetName(evt.newValue));
                ve.Add(nameField);

                var typeSelector = new TypeSelectorField(string.Empty, argument.Type)
                {
                    style =
                    {
                        flexBasis = Length.Percent(50f),
                    }
                };
                typeSelector.TypeChanged += (_, _, cur) => argument.SetType(cur);
                ve.Add(typeSelector);
                element.Add(ve);

                var passByRef = new Toggle("Pass By Ref")
                {
                    toggleOnLabelClick = false
                };
                passByRef.SetValueWithoutNotify(argument.IsRef);
                passByRef.RegisterValueChangedCallback(evt => argument.IsRef = evt.newValue);
                element.Add(passByRef);
            })
            {
                showFoldoutHeader = true,
                headerTitle = "Input Arguments",
                showAddRemoveFooter = true,
                showBorder = true,
                showBoundCollectionSize = false,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                onAdd = OnAddInputArg,
                onRemove = OnRemoveInputArg,
                fixedItemHeight = 44,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                style =
                {
                    marginLeft = 3f,
                    marginRight = 3f,
                    flexGrow = 1f,
                    flexShrink = 1f,
                },
            };
            inputArgView.itemIndexChanged += OnReorderArguments;
            Add(inputArgView);

            _outputArguments.AddRange(_model.Arguments.Where(arg => arg.IsOut || arg.IsReturn));
            var outArgView = new ListView(_outputArguments, makeItem: () => new VisualElement(), bindItem: (element, i) =>
            {
                element.Clear();
                var argument = _outputArguments[i];
                var ve = new VisualElement()
                {
                    style =
                    {
                        marginLeft = 3f,
                        flexDirection = FlexDirection.Row,
                    }
                };

                var nameField = new TextField
                {
                    style =
                    {
                        flexBasis = Length.Percent(50f),
                    }
                };
                nameField.SetValueWithoutNotify(argument.DisplayName);
                nameField.RegisterValueChangedCallback(evt => argument.SetName(evt.newValue));
                ve.Add(nameField);

                var typeSelector = new TypeSelectorField(string.Empty, argument.Type)
                {
                    style =
                    {
                        flexBasis = Length.Percent(50f),
                    }
                };
                typeSelector.TypeChanged += (_, _, cur) => argument.SetType(cur);
                ve.Add(typeSelector);

                element.Add(ve);
            })
            {
                showFoldoutHeader = true,
                headerTitle = "Output Arguments",
                showAddRemoveFooter = true,
                showBorder = true,
                showBoundCollectionSize = false,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                onAdd = OnAddOutputArg,
                onRemove = OnRemoveOutputArg,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                style =
                {
                    marginLeft = 3f,
                    marginRight = 3f,
                }
            };
            outArgView.itemIndexChanged += (o, n) =>
            {
                if (n == 0)
                {
                    _outputArguments[n].IsReturn = true;
                    _outputArguments[n].IsOut = false;
                    if (_outputArguments.IsValidIndex(1))
                    {
                        _outputArguments[1].IsReturn = false;
                        _outputArguments[1].IsOut = true;
                    }
                }

                OnReorderArguments(o, n);
            };
            Add(outArgView);
        }

        private void OnAddInputArg(BaseListView obj)
        {
            Debug.Log("Add Input Argument");
            var arg = _model.AddInputArgument(typeof(bool));
            _inputArguments.Add(arg);
            OnReorderArguments(-1, -1);
        }

        private void OnRemoveInputArg(BaseListView obj)
        {
            if (_inputArguments.Count == 0)
            {
                return;
            }

            var argument = _inputArguments[^1];
            _inputArguments.RemoveAt(_inputArguments.Count - 1);
            _model.RemoveArgument(argument);
            OnReorderArguments(-1, -1);
            obj.Rebuild();
        }
        
        private void OnAddOutputArg(BaseListView obj)
        {
            Debug.Log("Add Output Argument");
            var arg = _model.AddOutputArgument(typeof(bool));
            _outputArguments.Add(arg);
            OnReorderArguments(-1, -1);
        }

        private void OnRemoveOutputArg(BaseListView obj)
        {
            if (_outputArguments.Count == 0)
            {
                return;
            }

            var argument = _outputArguments[^1];
            _outputArguments.RemoveAt(_outputArguments.Count - 1);
            _model.RemoveArgument(argument);
            OnReorderArguments(-1, -1);
            obj.Rebuild();
        }

        private void OnReorderArguments(int oldIndex, int newIndex)
        {
            for (int i = 0; i < _inputArguments.Count; i++)
            {
                _inputArguments[i].ParameterIndex = i;
            }

            for (int i = 0; i < _outputArguments.Count; i++)
            {
                int shiftedIndex = i + _inputArguments.Count;
                if (_outputArguments[i].IsReturn)
                {
                    _outputArguments[i].ParameterIndex = -1;
                }
                else
                {
                    _outputArguments[i].ParameterIndex = shiftedIndex;
                }
            }
            _model.OnArgumentsReordered();
        }
    }
}