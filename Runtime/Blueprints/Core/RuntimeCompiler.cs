using System;
using UnityEngine;

namespace Vapor.Blueprints
{
    public static class RuntimeCompiler
    {
        public static BlueprintBaseNode Compile(BlueprintDesignNodeDto dto)
        {
            switch (dto.NodeEnumType)
            {
                case NodeType.Entry:
                    return new BlueprintEntryNode(dto);
                case NodeType.Method:
                    return new BlueprintMethodNode(dto);
                case NodeType.MemberAccess:
                    return new BlueprintMemberAccessNode(dto);
                case NodeType.Return:
                    return new BlueprintReturnNode(dto);
                case NodeType.Branch:
                    return new BlueprintIfElseNode(dto);
                case NodeType.Switch:
                    break;
                case NodeType.Sequence:
                    break;
                case NodeType.For:
                    break;
                case NodeType.ForEach:
                    return new BlueprintForEachNode(dto);
                case NodeType.While:
                    break;
                case NodeType.Break:
                    break;
                case NodeType.Continue:
                    break;
                case NodeType.Conversion:
                    return new BlueprintConverterNode(dto);
                case NodeType.Cast:
                    break;
                case NodeType.Redirect:
                    return new BlueprintRedirectNode(dto);
                case NodeType.Inline:
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            throw new NotImplementedException(dto.NodeEnumType.ToString());
        }
    }
}
