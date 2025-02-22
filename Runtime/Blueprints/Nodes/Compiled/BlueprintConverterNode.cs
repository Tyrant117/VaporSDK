using System;
using System.Collections.Generic;
using System.Reflection;

namespace Vapor.Blueprints
{
    public class BlueprintConverterNode : BlueprintBaseNode
    {
        private readonly Delegate _function;
        private readonly object[] _parameterValues;
        
        public BlueprintConverterNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            _function = MethodDelegateHelper.GetDelegateForMethod(dataModel.MethodInfo);
            InEdges = dataModel.InEdges;
            
            _parameterValues = new object[1];
            InPortValues = new Dictionary<string, object>(1);
            OutPortValues = new Dictionary<string, object>(1);
            InPortValues[PinNames.EXECUTE_IN] = null;
            OutPortValues[PinNames.RETURN] = null;
        }

        public BlueprintConverterNode(BlueprintCompiledNodeDto dto, MethodInfo methodInfo)
        {
            Guid = dto.Guid;
            _function = MethodDelegateHelper.GetDelegateForMethod(methodInfo);
            InEdges = dto.InputWires;
            
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

            _parameterValues[0] = InPortValues[PinNames.EXECUTE_IN];
            OutPortValues[PinNames.RETURN] = _function.DynamicInvoke(_parameterValues);
        }

        protected override void Continue()
        {
        }
    }
}