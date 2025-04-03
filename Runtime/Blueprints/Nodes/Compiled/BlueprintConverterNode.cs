using System;
using System.Collections.Generic;

namespace Vapor.Blueprints
{
    public class BlueprintConverterNode : BlueprintBaseNode
    {
        private readonly Delegate _function;
        private readonly object[] _parameterValues;

        public BlueprintConverterNode(BlueprintDesignNodeDto dto)
        {
            Guid = dto.Guid;
            dto.Properties.TryGetValue(NodePropertyNames.K_METHOD_DECLARING_TYPE, out var methodAssemblyType);
            dto.Properties.TryGetValue(NodePropertyNames.K_METHOD_NAME, out var methodName);
            dto.Properties.TryGetValue(NodePropertyNames.K_METHOD_PARAMETER_TYPES, out var methodParameterTypes);
            var methodInfo = RuntimeReflectionUtility.GetMethodInfo((Type)methodAssemblyType.Item2, (string)methodName.Item2, (string[])methodParameterTypes.Item2);
            _function = MethodDelegateHelper.GetDelegateForMethod(methodInfo);
            InputWires = dto.InputWires;
            OutputWires = dto.OutputWires;
            
            _parameterValues = new object[1];
            InPortValues = new Dictionary<string, object>(1);
            OutPortValues = new Dictionary<string, object>(1);
            InPortValues[PinNames.EXECUTE_IN] = null;
            OutPortValues[PinNames.RETURN] = null;
        }

        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
        }

        protected override void CacheInputValues()
        {
            GetAllInputPinValues();
        }

        protected override void WriteOutputValues()
        {
            if (_function == null)
            {
                return;
            }

            _parameterValues[0] = InPortValues[PinNames.EXECUTE_IN];
            OutPortValues[PinNames.RETURN] = _function.DynamicInvoke(_parameterValues);
        }

        protected override void Continue()
        {
        }
    }
}