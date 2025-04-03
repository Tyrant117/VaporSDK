using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor;
using Vapor.Blueprints;
using Vapor.Inspector;

namespace VaporEditor.Blueprints
{
    public abstract class NodeModelBase
    {
        public string Guid { get; }
        public uint Uuid { get; }
        public NodeType NodeType { get; }
        public Rect Position { get; set; }
        public List<BlueprintWireReference> InputWires { get; }
        public List<BlueprintWireReference> OutputWires { get; }

        protected NodeModelBase(NodeType nodeType, Vector2 position)
        {
            Guid = System.Guid.NewGuid().ToString();
            Uuid = Guid.GetStableHashU32();
            NodeType = nodeType;
            Position = new Rect(position, Vector2.zero);
            InputWires = new List<BlueprintWireReference>();
            OutputWires = new List<BlueprintWireReference>();
        }

        protected NodeModelBase(BlueprintDesignNodeDto dto)
        {
            Guid = dto.Guid;
            Uuid = Guid.GetStableHashU32();
            NodeType = dto.NodeEnumType;
            Position = dto.Position;
            InputWires = new List<BlueprintWireReference>(dto.InputWires);
            OutputWires = new List<BlueprintWireReference>(dto.OutputWires);
        }

        public virtual BlueprintDesignNodeDto Serialize()
        {
            var dto = new BlueprintDesignNodeDto
            {
                Guid = Guid,
                NodeEnumType = NodeType,
                Position = Position,
                InputWires = InputWires,
                OutputWires = OutputWires,
                InputPins = new List<BlueprintPinDto>(),
                OutputPins = new List<BlueprintPinDto>(),
                Properties = new Dictionary<string, (Type, object)>(),
            };
            return dto;
        }
    }

    public class NodeModel : NodeModelBase
    {
        public NodeModel(NodeType nodeType, Vector2 position) : base(nodeType, position)
        {
        }

        public NodeModel(BlueprintDesignNodeDto dto) : base(dto)
        {
        }
    }
    
    public class MethodNodeModel : NodeModelBase
    {
        // Serialized
        public Type MethodType { get; }
        public string MethodName { get; }
        public string[] MethodParameters { get; }
        
        // Non-Serialized
        public MethodInfo MethodInfo { get; }

        public bool IsPure
        {
            get
            {
                if (MethodInfo.IsStatic && MethodInfo.ReturnType != typeof(void))
                {
                    return true;
                }

                return MethodInfo.IsDefined(typeof(BlueprintPureAttribute), false);
            }
        }

        public MethodNodeModel(NodeType nodeType, Vector2 position, MethodInfo methodInfo) : base(nodeType, position)
        {
            MethodType = methodInfo.DeclaringType;
            MethodName = methodInfo.Name;
            MethodParameters = methodInfo.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName ?? p.ParameterType.Name).ToArray();
            
            MethodInfo = methodInfo;
        }
        
        public MethodNodeModel(BlueprintDesignNodeDto dto) : base(dto)
        {
            if(dto.Properties.TryGetValue(NodePropertyNames.K_METHOD_DECLARING_TYPE, out var val))
            {
                MethodType = (Type)TypeUtility.CastToType(val.Item2, val.Item1);
            }
            if(dto.Properties.TryGetValue(NodePropertyNames.K_METHOD_NAME, out val))
            {
                MethodName = (string)TypeUtility.CastToType(val.Item2, val.Item1);
            }
            if(dto.Properties.TryGetValue(NodePropertyNames.K_METHOD_PARAMETER_TYPES, out val))
            {
                MethodParameters = (string[])TypeUtility.CastToType(val.Item2, val.Item1);
            }

            MethodInfo = RuntimeReflectionUtility.GetMethodInfo(MethodType, MethodName, MethodParameters);
        }


        public override BlueprintDesignNodeDto Serialize()
        {
            var dto = base.Serialize();
            dto.Properties.TryAdd(NodePropertyNames.K_METHOD_DECLARING_TYPE, (typeof(Type), MethodType));
            dto.Properties.TryAdd(NodePropertyNames.K_METHOD_NAME, (typeof(string), MethodName));
            dto.Properties.TryAdd(NodePropertyNames.K_METHOD_PARAMETER_TYPES, (typeof(string[]), MethodParameters));
            return dto;
        }
    }

    public class MemberNodeModel : NodeModelBase
    {
        // Serialized
        public Type FieldDeclaringType { get; }
        public string FieldName { get; }
        public string VariableName { get; set; }
        public VariableAccessType VariableAccess { get; }
        public VariableScopeType VariableScope { get; }
        
        // Non-Serialized
        public FieldInfo FieldInfo { get; }
        
        public MemberNodeModel(NodeType nodeType, Vector2 position, FieldInfo fieldInfo, VariableAccessType variableAccess) : base(nodeType, position)
        {
            FieldInfo = fieldInfo;
            FieldDeclaringType = FieldInfo.DeclaringType;
            FieldName = FieldInfo.Name;
            VariableName = FieldInfo.Name;
            VariableAccess = variableAccess;
            VariableScope = VariableScopeType.Class;
        }

        public MemberNodeModel(NodeType nodeType, Vector2 position, string variableName, VariableAccessType variableAccess, VariableScopeType variableScope) : base(nodeType, position)
        {
            VariableName = variableName;
            VariableAccess = variableAccess;
            VariableScope = variableScope;
        }
        
        public MemberNodeModel(BlueprintDesignNodeDto dto) : base(dto)
        {
            if(dto.Properties.TryGetValue(NodePropertyNames.VARIABLE_NAME, out var val))
            {
                VariableName = (string)TypeUtility.CastToType(val.Item2, val.Item1);
            }
            if(dto.Properties.TryGetValue(NodePropertyNames.VARIABLE_ACCESS, out val))
            {
                VariableAccess = (VariableAccessType)TypeUtility.CastToType(val.Item2, val.Item1);
            }
            if(dto.Properties.TryGetValue(NodePropertyNames.VARIABLE_SCOPE, out val))
            {
                VariableScope = (VariableScopeType)TypeUtility.CastToType(val.Item2, val.Item1);
            }
            
            if(dto.Properties.TryGetValue(NodePropertyNames.FIELD_TYPE, out val))
            {
                FieldDeclaringType = (Type)TypeUtility.CastToType(val.Item2, val.Item1);
            }
            if(dto.Properties.TryGetValue(NodePropertyNames.FIELD_NAME, out val))
            {
                FieldName = (string)TypeUtility.CastToType(val.Item2, val.Item1);
            }

            if (FieldDeclaringType != null && !FieldName.EmptyOrNull())
            {
                FieldInfo = RuntimeReflectionUtility.GetFieldInfo(FieldDeclaringType, FieldName);
            }
        }

        public override BlueprintDesignNodeDto Serialize()
        {
            var dto = base.Serialize();
            dto.Properties.TryAdd(NodePropertyNames.VARIABLE_NAME, (typeof(string), VariableName));
            dto.Properties.TryAdd(NodePropertyNames.VARIABLE_ACCESS, (typeof(VariableAccessType), VariableAccess));
            dto.Properties.TryAdd(NodePropertyNames.VARIABLE_SCOPE, (typeof(VariableScopeType), VariableScope));

            if (FieldInfo != null)
            {
                dto.Properties.TryAdd(NodePropertyNames.FIELD_TYPE, (typeof(Type), FieldDeclaringType));
                dto.Properties.TryAdd(NodePropertyNames.FIELD_NAME, (typeof(string), FieldName));
            }
            return dto;
        }
    }

    public class DataNodeModel<T> : NodeModelBase
    {
        // Serialized
        public T Data { get; set; }
        
        public DataNodeModel(NodeType nodeType, Vector2 position, T data) : base(nodeType, position)
        {
            Data = data;
        }
        
        public DataNodeModel(BlueprintDesignNodeDto dto) : base(dto)
        {
            if(dto.Properties.TryGetValue(NodePropertyNames.DATA_VALUE, out var val))
            {
                Data = (T)TypeUtility.CastToType(val.Item2, val.Item1);
            }
        }

        public override BlueprintDesignNodeDto Serialize()
        {
            var dto = base.Serialize();
            dto.Properties.TryAdd(NodePropertyNames.DATA_VALUE, (typeof(T), Data));
            return dto;
        }
    }
}
