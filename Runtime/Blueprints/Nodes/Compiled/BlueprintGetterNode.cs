using UnityEngine;

namespace Vapor.Blueprints
{
    [System.Obsolete]
    public class BlueprintGetterNode : BlueprintBaseNode
    {
        private readonly string _tempFieldName;

        public BlueprintGetterNode(BlueprintDesignNodeDto dto)
        {
            Guid = dto.Guid;
            if(dto.Properties.TryGetValue(NodePropertyNames.VARIABLE_NAME, out var tempFieldName))
            {
                _tempFieldName = (string)tempFieldName.Item2;
            }

            SetupOutputPins(dto);
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