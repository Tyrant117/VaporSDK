using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using Vapor.Inspector;
using VaporEditor.Blueprints;
using VaporEditor.Inspector;

namespace Vapor.Blueprints
{
    [Serializable]
    public struct BlueprintVariableDto
    {
        public string Name;
        public Type Type;
        public VariableScopeType Scope;
        public VariableAccessModifier AccessModifier;
        
        // Variable Constructions
        public string ConstructorName;
        public List<(Type, object)> DefaultParametersValue;
    }

    [Serializable]
    public struct BlueprintArgumentDto
    {
        public Type Type;
        public string DisplayName;
        public int ParameterIndex;
        public bool IsRef;
        public bool IsReturn;
        public bool IsOut;
    }
    
    public class BlueprintVariable
    {
        public enum ChangeType
        {
            Name,
            Type,
            Delete,
        }
        
        private BlueprintClassGraphModel _classGraphModel;
        private BlueprintMethodGraph _methodGraph;
        private string _name;
        private Type _type;

        public string Name
        {
            get => _name;
            set
            {
                string oldName = _name;
                _name = value;
                Rename(oldName, _name);
            }
        }
        public Type Type
        {
            get => _type;
            set
            {
                Type oldType = _type;
                _type = value;
                Retype(oldType, _type);
            }
        }
        public VariableScopeType Scope { get; }
        public VariableAccessModifier AccessModifier { get; set; }

        public object DefaultValue { get; set; }
        public string ConstructorName { get; set; }
        public List<object> ParameterValues { get; set; } = new();

        public event Action<BlueprintVariable, ChangeType> Changed;

        public BlueprintVariable(string name, Type type, VariableScopeType scope)
        {
            _name = name;
            _type = type;
            Scope = scope;
            ConstructorName = "Default(T)";
            DefaultValue = type.IsClass ? type.IsSerializable ? FormatterServices.GetUninitializedObject(type) : null : Activator.CreateInstance(type);
            ParameterValues.Add(DefaultValue);
        }

        public BlueprintVariable(BlueprintVariableDto dto)
        {
            _name = dto.Name;
            _type = dto.Type;
            Scope = dto.Scope;
            AccessModifier = dto.AccessModifier;
            ConstructorName = dto.ConstructorName;
            if (!ConstructorName.EmptyOrNull() && !ConstructorName.Equals("Default(T)"))
            {
                if (dto.DefaultParametersValue != null)
                {
                    var convertedTypes = dto.DefaultParametersValue.Select(t => (t.Item1, TypeUtility.CastToType(t.Item2, t.Item1))).ToArray();
                    var paramObjs = convertedTypes.Select(t => t.Item2).ToArray();
                    ParameterValues.AddRange(paramObjs);
                }
                
                var constructor = GetConstructor(_type, ConstructorName);
                DefaultValue = constructor.Invoke(ParameterValues.ToArray());
            }
            else
            {
                if (dto.DefaultParametersValue is { Count: 1 })
                {
                    var converted = TypeUtility.CastToType(dto.DefaultParametersValue[0].Item2, dto.DefaultParametersValue[0].Item1);
                    ParameterValues.Add(converted);
                }
                
                DefaultValue = ParameterValues.Count == 1 
                    ? ParameterValues[0] 
                    : _type.IsClass 
                        ? _type.IsSerializable 
                            ? FormatterServices.GetUninitializedObject(_type) 
                            : null 
                        : Activator.CreateInstance(_type);
            }
        }

        public BlueprintVariable WithClassGraph(BlueprintClassGraphModel graphModel)
        {
            _classGraphModel = graphModel;
            return this;
        }

        public BlueprintVariable WithMethodGraph(BlueprintMethodGraph graph)
        {
            _methodGraph = graph;
            return this;
        }

        public BlueprintVariableDto Serialize()
        {
            return new BlueprintVariableDto
            {
                Name = Name,
                Type = Type,
                Scope = Scope,
                AccessModifier = AccessModifier,
                ConstructorName = ConstructorName,
                DefaultParametersValue = ParameterValues.Select(p => (p.GetType(), p)).ToList()
            };
        }

        private void Rename(string oldName, string newName)
        {
            Updated(ChangeType.Name);
            
            if (Scope != VariableScopeType.Class)
            {
                foreach (var n in _methodGraph.Nodes.Values)
                {
                    // Works for Getters and Setters
                    if (n.NodeType == NodeType.MemberAccess &&
                        ((MemberNodeModel)n).VariableName == oldName)
                    {
                        var model = (MemberNodeModel)n;
                        model.VariableName = newName;
                        n.SetName(model.VariableAccess == VariableAccessType.Get ? $"Get <b><i>{newName}</i></b>" : $"Set <b><i>{newName}</i></b>");
                        var edges = n.OutputWires.FindAll(e => e.LeftSidePin.PinName == oldName);
                        foreach (var edge in edges)
                        {
                            int idx = n.OutputWires.IndexOf(edge);
                            var oldPort = n.OutputWires[idx].LeftSidePin;
                            var newPort = new BlueprintPinReference(newName, oldPort.NodeGuid, oldPort.IsExecutePin);
                            var newEdge = new BlueprintWireReference(newPort, n.OutputWires[idx].RightSidePin);
                            n.OutputWires[idx] = newEdge;
                        }

                        var edgesIn = n.InputWires.FindAll(e => e.RightSidePin.PinName == oldName);
                        foreach (var edge in edgesIn)
                        {
                            int idx = n.InputWires.IndexOf(edge);
                            var oldPort = n.InputWires[idx].RightSidePin;
                            var newPort = new BlueprintPinReference(newName, oldPort.NodeGuid, oldPort.IsExecutePin);
                            var newEdge = new BlueprintWireReference(n.InputWires[idx].LeftSidePin, newPort);
                            n.InputWires[idx] = newEdge;
                        }
                    }

                    if (n.NodeType == NodeType.Entry)
                    {
                        if (n.OutputPins.TryGetValue(oldName, out var pin))
                        {
                            pin.RenamePort(newName);
                            n.OutputPins[newName] = pin;
                            n.OutputPins.Remove(oldName);
                        }
                    }

                    if (n.NodeType == NodeType.Return)
                    {
                        if (n.InputPins.TryGetValue(oldName, out var pin))
                        {
                            pin.RenamePort(newName);
                            n.InputPins[newName] = pin;
                            n.InputPins.Remove(oldName);
                        }
                    }

                    var edgeConnections = n.InputWires.FindAll(e => e.LeftSidePin.PinName == oldName);
                    foreach (var edge in edgeConnections)
                    {
                        int idx = n.InputWires.IndexOf(edge);
                        var oldPort = n.InputWires[idx].LeftSidePin;
                        var newPort = new BlueprintPinReference(newName, oldPort.NodeGuid, oldPort.IsExecutePin);
                        var newEdge = new BlueprintWireReference(newPort, n.InputWires[idx].RightSidePin);
                        n.InputWires[idx] = newEdge;
                    }
                }
            }
        }
        
        private void Retype(Type oldType, Type newType)
        {
            DefaultValue = newType.IsClass ? null : FormatterServices.GetUninitializedObject(newType);
            Updated(ChangeType.Type);
            
            if(Scope != VariableScopeType.Class)
            {
                foreach (var n in _methodGraph.Nodes.Values)
                {
                    if (n.NodeType == NodeType.Entry)
                    {
                        if (n.OutputPins.TryGetValue(Name, out var pin))
                        {
                            pin.Type = newType;
                        }
                    }

                    if (n.NodeType == NodeType.Return)
                    {
                        if (n.InputPins.TryGetValue(Name, out var pin))
                        {
                            pin.Type = newType;
                        }
                    }

                    if (n.NodeType == NodeType.MemberAccess)
                    {
                        var model =  (MemberNodeModel)n;
                        if (model.VariableAccess == VariableAccessType.Get)
                        {
                            if (n.OutputPins.TryGetValue(Name, out var pin))
                            {
                                pin.Type = newType;
                            }
                        }
                        else
                        {
                            if (n.InputPins.TryGetValue(Name, out var pin1))
                            {
                                pin1.Type = newType;
                            }

                            if (n.OutputPins.TryGetValue(Name, out var pin2))
                            {
                                pin2.Type = newType;
                            }
                        }
                    }
                }
            }
        }

        private void Updated(ChangeType changeType)
        {
            Changed?.Invoke(this, changeType);
            if (Scope == VariableScopeType.Class)
            {
                _classGraphModel.OnVariableUpdated(this);
            }
            else
            {
                _methodGraph.OnVariableUpdated(this);
            }
        }
        
        public static string FormatConstructorSignature(ConstructorInfo c)
        {
            // Get the parameter list as "Type paramName"
            string parameters = string.Join(", ", c.GetParameters()
                .Select(p => $"{TypeSelectorField.GetReadableTypeName(p.ParameterType)} {p.Name}"));

            // Format the constructor signature nicely
            string constructorSignature = $"{TypeSelectorField.GetReadableTypeName(c.DeclaringType)}({parameters})";
            return constructorSignature;
        }
        
        public static ConstructorInfo GetConstructor(Type type, string constructorSignature)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            return (from constructor in constructors let signature = FormatConstructorSignature(constructor) where signature.Equals(constructorSignature) select constructor).FirstOrDefault();
        }

        public void Delete()
        {
            Changed?.Invoke(this, ChangeType.Delete);
            if (Scope == VariableScopeType.Class)
            {
                _classGraphModel.RemoveVariable(this);
            }
            else
            {
                _methodGraph.RemoveVariable(this);
            }
        }
    }

    public class BlueprintArgument
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
        public string DisplayName { get; private set; }

        private readonly BlueprintMethodGraph _method;
        
        public event Action<BlueprintArgument, ChangeType> Changed;

        public BlueprintArgument(BlueprintMethodGraph method, Type type, string name, int parameterIndex, bool isRef, bool isOut, bool isReturn)
        {
            _method = method;
            Type = type;
            DisplayName = name;
            ParameterIndex = parameterIndex;
            IsRef = isRef;
            IsOut = isOut;
            IsReturn = isReturn;
        }

        public BlueprintArgument(BlueprintMethodGraph method, BlueprintArgumentDto dto) : this(method, dto.Type, dto.DisplayName, dto.ParameterIndex, dto.IsRef, dto.IsOut, dto.IsReturn)
        {
            
        }

        public void SetName(string newName)
        {
            DisplayName = newName;
            Changed?.Invoke(this, ChangeType.Name);
            _method.OnArgumentUpdated(this);
        }
        
        public void SetType(Type newType)
        {
            Type = newType;
            Changed?.Invoke(this, ChangeType.Type);
            _method.OnArgumentUpdated(this);
        }
        
        public BlueprintArgumentDto Serialize()
        {
            return new BlueprintArgumentDto
            {
                Type = Type,
                DisplayName = DisplayName,
                ParameterIndex = ParameterIndex,
                IsRef = IsRef,
                IsOut = IsOut,
                IsReturn = IsReturn,
            };
        }
    }
}