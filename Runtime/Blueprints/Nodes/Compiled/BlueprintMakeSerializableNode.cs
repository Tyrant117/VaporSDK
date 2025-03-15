using System.Collections.Generic;
using UnityEngine;

namespace Vapor.Blueprints
{
    public class BlueprintMakeSerializableNode : BlueprintBaseNode
    {
        public BlueprintMakeSerializableNode(BlueprintCompiledNodeDto dto)
        {
            Guid = dto.Guid;
            InEdges = dto.InputWires;
            
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
            if (OutPortValues.ContainsKey(PinNames.RETURN))
            {
                OutPortValues[PinNames.RETURN] = InPortValues[PinNames.IGNORE];
            }
            else
            {
                Debug.LogError($"Failed to get output value for {PinNames.RETURN}");
            }
        }

        protected override void Continue()
        {
            
        }
    }
}