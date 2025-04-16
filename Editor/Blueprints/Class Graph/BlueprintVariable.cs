using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Codice.Client.BaseCommands.TubeClient;
using UnityEngine;
using Vapor;
using Vapor.Blueprints;
using Vapor.Inspector;
using VaporEditor;
using VaporEditor.Blueprints;
using VaporEditor.Inspector;

namespace VaporEditor.Blueprints
{
    public class BlueprintVariable : IBlueprintGraphModel
    {
        public enum ChangeType
        {
            Name,
            Type,
            Delete,
        }

        public BlueprintClassGraphModel ClassGraphModel { get; private set; }
        public BlueprintMethodGraph MethodGraph { get; private set; }
        private string _displayName;
        private Type _type;

        public string Id { get; }

        public string VariableName { get; private set; }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                string oldName = _displayName;
                _displayName = value;
                VariableName = _displayName.Trim().Replace(" ", "");
                Updated(ChangeType.Name);
            }
        }
        public Type Type
        {
            get => _type;
            set
            {
                Type oldType = _type;
                _type = value;
                DefaultValue = _type.IsClass ? _type.IsSerializable ? FormatterServices.GetUninitializedObject(_type) : null : Activator.CreateInstance(_type);
                Updated(ChangeType.Type);
            }
        }
        public VariableScopeType Scope { get; }
        public VariableAccessModifier AccessModifier { get; set; }
        public bool IsProperty { get; set; }

        public object DefaultValue { get; set; }
        public string ConstructorName { get; set; }
        public List<object> ParameterValues { get; set; } = new();

        public event Action<BlueprintVariable, ChangeType> Changed;

        public BlueprintVariable(string displayName, Type type, VariableScopeType scope, bool isProperty = false)
        {
            Id = Guid.NewGuid().ToString();
            _displayName = displayName;
            VariableName = _displayName.Trim().Replace(" ", "");
            _type = type;
            Scope = scope;
            IsProperty = isProperty;
            ConstructorName = "Default(T)";
            DefaultValue = type.IsClass ? type.IsSerializable ? FormatterServices.GetUninitializedObject(type) : null : Activator.CreateInstance(type);
            ParameterValues.Add(DefaultValue);
        }

        public BlueprintVariable(BlueprintVariableDto dto)
        {
            Id = dto.Id;
            VariableName = dto.VariableName;
            _displayName = dto.DisplayName;
            _type = dto.Type;
            Scope = dto.Scope;
            AccessModifier = dto.AccessModifier;
            IsProperty = dto.IsProperty;
            ConstructorName = dto.ConstructorName;
            if (!ConstructorName.EmptyOrNull() && !ConstructorName.Equals("Default(T)"))
            {
                if (dto.DefaultParametersValue != null)
                {
                    var convertedTypes = dto.DefaultParametersValue.Select(t => (t.Item1, TypeUtility.CastToType(t.Item2, t.Item1))).ToArray();
                    var paramObjs = convertedTypes.Select(t => t.Item2).ToArray();
                    ParameterValues.AddRange(paramObjs);
                }
                
                var constructor = BlueprintEditorUtility.GetConstructor(_type, ConstructorName);
                DefaultValue = constructor.Invoke(ParameterValues.ToArray());
            }
            else
            {
                if (dto.DefaultParametersValue is { Count: 1 })
                {
                    var converted = TypeUtility.CastToType(dto.DefaultParametersValue[0].Item2, dto.DefaultParametersValue[0].Item1);
                    ParameterValues.Add(converted);
                    DefaultValue = converted;
                }
                else
                {
                    DefaultValue = ParameterValues.Count == 1
                        ? ParameterValues[0]
                        : _type.IsClass
                            ? _type.IsSerializable
                                ? FormatterServices.GetUninitializedObject(_type)
                                : null
                            : Activator.CreateInstance(_type);
                }
            }
        }

        public BlueprintVariable WithClassGraph(BlueprintClassGraphModel graphModel)
        {
            ClassGraphModel = graphModel;
            return this;
        }

        public BlueprintVariable WithMethodGraph(BlueprintMethodGraph graph)
        {
            MethodGraph = graph;
            return this;
        }

        public BlueprintVariableDto Serialize()
        {
            return new BlueprintVariableDto
            {
                Id = Id,
                VariableName = VariableName,
                DisplayName = DisplayName,
                Type = Type,
                Scope = Scope,
                AccessModifier = AccessModifier,
                IsProperty = IsProperty,
                ConstructorName = ConstructorName,
                DefaultParametersValue = ParameterValues.Select(p => (p.GetType(), p)).ToList()
            };
        }

        private void Updated(ChangeType changeType)
        {
            Debug.Log("Changing Variable!");
            Changed?.Invoke(this, changeType);
            if (Scope == VariableScopeType.Class)
            {
                ClassGraphModel.OnVariableUpdated(this);
            }
            else
            {
                MethodGraph.OnVariableUpdated(this);
            }
        }
        
        public void Delete()
        {
            Changed?.Invoke(this, ChangeType.Delete);
            if (Scope == VariableScopeType.Class)
            {
                ClassGraphModel.RemoveVariable(this);
            }
            else
            {
                MethodGraph.RemoveVariable(this);
            }
        }

        public MemberSearchData GetMemberSearchData(VariableAccessType accessType)
        {
            return new MemberSearchData
            {
                Id = Id,
                AccessType = accessType,
                ScopeType = Scope,
            };
        }
    }

    public class BlueprintArgument : IBlueprintGraphModel
    {
        public enum ChangeType
        {
            Name,
            Type,
        }
        
        public int ParameterIndex { get; set; }
        public bool IsRef { get; set; }
        
        public bool IsOut { get; set; }
        public bool IsReturn { get; set; }
        
        public Type Type { get; private set; }
        public string ArgumentName { get; private set; }
        public string DisplayName { get; private set; }
        public BlueprintMethodGraph Method { get; }
        
        public event Action<BlueprintArgument, ChangeType> Changed;

        public BlueprintArgument(BlueprintMethodGraph method, Type type, string name, int parameterIndex, bool isRef, bool isOut, bool isReturn)
        {
            Method = method;
            Type = type;
            ArgumentName = name;
            DisplayName = name;
            ParameterIndex = parameterIndex;
            IsRef = isRef;
            IsOut = isOut;
            IsReturn = isReturn;
        }

        public BlueprintArgument(BlueprintMethodGraph method, BlueprintArgumentDto dto) : this(method, dto.Type, dto.DisplayName, dto.ParameterIndex, dto.IsRef, dto.IsOut, dto.IsReturn)
        {
            ArgumentName = dto.ParameterName;
        }

        public void SetName(string newName)
        {
            DisplayName = newName;
            Changed?.Invoke(this, ChangeType.Name);
            Method.OnArgumentUpdated(this);
        }
        
        public void SetType(Type newType)
        {
            Type = newType;
            Changed?.Invoke(this, ChangeType.Type);
            Method.OnArgumentUpdated(this);
        }
        
        public BlueprintArgumentDto Serialize()
        {
            return new BlueprintArgumentDto
            {
                Type = Type,
                ParameterName = ArgumentName,
                DisplayName = DisplayName,
                ParameterIndex = ParameterIndex,
                IsRef = IsRef,
                IsOut = IsOut,
                IsReturn = IsReturn,
            };
        }
    }
}