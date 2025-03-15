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

        public BlueprintMethodNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            _function = MethodDelegateHelper.GetDelegateForMethod(dataModel.MethodInfo);
            InEdges = dataModel.InEdges;
            
            _hasReturnValue = dataModel.MethodInfo.ReturnType != typeof(void);
            _isStatic = dataModel.MethodInfo.IsStatic;
            _parameters = dataModel.MethodInfo.GetParameters();
            _parameterValues = new object[_parameters.Length + (_isStatic ? 0 : 1)];
            
            InPortValues = new Dictionary<string, object>(dataModel.InPorts.Count);
            foreach (var inPort in dataModel.InPorts.Values)
            {
                if (inPort.HasInlineValue)
                {
                    InPortValues[inPort.PortName] = inPort.GetContent();
                }
                else if(!inPort.IsExecutePin)
                {
                    if (inPort.Type.IsClass)
                    {
                        InPortValues[inPort.PortName] = null;
                    }
                    else
                    {
                        InPortValues[inPort.PortName] = Activator.CreateInstance(inPort.Type);
                    }
                }
            }

            OutPortValues = new Dictionary<string, object>(dataModel.OutPorts.Count);
            foreach (var outPort in dataModel.OutPorts.Values)
            {
                if (!outPort.IsExecutePin)
                {
                    OutPortValues[outPort.PortName] = null;
                }
            }

            var outEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePin.PinName == PinNames.EXECUTE_OUT);
            if (outEdge.RightSidePin.IsValid())
            {
                _nextNodeGuid = outEdge.RightSidePin.NodeGuid;
            }
        }

        public BlueprintMethodNode(BlueprintCompiledNodeDto dto, MethodInfo methodInfo)
        {
            Guid = dto.Guid;
            _function = MethodDelegateHelper.GetDelegateForMethod(methodInfo);
            InEdges = dto.InputWires;
            
            _hasReturnValue = methodInfo.ReturnType != typeof(void);
            _isStatic = methodInfo.IsStatic;
            _parameters = methodInfo.GetParameters();
            _parameterValues = new object[_parameters.Length + (_isStatic ? 0 : 1)];
            
            InPortValues = new Dictionary<string, object>(dto.InputPinValues.Count);
            foreach (var (key, tuple) in dto.InputPinValues)
            {
                var val = TypeUtility.CastToType(tuple.Item2, tuple.Item1);
                InPortValues[key] = val;
            }

            OutPortValues = new Dictionary<string, object>(dto.OutputPinNames.Count);
            foreach (var outPort in dto.OutputPinNames)
            {
                OutPortValues[outPort] = null;
            }

            if (dto.Properties.TryGetValue(NEXT_NODE_GUID, out var nextNodeGuid))
            {
                _nextNodeGuid = nextNodeGuid as string;
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
            foreach (var edge in InEdges)
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

    public class BlueprintFieldSetterNode : BlueprintBaseNode
    {
        private readonly Delegate _function;
        private readonly bool _isStatic;
        private readonly string _valuePinName;

        private readonly string _nextNodeGuid;
        private BlueprintBaseNode _nextNode;
        
        public BlueprintFieldSetterNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            _function = FieldDelegateHelper.GetDelegateForFieldSetter(dataModel.FieldInfo);
            _isStatic = dataModel.FieldInfo.IsStatic;
            InEdges = dataModel.InEdges;

            InPortValues = new Dictionary<string, object>(dataModel.InPorts.Count);
            foreach (var inPort in dataModel.InPorts.Values)
            {
                if (!inPort.HasInlineValue)
                {
                    continue;
                }

                _valuePinName = inPort.PortName;
                InPortValues[inPort.PortName] = inPort.GetContent();
            }
            
            var outEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePin.PinName == "OUT");
            if (outEdge.RightSidePin.IsValid())
            {
                _nextNodeGuid = outEdge.RightSidePin.NodeGuid;
            }
        }

        public BlueprintFieldSetterNode(BlueprintCompiledNodeDto dto, FieldInfo fieldInfo)
        {
            Guid = dto.Guid;
            _function = FieldDelegateHelper.GetDelegateForFieldSetter(fieldInfo);
            _isStatic = fieldInfo.IsStatic;
            InEdges = dto.InputWires;

            InPortValues = new Dictionary<string, object>(dto.InputPinValues.Count);
            foreach (var (key, tuple) in dto.InputPinValues)
            {
                var val = TypeUtility.CastToType(tuple.Item2, tuple.Item1);
                _valuePinName = key;
                InPortValues[key] = val;
            }

            OutPortValues = new Dictionary<string, object>(dto.OutputPinNames.Count);
            foreach (var outPort in dto.OutputPinNames)
            {
                OutPortValues[outPort] = null;
            }

            if (dto.Properties.TryGetValue(NEXT_NODE_GUID, out var nextNodeGuid))
            {
                _nextNodeGuid = nextNodeGuid as string;
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
            foreach (var edge in InEdges)
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

            if (_isStatic)
            {
                _function.DynamicInvoke(InPortValues[_valuePinName]);
            }
            else
            {
                _function.DynamicInvoke(InPortValues[PinNames.OWNER], InPortValues[_valuePinName]);
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