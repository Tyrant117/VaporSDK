using System;
using System.Reflection;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class BlueprintMemberAccessNode : BlueprintBaseNode
    {
        private readonly string _nextNodeGuid;
        private BlueprintBaseNode _nextNode;
        private readonly FieldInfo _fieldInfo;
        private readonly string _variableName;
        private readonly VariableScopeType _scope;
        private readonly VariableAccessType _access;

        public BlueprintMemberAccessNode(BlueprintDesignNodeDto dto)
        {
            Guid = dto.Guid;
            InputWires = dto.InputWires;
            OutputWires = dto.OutputWires;
            
            SetupInputPins(dto);
            SetupOutputPins(dto);
            _nextNodeGuid = GetNodeGuidForPinName(dto);
            
            _scope = dto.GetProperty<VariableScopeType>(NodePropertyNames.VARIABLE_SCOPE);
            _access = dto.GetProperty<VariableAccessType>(NodePropertyNames.VARIABLE_ACCESS);

            if (InputWires.Exists(w => w.RightSidePin.PinName == PinNames.OWNER))
            {
                // This means there is a field info to get.
                var type = dto.GetProperty<Type>(NodePropertyNames.FIELD_TYPE);
                var name = dto.GetProperty<string>(NodePropertyNames.FIELD_NAME);
                _fieldInfo = RuntimeReflectionUtility.GetFieldInfo(type, name);
                _variableName = _fieldInfo.Name;
            }
            else
            {
                _variableName = dto.GetProperty<string>(NodePropertyNames.VARIABLE_NAME);
            }
        }
        
        
        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
            if (!_nextNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_nextNodeGuid, out _nextNode);
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
                            Graph.TrySetTempValue(_variableName, InPortValues[PinNames.SET_IN]);
                            break;
                        case VariableScopeType.Class:
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
                        Graph.TryGetTempValue(_variableName, out var methodVar);
                        OutPortValues[PinNames.RETURN] = methodVar;
                        break;
                    case VariableScopeType.Class:
                        Graph.TryGetTempValue(_variableName, out var classVar);
                        OutPortValues[PinNames.RETURN] = classVar;
                        break;
                }
            }
            else
            {
                switch (_access)
                {
                    case VariableAccessType.Get:
                        OutPortValues[PinNames.RETURN] = _fieldInfo.IsStatic ? _fieldInfo.GetValue(null) : _fieldInfo.GetValue(InPortValues[PinNames.OWNER]);
                        break;
                    case VariableAccessType.Set:
                        _fieldInfo.SetValue(_fieldInfo.IsStatic ? null : InPortValues[PinNames.OWNER], InPortValues[PinNames.SET_IN]);
                        OutPortValues[PinNames.RETURN] = _fieldInfo.IsStatic ? _fieldInfo.GetValue(null) : _fieldInfo.GetValue(InPortValues[PinNames.OWNER]);
                        break;
                }
            }
        }

        protected override void Continue()
        {
            if (!Graph.IsEvaluating)
            {
                return;
            }
            
            _nextNode?.InvokeAndContinue();
        }
    }
}