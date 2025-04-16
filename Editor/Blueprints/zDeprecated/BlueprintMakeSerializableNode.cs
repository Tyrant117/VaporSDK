using System.Collections.Generic;
using UnityEngine;

namespace Vapor.Blueprints
{
    // public class BlueprintMakeSerializableNode : BlueprintBaseNode
    // {
    //     public BlueprintMakeSerializableNode(BlueprintDesignNodeDto dto)
    //     {
    //         Guid = dto.Guid;
    //         
    //         
    //         InPortValues = new Dictionary<string, object>(dto.InputPins.Count);
    //         foreach (var pin in dto.InputPins)
    //         {
    //             var val = TypeUtility.CastToType(pin.Content, pin.PinType);
    //             InPortValues[pin.PinName] = val;
    //         }
    //
    //         // OutPortValues = new Dictionary<string, object>(dto.OutputWires.Count);
    //         // foreach (var outPort in dto.OutputWires)
    //         // {
    //             // OutPortValues[outPort.LeftSidePin.PinName] = null;
    //         // }
    //     }
    //
    //     public override void Init(PlayModeClass playModeClass, PlayModeMethod playModeMethod)
    //     {
    //         Method = playModeClass;
    //     }
    //
    //     protected override void CacheInputValues()
    //     {
    //         
    //     }
    //
    //     protected override void WriteOutputValues()
    //     {
    //         if (OutPortValues.ContainsKey(PinNames.RETURN))
    //         {
    //             OutPortValues[PinNames.RETURN] = InPortValues[PinNames.IGNORE];
    //         }
    //         else
    //         {
    //             Debug.LogError($"Failed to get output value for {PinNames.RETURN}");
    //         }
    //     }
    //
    //     protected override void Continue()
    //     {
    //         
    //     }
    // }
}