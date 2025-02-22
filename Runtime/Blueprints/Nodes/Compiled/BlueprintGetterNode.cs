using System.Collections.Generic;
using UnityEngine;

namespace Vapor.Blueprints
{
    public class BlueprintGetterNode : BlueprintBaseNode
    {
        private readonly string _tempFieldName;
        public BlueprintGetterNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            _tempFieldName = dataModel.MethodName;
            
            OutPortValues = new Dictionary<string, object>(dataModel.OutPorts.Count);
            foreach (var outPort in dataModel.OutPorts.Values)
            {
                if (!outPort.IsExecutePin)
                {
                    OutPortValues[outPort.PortName] = null;
                }
            }
        }

        public BlueprintGetterNode(BlueprintCompiledNodeDto dto, string tempFieldName)
        {
            Guid = dto.Guid;
            _tempFieldName = tempFieldName;
            
            OutPortValues = new Dictionary<string, object>(dto.OutputPinNames.Count);
            foreach (var outPort in dto.OutputPinNames)
            {
                OutPortValues[outPort] = null;
            }
        }

        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
        }

        protected override void CacheInputValues()
        {
        }

        protected override void WriteOutputValues()
        {
            Graph.TryGetTempValue(_tempFieldName, out var temp);
            if (OutPortValues.ContainsKey(_tempFieldName))
            {
                OutPortValues[_tempFieldName] = temp;
            }
            else
            {
                Debug.LogError($"Failed to get output value for {_tempFieldName}");
            }
        }

        protected override void Continue()
        {
        }
    }
}