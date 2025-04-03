using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vapor.Blueprints
{
    public static class SearchModelParams
    {
        public const string NODE_TYPE_PARAM = "NodeType";
        
        public const string METHOD_INFO_PARAM = "MethodInfo";
        
        public const string FIELD_INFO_PARAM = "FieldInfo";
        public const string VARIABLE_NAME_PARAM = "VariableName";
        public const string VARIABLE_ACCESS_PARAM = "VariableAccess";
        public const string VARIABLE_SCOPE_PARAM = "VariableScope";
        
        public const string DATA_TYPE_PARAM = "DataType";
        
        // Dynamic Fields
        public const string GRAPH_PARAM = "Graph";
        public const string PORT_PARAM = "Port";

        // BlueprintNodeController CreateDesignNode(Vector2 position, List<(string, object)> parameters);
        // void UpdateDesignNode(BlueprintNodeController nodeController);
        // // BlueprintCompiledNodeDto Compile(BlueprintDesignNode node);
        // BlueprintBaseNode Decompile(BlueprintDesignNodeDto dto);
    }

    // internal static class NodeTypeExtensions
    // {
    //     internal static T FindParam<T>(this INodeType source, List<ValueTuple<string, object>> parameters, string paramName)
    //     {
    //         return (T)parameters.First(p => p.Item1 == paramName).Item2;
    //     }
    // }


    // Not Implemented Yet
    // public struct SequenceNodeType : INodeType
    // {
    //     public BlueprintNodeController CreateDesignNode(Vector2 position, List<(string, object)> parameters)
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     public void UpdateDesignNode(BlueprintNodeController nodeController)
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     // public BlueprintCompiledNodeDto Compile(BlueprintDesignNode node)
    //     // {
    //     //     throw new NotImplementedException();
    //     // }
    //
    //     public BlueprintBaseNode Decompile(BlueprintDesignNodeDto dto)
    //     {
    //         throw new NotImplementedException();
    //     }
    // }
    //
    // public struct SwitchNodeType : INodeType
    // {
    //     public BlueprintNodeController CreateDesignNode(Vector2 position, List<(string, object)> parameters)
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     public void UpdateDesignNode(BlueprintNodeController nodeController)
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     // public BlueprintCompiledNodeDto Compile(BlueprintDesignNode node)
    //     // {
    //     //     throw new NotImplementedException();
    //     // }
    //
    //     public BlueprintBaseNode Decompile(BlueprintDesignNodeDto dto)
    //     {
    //         throw new NotImplementedException();
    //     }
    // }
    //
    // public struct WhileNodeType : INodeType
    // {
    //     public BlueprintNodeController CreateDesignNode(Vector2 position, List<(string, object)> parameters)
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     public void UpdateDesignNode(BlueprintNodeController nodeController)
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     // public BlueprintCompiledNodeDto Compile(BlueprintDesignNode node)
    //     // {
    //     //     throw new NotImplementedException();
    //     // }
    //
    //     public BlueprintBaseNode Decompile(BlueprintDesignNodeDto dto)
    //     {
    //         throw new NotImplementedException();
    //     }
    // }
    //
    // public struct ForNodeType : INodeType
    // {
    //     public BlueprintNodeController CreateDesignNode(Vector2 position, List<(string, object)> parameters)
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     public void UpdateDesignNode(BlueprintNodeController nodeController)
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     // public BlueprintCompiledNodeDto Compile(BlueprintDesignNode node)
    //     // {
    //     //     throw new NotImplementedException();
    //     // }
    //
    //     public BlueprintBaseNode Decompile(BlueprintDesignNodeDto dto)
    //     {
    //         throw new NotImplementedException();
    //     }
    // }
}
