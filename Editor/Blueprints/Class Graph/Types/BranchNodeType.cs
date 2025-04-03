using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vapor.Blueprints
{
    // public struct BranchNodeType : INodeType
    // {
    //     public BlueprintNodeController CreateDesignNode(Vector2 position, List<(string, object)> parameters)
    //     {
    //         var node = new BlueprintNodeController(this)
    //         {
    //             NodeName = "Branch",
    //             Position = new Rect(position, Vector2.zero)
    //         };
    //         UpdateDesignNode(node);
    //         return node;
    //     }
    //
    //     public void UpdateDesignNode(BlueprintNodeController nodeController)
    //     {
    //         var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
    //             .WithDisplayName("")
    //             .WithAllowMultipleWires();
    //         nodeController.InPorts.Add(PinNames.EXECUTE_IN, inSlot);
    //
    //         var slot = new BlueprintPin(PinNames.VALUE_IN, PinDirection.In, typeof(bool), false);
    //         nodeController.InPorts.Add(PinNames.VALUE_IN, slot);
    //
    //         var trueSlot = new BlueprintPin(PinNames.TRUE_OUT, PinDirection.Out, typeof(ExecutePin), false)
    //             .WithDisplayName("True");
    //         nodeController.OutPorts.Add(PinNames.TRUE_OUT, trueSlot);
    //
    //         var falseSlot = new BlueprintPin(PinNames.FALSE_OUT, PinDirection.Out, typeof(ExecutePin), false)
    //             .WithDisplayName("False");
    //         nodeController.OutPorts.Add(PinNames.FALSE_OUT, falseSlot);
    //     }
    //
    //     public BlueprintBaseNode Decompile(BlueprintDesignNodeDto dto)
    //     {
    //         return new BlueprintIfElseNode(dto);
    //     }
    // }
}