using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vapor.Blueprints
{
    // public struct MakeSerializableNodeType : INodeType
    // {
    //     public BlueprintNodeController CreateDesignNode(Vector2 position, List<(string, object)> parameters)
    //     {
    //         var type = this.FindParam<Type>(parameters, INodeType.CONNECTION_TYPE_PARAM);
    //         var node = new BlueprintNodeController(this)
    //         {
    //             Position = new Rect(position, Vector2.zero)
    //         };
    //         node.AddOrUpdateProperty(NodePropertyNames.CONNECTION_TYPE, type, true);
    //         UpdateDesignNode(node);
    //         return node;
    //     }
    //
    //     public void UpdateDesignNode(BlueprintNodeController nodeController)
    //     {
    //         nodeController.TryGetProperty<Type>(NodePropertyNames.CONNECTION_TYPE, out var makeType);
    //         
    //         nodeController.NodeName = $"Make <b><i>{makeType.Name}</i></b>";
    //         
    //         var inData = new BlueprintPin(PinNames.IGNORE, PinDirection.In, makeType, false)
    //             .WithDisplayName(string.Empty);
    //         nodeController.InPorts.Add(PinNames.IGNORE, inData);
    //
    //         var os = new BlueprintPin(PinNames.RETURN, PinDirection.Out, makeType, false)
    //             .WithDisplayName(string.Empty)
    //             .WithAllowMultipleWires();
    //         nodeController.OutPorts.Add(PinNames.RETURN, os);
    //     }
    //     
    //
    //     public BlueprintBaseNode Decompile(BlueprintDesignNodeDto dto)
    //     {
    //         return new BlueprintMakeSerializableNode(dto);
    //     }
    // }
}