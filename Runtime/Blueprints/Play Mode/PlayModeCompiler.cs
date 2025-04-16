using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vapor.Blueprints
{
    public static class PlayModeCompiler
    {
        public static PlayModeNodeBase Compile(BlueprintDesignNodeDto dto, List<BlueprintWireDto> wires)
        {
            switch (dto.NodeEnumType)
            {
                case NodeType.Entry:
                    return new PlayModeEntryNode(dto, wires);
                case NodeType.Method:
                    return new PlayModeMethodNode(dto, wires);
                case NodeType.MemberAccess:
                    return new PlayModeMemberAccessNode(dto, wires);
                case NodeType.Return:
                    return new PlayModeReturnNode(dto, wires);
                case NodeType.Branch:
                    return new PlayModeIfElseNode(dto, wires);
                case NodeType.Switch:
                    return new PlayModeSwitchNode(dto, wires);
                case NodeType.Sequence:
                    return new PlayModeSequenceNode(dto, wires);
                case NodeType.For:
                    return new PlayModeForNode(dto, wires);
                case NodeType.ForEach:
                    return new PlayModeForEachNode(dto, wires);
                case NodeType.While:
                    return new PlayModeWhileNode(dto, wires);
                case NodeType.Break:
                    return new PlayModeBreakNode(dto, wires);
                case NodeType.Continue:
                    return new PlayModeContinueNode(dto, wires);
                case NodeType.Conversion:
                    return new PlayModeConverterNode(dto, wires);
                case NodeType.Cast:
                    return new PlayModeCastNode(dto, wires);
                case NodeType.Redirect:
                    return new PlayModeRedirectNode(dto, wires);
                case NodeType.Constructor:
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            throw new NotImplementedException(dto.NodeEnumType.ToString());
        }
    }
}
