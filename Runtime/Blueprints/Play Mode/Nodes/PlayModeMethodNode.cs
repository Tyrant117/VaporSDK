using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public class PlayModeMethodNode : PlayModeNodeBase
    {
        private readonly MethodInfo _methodInfo;
        private readonly bool _hasReturnValue;
        private readonly bool _isStatic;
        private readonly ParameterInfo[] _parameters;
        private readonly object[] _parameterValues;

        private readonly string _nextNodeGuid;
        private PlayModeNodeBase _nextNode;

        public PlayModeMethodNode(BlueprintDesignNodeDto dto, List<BlueprintWireDto> wires)
        {
            Guid = dto.Guid;
            Uuid = Guid.GetStableHashU32();
            Type methodType = null;
            if(dto.Properties.TryGetValue(NodePropertyNames.METHOD_DECLARING_TYPE, out var val))
            {
                methodType = (Type)TypeUtility.CastToType(val.Item2, val.Item1);
            }

            string methodName = null;
            if(dto.Properties.TryGetValue(NodePropertyNames.METHOD_NAME, out val))
            {
                methodName = (string)TypeUtility.CastToType(val.Item2, val.Item1);
            }

            string[] methodParameters = null;
            if(dto.Properties.TryGetValue(NodePropertyNames.METHOD_PARAMETER_TYPES, out val))
            {
                methodParameters = (string[])TypeUtility.CastToType(val.Item2, val.Item1);
            }
            _methodInfo = RuntimeReflectionUtility.GetMethodInfo(methodType, methodName, methodParameters);
            
            
            _hasReturnValue = _methodInfo.ReturnType != typeof(void);
            _isStatic = _methodInfo.IsStatic;
            _parameters = _methodInfo.GetParameters();
            _parameterValues = new object[_parameters.Length];

            SetupWires(wires);
            SetupInputPins(dto);
            SetupOutputPins(dto);
            _nextNodeGuid = GetOutputNodeGuidForPinName(OutputWires);
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
            foreach (var edge in InputWires)
            {
                if (edge.IsExecuteWire)
                {
                    continue;
                }

                var leftNode = Method.GetNode(edge.LeftGuid);
                leftNode.Invoke();
                if (leftNode.TryGetOutputValue(edge.LeftName, out var outputValue))
                {
                    InPortValues[edge.RightName] = outputValue;
                }
            }
        }

        protected override void WriteOutputValues()
        {
            if (_methodInfo == null)
            {
                return;
            }

            // if the method isn't static the first parameter needs to be the assigned owner
            // then custom data
            // then the other in port values
            // then the out ports
            object target = null;
            if (!_isStatic)
            {
                target = InPortValues[PinNames.OWNER];
                Assert.IsNotNull(target, $"Owner Can't Be Null.");
            }

            for (int i = 0; i < _parameters.Length; i++)
            {
                _parameterValues[i] = _parameters[i].IsOut ? null : InPortValues[_parameters[i].Name];
            }
            
            var retVal = _methodInfo.Invoke(target, _parameterValues);
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
            if (!Method.IsEvaluating)
            {
                return;
            }
            
            _nextNode?.InvokeAndContinue();
        }
    }
}