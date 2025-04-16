using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class PlayModeSwitchNode : PlayModeNodeBase
    {
        private readonly (string, string)[] _caseNodeGuids;
        private readonly Dictionary<string, PlayModeNodeBase> _caseNodes;
        private readonly Type _enumType;

        private object _currentEnumKey;
        private int _currentIntKey;
        private string _currentStringKey;

        public PlayModeSwitchNode(BlueprintDesignNodeDto dto, List<BlueprintWireDto> wires)
        {
            Guid = dto.Guid;
            Uuid = Guid.GetStableHashU32();

            SetupWires(wires);
            SetupInputPins(dto);
            _caseNodeGuids = new (string, string)[OutputWires.Count];
            _caseNodes = new Dictionary<string, PlayModeNodeBase>(OutputWires.Count);

            _enumType = InPortValues[PinNames.VALUE_IN].GetType();
            if (_enumType.IsEnum)
            {
                _currentEnumKey = null;
            }
            else if (_enumType == typeof(int))
            {
                _currentIntKey = 0;
            }
            else if (_enumType == typeof(string))
            {
                _currentStringKey = string.Empty;
            }
            else
            {
                Debug.LogError($"Enum type not supported: {_enumType}");
            }

            for (var i = 0; i < OutputWires.Count; i++)
            {
                var wire = OutputWires[i];
                if (wire.Guid.EmptyOrNull())
                {
                    continue;
                }

                _caseNodeGuids[i] = (wire.LeftName, wire.RightGuid);
                _caseNodes.Add(wire.LeftName, null);
            }
        }

        public override void Init(PlayModeClass playModeClass, PlayModeMethod playModeMethod)
        {
            base.Init(playModeClass, playModeMethod);
            for (var i = 0; i < _caseNodeGuids.Length; i++)
            {
                if (_caseNodeGuids[i].Item2.EmptyOrNull())
                {
                    continue;
                }

                _caseNodes[_caseNodeGuids[i].Item1] = Method.GetNode(_caseNodeGuids[i].Item2);
            }
        }

        protected override void CacheInputValues()
        {
            GetAllInputPinValues();
        }

        protected override void WriteOutputValues()
        {
            if (_enumType.IsEnum)
            {
                _currentEnumKey = InPortValues[PinNames.VALUE_IN];
            }
            else if (_enumType == typeof(int))
            {
                _currentIntKey = (int)InPortValues[PinNames.VALUE_IN];
            }
            else
            {
                _currentStringKey = (string)InPortValues[PinNames.VALUE_IN];
            }
        }

        protected override void Continue()
        {
            if (!Method.IsEvaluating)
            {
                return;
            }

            if (_enumType.IsEnum)
            {
                if (_caseNodes.TryGetValue(_currentEnumKey.ToString(), out var caseNode))
                {
                    caseNode?.InvokeAndContinue();
                }
                else
                {
                    _caseNodes["Default"]?.InvokeAndContinue();
                }
            }
            else if (_enumType == typeof(int))
            {
                if (_caseNodes.TryGetValue(_currentIntKey.ToString(), out var caseNode))
                {
                    caseNode?.InvokeAndContinue();
                }
                else
                {
                    _caseNodes["Default"]?.InvokeAndContinue();
                }
            }
            else if (_enumType == typeof(string))
            {
                if (_caseNodes.TryGetValue(_currentStringKey, out var caseNode))
                {
                    caseNode?.InvokeAndContinue();
                }
                else
                {
                    _caseNodes["Default"]?.InvokeAndContinue();
                }
            }
        }
    }
}