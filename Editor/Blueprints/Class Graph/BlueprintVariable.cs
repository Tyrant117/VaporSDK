using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Vapor.Inspector;
using VaporEditor.Blueprints;
using VaporEditor.Inspector;

namespace Vapor.Blueprints
{
    public enum VariableType
    {
        Local,
        Global,
        Argument,
        OutArgument,
        Return
    }
    
    [Serializable]
    public struct BlueprintVariableDto
    {
        public string Name;
        public Type Type;
        public VariableType VariableType;
        public VariableAccessModifier AccessModifier;
        
        // Variable Constructions
        public string ConstructorName;
        public List<(Type, object)> DefaultParametersValue;
    }
    
    public class BlueprintVariable
    {
        
        
        private BlueprintDesignGraph _classGraph;
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
        public VariableType VariableType { get; }
        public VariableAccessModifier AccessModifier { get; set; }

        public object DefaultValue { get; set; }
        public string ConstructorName { get; set; }
        public List<object> ParameterValues { get; set; } = new();

        public BlueprintVariable(string name, Type type, VariableType variableType)
        {
            _name = name;
            _type = type;
            VariableType = variableType;
            ConstructorName = "Default(T)";
            switch (VariableType)
            {
                case VariableType.Local:
                case VariableType.Global:
                    DefaultValue = type.IsClass ? null : FormatterServices.GetUninitializedObject(type);
                    ParameterValues.Add(DefaultValue);
                    break;
            }
        }

        public BlueprintVariable(BlueprintVariableDto dto)
        {
            _name = dto.Name;
            _type = dto.Type;
            VariableType = dto.VariableType;
            AccessModifier = dto.AccessModifier;
            ConstructorName = dto.ConstructorName;
            if (!ConstructorName.EmptyOrNull() && !ConstructorName.Equals("Default(T)"))
            {
                if (dto.DefaultParametersValue != null)
                {
                    var convertedTypes = dto.DefaultParametersValue.Select(t => (t.Item1, TypeUtility.CastToType(t.Item2, t.Item1))).ToArray();
                    // var types = convertedTypes.Select(t => t.Item1).ToArray();
                    var paramObjs = convertedTypes.Select(t => t.Item2).ToArray();
                    ParameterValues.AddRange(paramObjs);
                }

                switch (VariableType)
                {
                    case VariableType.Local:
                    case VariableType.Global:
                        var constructor = GetConstructor(_type, ConstructorName);
                        DefaultValue = constructor.Invoke(ParameterValues.ToArray());
                        break;
                }
            }
            else
            {
                if (dto.DefaultParametersValue is { Count: 1 })
                {
                    var converted = TypeUtility.CastToType(dto.DefaultParametersValue[0].Item2, dto.DefaultParametersValue[0].Item1);
                    ParameterValues.Add(converted);
                }
                switch (VariableType)
                {
                    case VariableType.Local:
                    case VariableType.Global:
                        DefaultValue = ParameterValues.Count == 1 ? ParameterValues[0] : _type.IsClass ? null : FormatterServices.GetUninitializedObject(_type);
                        break;
                }
            }
        }

        public BlueprintVariable WithClassGraph(BlueprintDesignGraph graph)
        {
            _classGraph = graph;
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
                VariableType = VariableType,
                AccessModifier = AccessModifier,
                ConstructorName = ConstructorName,
                DefaultParametersValue = ParameterValues.Select(p => (p.GetType(), p)).ToList()
            };
        }

        private void Rename(string oldName, string newName)
        {
            if (VariableType != VariableType.Global)
            {
                foreach (var n in _methodGraph.Nodes)
                {
                    // Works for Getters and Setters
                    if (n.Model.NodeType == NodeType.MemberAccess &&
                        n.ModelAs<MemberNodeModel>().VariableName == oldName)
                    {
                        var model = n.ModelAs<MemberNodeModel>();
                        model.VariableName = newName;
                        n.NodeName = model.VariableAccess == VariableAccessType.Get ? $"Get <b><i>{newName}</i></b>" : $"Set <b><i>{newName}</i></b>";
                        var edges = n.Model.OutputWires.FindAll(e => e.LeftSidePin.PinName == oldName);
                        foreach (var edge in edges)
                        {
                            int idx = n.Model.OutputWires.IndexOf(edge);
                            var oldPort = n.Model.OutputWires[idx].LeftSidePin;
                            var newPort = new BlueprintPinReference(newName, oldPort.NodeGuid, oldPort.IsExecutePin);
                            var newEdge = new BlueprintWireReference(newPort, n.Model.OutputWires[idx].RightSidePin);
                            n.Model.OutputWires[idx] = newEdge;
                        }

                        var edgesIn = n.Model.InputWires.FindAll(e => e.RightSidePin.PinName == oldName);
                        foreach (var edge in edgesIn)
                        {
                            int idx = n.Model.InputWires.IndexOf(edge);
                            var oldPort = n.Model.InputWires[idx].RightSidePin;
                            var newPort = new BlueprintPinReference(newName, oldPort.NodeGuid, oldPort.IsExecutePin);
                            var newEdge = new BlueprintWireReference(n.Model.InputWires[idx].LeftSidePin, newPort);
                            n.Model.InputWires[idx] = newEdge;
                        }
                    }

                    if (n.Model.NodeType == NodeType.Entry)
                    {
                        if (n.OutPorts.TryGetValue(oldName, out var pin))
                        {
                            pin.RenamePort(newName);
                            n.OutPorts[newName] = pin;
                            n.OutPorts.Remove(oldName);
                        }
                    }

                    if (n.Model.NodeType == NodeType.Return)
                    {
                        if (n.InPorts.TryGetValue(oldName, out var pin))
                        {
                            pin.RenamePort(newName);
                            n.InPorts[newName] = pin;
                            n.InPorts.Remove(oldName);
                        }
                    }

                    var edgeConnections = n.Model.InputWires.FindAll(e => e.LeftSidePin.PinName == oldName);
                    foreach (var edge in edgeConnections)
                    {
                        int idx = n.Model.InputWires.IndexOf(edge);
                        var oldPort = n.Model.InputWires[idx].LeftSidePin;
                        var newPort = new BlueprintPinReference(newName, oldPort.NodeGuid, oldPort.IsExecutePin);
                        var newEdge = new BlueprintWireReference(newPort, n.Model.InputWires[idx].RightSidePin);
                        n.Model.InputWires[idx] = newEdge;
                    }
                }
            }
        }
        
        private void Retype(Type oldType, Type newType)
        {
            DefaultValue = newType.IsClass ? null : FormatterServices.GetUninitializedObject(newType);
            if(VariableType != VariableType.Global)
            {
                foreach (var n in _methodGraph.Nodes)
                {
                    if (n.Model.NodeType == NodeType.Entry)
                    {
                        if (n.OutPorts.TryGetValue(Name, out var pin))
                        {
                            pin.Type = newType;
                        }
                    }

                    if (n.Model.NodeType == NodeType.Return)
                    {
                        if (n.InPorts.TryGetValue(Name, out var pin))
                        {
                            pin.Type = newType;
                        }
                    }

                    if (n.Model.NodeType == NodeType.MemberAccess)
                    {
                        var model = n.ModelAs<MemberNodeModel>();
                        if (model.VariableAccess == VariableAccessType.Get)
                        {
                            if (n.OutPorts.TryGetValue(Name, out var pin))
                            {
                                pin.Type = newType;
                            }
                        }
                        else
                        {
                            if (n.InPorts.TryGetValue(Name, out var pin1))
                            {
                                pin1.Type = newType;
                            }

                            if (n.OutPorts.TryGetValue(Name, out var pin2))
                            {
                                pin2.Type = newType;
                            }
                        }
                    }
                }
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
    }
}