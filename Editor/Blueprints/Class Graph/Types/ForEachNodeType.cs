using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vapor.Blueprints
{
    // public struct ForEachNodeType : INodeType
    // {
    //     public BlueprintNodeController CreateDesignNode(Vector2 position, List<(string, object)> parameters)
    //     {
    //         var node = new BlueprintNodeController(this)
    //         {
    //             NodeName = "ForEach",
    //             Position = new Rect(position, Vector2.zero)
    //         };
    //         UpdateDesignNode(node);
    //         return node;
    //     }
    //     public void UpdateDesignNode(BlueprintNodeController nodeController)
    //     {
    //         var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
    //             .WithDisplayName(string.Empty)
    //             .WithAllowMultipleWires();
    //         nodeController.InPorts.Add(PinNames.EXECUTE_IN, inSlot);
    //
    //         var breakSlot = new BlueprintPin(PinNames.BREAK_IN, PinDirection.In, typeof(ExecutePin), false)
    //             .WithDisplayName("Break")
    //             .WithAllowMultipleWires();
    //         nodeController.InPorts.Add(PinNames.BREAK_IN, breakSlot);
    //
    //         var arraySlot = new BlueprintPin(PinNames.ARRAY_IN, PinDirection.In, typeof(object), false)
    //             .WithDisplayName("Array");
    //         nodeController.InPorts.Add(PinNames.ARRAY_IN, arraySlot);
    //
    //         var loopSlot = new BlueprintPin(PinNames.LOOP_OUT, PinDirection.Out, typeof(ExecutePin), false)
    //             .WithDisplayName("Loop");
    //         nodeController.OutPorts.Add(PinNames.LOOP_OUT, loopSlot);
    //
    //         var indexSlot = new BlueprintPin(PinNames.INDEX_OUT, PinDirection.Out, typeof(int), false)
    //             .WithDisplayName("Index");
    //         nodeController.OutPorts.Add(PinNames.INDEX_OUT, indexSlot);
    //
    //         var elementSlot = new BlueprintPin(PinNames.ELEMENT_OUT, PinDirection.Out, typeof(object), false)
    //             .WithDisplayName("Element");
    //         nodeController.OutPorts.Add(PinNames.ELEMENT_OUT, elementSlot);
    //
    //         var completedSlot = new BlueprintPin(PinNames.COMPLETE_OUT, PinDirection.Out, typeof(ExecutePin), false)
    //             .WithDisplayName("Complete");
    //         nodeController.OutPorts.Add(PinNames.COMPLETE_OUT, completedSlot);
    //     }
    //
    //     public BlueprintBaseNode Decompile(BlueprintDesignNodeDto dto)
    //     {
    //         return new BlueprintForEachNode(dto);
    //     }
    // }
}