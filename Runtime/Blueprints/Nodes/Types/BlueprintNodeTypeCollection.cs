using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vapor.Blueprints
{
    public interface INodeType
    {
        public const string NAME_DATA_PARAM = "NameData";
        public const string METHOD_INFO_PARAM = "MethodInfo";
        public const string FIELD_INFO_PARAM = "FieldInfo";
        public const string CONNECTION_TYPE_PARAM = "ConnectionType";
        public const string KEY_DATA_PARAM = "KeyData";
        
        // Dynamic Fields
        public const string GRAPH_PARAM = "Graph";
        public const string PORT_PARAM = "Port";

        BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters);
        void UpdateDesignNode(BlueprintDesignNode node);
        BlueprintCompiledNodeDto Compile(BlueprintDesignNode node);
        BlueprintBaseNode Decompile(BlueprintCompiledNodeDto dto);
    }

    internal static class NodeTypeExtensions
    {
        internal static T FindParam<T>(this INodeType source, List<ValueTuple<string, object>> parameters, string paramName)
        {
            return (T)parameters.First(p => p.Item1 == paramName).Item2;
        }
    }


    // Not Implemented Yet
    public struct SequenceNodeType : INodeType
    {
        public BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters)
        {
            throw new NotImplementedException();
        }

        public void UpdateDesignNode(BlueprintDesignNode node)
        {
            throw new NotImplementedException();
        }

        public BlueprintCompiledNodeDto Compile(BlueprintDesignNode node)
        {
            throw new NotImplementedException();
        }

        public BlueprintBaseNode Decompile(BlueprintCompiledNodeDto dto)
        {
            throw new NotImplementedException();
        }
    }
    
    public struct SwitchNodeType : INodeType
    {
        public BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters)
        {
            throw new NotImplementedException();
        }

        public void UpdateDesignNode(BlueprintDesignNode node)
        {
            throw new NotImplementedException();
        }

        public BlueprintCompiledNodeDto Compile(BlueprintDesignNode node)
        {
            throw new NotImplementedException();
        }

        public BlueprintBaseNode Decompile(BlueprintCompiledNodeDto dto)
        {
            throw new NotImplementedException();
        }
    }

    public struct WhileNodeType : INodeType
    {
        public BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters)
        {
            throw new NotImplementedException();
        }

        public void UpdateDesignNode(BlueprintDesignNode node)
        {
            throw new NotImplementedException();
        }

        public BlueprintCompiledNodeDto Compile(BlueprintDesignNode node)
        {
            throw new NotImplementedException();
        }

        public BlueprintBaseNode Decompile(BlueprintCompiledNodeDto dto)
        {
            throw new NotImplementedException();
        }
    }

    public struct ForNodeType : INodeType
    {
        public BlueprintDesignNode CreateDesignNode(Vector2 position, List<(string, object)> parameters)
        {
            throw new NotImplementedException();
        }

        public void UpdateDesignNode(BlueprintDesignNode node)
        {
            throw new NotImplementedException();
        }

        public BlueprintCompiledNodeDto Compile(BlueprintDesignNode node)
        {
            throw new NotImplementedException();
        }

        public BlueprintBaseNode Decompile(BlueprintCompiledNodeDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
