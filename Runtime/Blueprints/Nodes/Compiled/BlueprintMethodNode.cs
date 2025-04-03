using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Assertions;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class BlueprintMethodNode : BlueprintBaseNode
    {
        private readonly Delegate _function;
        private readonly bool _hasReturnValue;
        private readonly bool _isStatic;
        private readonly ParameterInfo[] _parameters;
        private readonly object[] _parameterValues;

        private readonly string _nextNodeGuid;
        private BlueprintBaseNode _nextNode;

        public BlueprintMethodNode(BlueprintDesignNodeDto dto)
        {
            Guid = dto.Guid;
            dto.Properties.TryGetValue(NodePropertyNames.K_METHOD_DECLARING_TYPE, out var methodAssemblyType);
            dto.Properties.TryGetValue(NodePropertyNames.K_METHOD_NAME, out var methodName);
            dto.Properties.TryGetValue(NodePropertyNames.K_METHOD_PARAMETER_TYPES, out var methodParameterTypes);
            var methodInfo = RuntimeReflectionUtility.GetMethodInfo((Type)methodAssemblyType.Item2, (string)methodName.Item2, (string[])methodParameterTypes.Item2);
            
            _function = MethodDelegateHelper.GetDelegateForMethod(methodInfo);
            InputWires = dto.InputWires;
            
            _hasReturnValue = methodInfo.ReturnType != typeof(void);
            _isStatic = methodInfo.IsStatic;
            _parameters = methodInfo.GetParameters();
            _parameterValues = new object[_parameters.Length + (_isStatic ? 0 : 1)];

            SetupInputPins(dto);
            SetupOutputPins(dto);
            _nextNodeGuid = GetNodeGuidForPinName(dto);
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
            foreach (var edge in InputWires)
            {
                if (edge.LeftSidePin.IsExecutePin)
                {
                    continue;
                }
                
                if (!Graph.TryGetNode(edge.LeftSidePin.NodeGuid, out var leftNode))
                {
                    continue;
                }

                leftNode.Invoke();
                if (leftNode.TryGetOutputValue(edge.LeftSidePin.PinName, out var outputValue))
                {
                    InPortValues[edge.RightSidePin.PinName] = outputValue;
                }
            }
        }

        protected override void WriteOutputValues()
        {
            if (_function == null)
            {
                return;
            }

            // if the method isn't static the first parameter needs to be the assigned owner
            // then custom data
            // then the other in port values
            // then the out ports
            int insertIdx = 0;
            if (!_isStatic)
            {
                _parameterValues[0] = InPortValues[PinNames.OWNER];
                Assert.IsNotNull(_parameterValues[0], $"Owner Can't Be Null.");
                insertIdx = 1;
            }
            for (int i = 0; i < _parameters.Length; i++)
            {
                _parameterValues[i + insertIdx] = _parameters[i].IsOut ? null : InPortValues[_parameters[i].Name];
            }

            var retVal = _function.DynamicInvoke(_parameterValues);
            if (_hasReturnValue)
            {
                OutPortValues[PinNames.RETURN] = retVal;
            }

            for (int i = 0; i < _parameters.Length; i++)
            {
                if (_parameters[i].IsOut)
                {
                    OutPortValues[_parameters[i].Name] = _parameterValues[i];
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