using System;
using System.Collections.Generic;
using System.Reflection;

namespace Vapor.Blueprints
{
    public class BlueprintFieldGetterNode : BlueprintBaseNode
    {
        private readonly Delegate _function;
        private readonly bool _isStatic;

        public BlueprintFieldGetterNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            _function = FieldDelegateHelper.GetDelegateForFieldGetter(dataModel.FieldInfo);
            _isStatic = dataModel.FieldInfo.IsStatic;
            InEdges = dataModel.InEdges;
            
            InPortValues = new Dictionary<string, object>(dataModel.InPorts.Count)
            {
                [PinNames.OWNER] = null
            };
            OutPortValues = new Dictionary<string, object>(dataModel.OutPorts.Count)
            {
                [PinNames.RETURN] = null
            };
        }

        public BlueprintFieldGetterNode(BlueprintCompiledNodeDto dto, FieldInfo fieldInfo)
        {
            Guid = dto.Guid;
            _function = FieldDelegateHelper.GetDelegateForFieldGetter(fieldInfo);
            _isStatic = fieldInfo.IsStatic;
            InEdges = dto.InputWires;
            
            InPortValues = new Dictionary<string, object>(dto.InputPinValues.Count)
            {
                [PinNames.OWNER] = null
            };
            OutPortValues = new Dictionary<string, object>(dto.OutputPinNames.Count)
            {
                [PinNames.RETURN] = null
            };
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

            if (_isStatic)
            {
                OutPortValues[PinNames.RETURN] = _function.DynamicInvoke();
            }
            else
            {
                OutPortValues[PinNames.RETURN] = _function.DynamicInvoke(InPortValues[PinNames.OWNER]);
            }
        }

        protected override void Continue()
        {
        }
    }
}