using System;
using System.Collections.Generic;
using System.Reflection;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class PlayModeMemberAccessNode : PlayModeNodeBase
    {
        private readonly string _nextNodeGuid;
        private PlayModeNodeBase _nextNode;
        private readonly FieldInfo _fieldInfo;
        private readonly string _variableName;
        private readonly VariableScopeType _scope;
        private readonly VariableAccessType _access;

        public PlayModeMemberAccessNode(BlueprintDesignNodeDto dto, List<BlueprintWireDto> wires)
        {
            Guid = dto.Guid;
            Uuid = Guid.GetStableHashU32();
            
            SetupWires(wires);
            SetupInputPins(dto);
            SetupOutputPins(dto);
            _nextNodeGuid = GetOutputNodeGuidForPinName(OutputWires);
            
            _scope = dto.GetProperty<VariableScopeType>(NodePropertyNames.VARIABLE_SCOPE);
            _access = dto.GetProperty<VariableAccessType>(NodePropertyNames.VARIABLE_ACCESS);

            if (InputWires.Exists(w => w.RightName == PinNames.OWNER))
            {
                // This means there is a field info to get.
                var type = dto.GetProperty<Type>(NodePropertyNames.MEMBER_DECLARING_TYPE);
                var name = dto.GetProperty<string>(NodePropertyNames.FIELD_NAME);
                _fieldInfo = RuntimeReflectionUtility.GetFieldInfo(type, name);
                _variableName = _fieldInfo.Name;
            }
            else
            {
                _variableName = dto.GetProperty<string>(NodePropertyNames.VARIABLE_ID);
            }
        }


        public override void Init(PlayModeClass playModeClass, PlayModeMethod playModeMethod)
        {
            base.Init(playModeClass, playModeMethod);
            if (!_nextNodeGuid.EmptyOrNull())
            {
                _nextNode = Method.GetNode(_nextNodeGuid);
            }
        }

        protected override void CacheInputValues()
        {
            GetAllInputPinValues();

            if (_fieldInfo == null)
            {
                if (_access == VariableAccessType.Set)
                {
                    switch (_scope)
                    {
                        case VariableScopeType.Method:
                            Method.SetLocalVariable(_variableName, InPortValues[PinNames.SET_IN]);
                            break;
                        case VariableScopeType.Class:
                            Class.SetVariable(_variableName, InPortValues[PinNames.SET_IN]);
                            break;
                    }
                }
            }
        }

        protected override void WriteOutputValues()
        {
            if (_fieldInfo == null)
            {
                switch (_scope)
                {
                    case VariableScopeType.Method:
                        var methodVar = Method.GetLocalVariable(_variableName);
                        OutPortValues[PinNames.GET_OUT] = methodVar;
                        break;
                    case VariableScopeType.Class:
                        var classVar = Class.GetVariable(_variableName);
                        OutPortValues[PinNames.GET_OUT] = classVar;
                        break;
                }
            }
            else
            {
                switch (_access)
                {
                    case VariableAccessType.Get:
                        OutPortValues[PinNames.GET_OUT] = _fieldInfo.IsStatic ? _fieldInfo.GetValue(null) : _fieldInfo.GetValue(InPortValues[PinNames.OWNER]);
                        break;
                    case VariableAccessType.Set:
                        _fieldInfo.SetValue(_fieldInfo.IsStatic ? null : InPortValues[PinNames.OWNER], InPortValues[PinNames.SET_IN]);
                        OutPortValues[PinNames.GET_OUT] = _fieldInfo.IsStatic ? _fieldInfo.GetValue(null) : _fieldInfo.GetValue(InPortValues[PinNames.OWNER]);
                        break;
                }
            }
        }

        protected override void Continue()
        {
            if (!Method.IsEvaluating)
            {
                return;
            }
            
            _nextNode?.InvokeAndContinue();
        }
    }
}