using UnityEngine;

namespace Vapor.Blueprints
{
    // [System.Obsolete]
    // public class BlueprintGetterNode : BlueprintBaseNode
    // {
    //     private readonly string _tempFieldName;
    //
    //     public BlueprintGetterNode(BlueprintDesignNodeDto dto)
    //     {
    //         Guid = dto.Guid;
    //         Uuid = Guid.GetStableHashU32();
    //         if(dto.Properties.TryGetValue(NodePropertyNames.VARIABLE_ID, out var tempFieldName))
    //         {
    //             _tempFieldName = (string)tempFieldName.Item2;
    //         }
    //
    //         SetupOutputPins(dto);
    //     }
    //
    //     protected override void CacheInputValues()
    //     {
    //     }
    //
    //     protected override void WriteOutputValues()
    //     {
    //         Method.TryGetTempValue(_tempFieldName, out var temp);
    //         if (OutPortValues.ContainsKey(_tempFieldName))
    //         {
    //             OutPortValues[_tempFieldName] = temp;
    //         }
    //         else
    //         {
    //             Debug.LogError($"Failed to get output value for {_tempFieldName}");
    //         }
    //     }
    //
    //     protected override void Continue()
    //     {
    //     }
    // }
}