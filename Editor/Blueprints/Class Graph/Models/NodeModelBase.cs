using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Vapor;
using Vapor.Blueprints;
using Vapor.Inspector;
using VaporEditor.Inspector;

namespace VaporEditor.Blueprints
{
    public abstract class NodeModelBase
    {
        private static readonly Color s_DefaultTextColor = new(0.7568628f, 0.7568628f, 0.7568628f);
        private static readonly Color s_ErrorTextColor = new(0.7568628f, 0f, 0f);
        
        public BlueprintMethodGraph Method { get; }
        public string Guid { get; }
        public uint Uuid { get; }
        public NodeType NodeType { get; }
        public Rect Position { get; set; }
        public List<BlueprintWireReference> InputWires { get; }
        public List<BlueprintWireReference> OutputWires { get; }
        
        // Non-Serialized
        public string Name { get; protected set; }
        public Dictionary<string, BlueprintPin> InputPins { get; } = new();
        public Dictionary<string, BlueprintPin> OutputPins { get; } = new();
        
        // Errors
        public bool HasError { get; protected set; }
        public string ErrorText { get; protected set; }
        
        // Events
        public event Action<NodeModelBase> NameChanged;

        protected NodeModelBase(BlueprintMethodGraph method, BlueprintDesignNodeDto dto)
        {
            Method = method;
            Guid = dto.Guid;
            Uuid = dto.Uuid;
            NodeType = dto.NodeEnumType;
            Position = dto.Position;
            InputWires = new List<BlueprintWireReference>(dto.InputWires);
            OutputWires = new List<BlueprintWireReference>(dto.OutputWires);
        }


        public virtual void BuildPins(){}

        public virtual void PostBuildData()
        {
            Validate();
        }

        #region - Settings -

        public virtual (string, Color) GetNodeName() => HasError ? (Name, s_ErrorTextColor) : (Name, s_DefaultTextColor);
        public virtual (string, Color, string) GetNodeNameIcon() => HasError ? ("Error", Color.white, ErrorText) : (null, Color.white, string.Empty);
        
        public void SetName(string name)
        {
            Name = name;
            NameChanged?.Invoke(this);
        }

        #endregion

        #region - Validation  -

        public void SetError(string errorText)
        {
            HasError = true;
            ErrorText = errorText;
        }

        protected virtual void Validate()
        {
            // InputWires.RemoveAll(w => Method.Nodes.FindIndex(n => n.Guid == w.LeftSidePin.NodeGuid) == -1);
            // OutputWires.RemoveAll(w => Method.Nodes.FindIndex(n => n.Guid == w.RightSidePin.NodeGuid) == -1);
        }
        
        public string FormatWithUuid(string prefix) => $"{prefix}_{Uuid}";
        #endregion

        public bool OnEnumChanged()
        {
            var wire = InputWires.FirstOrDefault(w => w.RightSidePin.PinName == PinNames.VALUE_IN);
            if (!wire.IsValid())
            {
                return false;
            }
            
            var node = Method.Nodes.FirstOrDefault(n => n.Value.Guid == wire.LeftSidePin.NodeGuid).Value;
            if (node == null)
            {
                return false;
            }
        
            var pin = node.OutputPins[wire.LeftSidePin.PinName];
            if (!pin.Type.IsEnum)
            {
                return false;
            }
        
            OutputPins.Clear();
            OutputWires.Clear();
                        
            var defaultSlot = new BlueprintPin(this, PinNames.DEFAULT_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(PinNames.DEFAULT_OUT);
            OutputPins.Add(PinNames.DEFAULT_OUT, defaultSlot);
                        
            var enumNames = pin.Type.GetEnumNames();
            foreach (var name in enumNames)
            {
                var enumSlot = new BlueprintPin(this, name, PinDirection.Out, typeof(ExecutePin), false)
                    .WithDisplayName(name);
                OutputPins.Add(name, enumSlot);
            }
            return true;
        }
        
        public BlueprintDesignNodeDto Serialize()
        {
            var dto = BeginSerialize();
            
            foreach (var port in InputPins.Where(port => !port.Value.IsExecutePin)
                         .Where(port => port.Value.HasInlineValue))
            {
                dto.InputPins.Add(new BlueprintPinDto
                {
                    PinName = port.Key,
                    PinType = port.Value.InlineValue.GetResolvedType(),
                    Content = port.Value.InlineValue.Get(),
                    WireGuids = new List<string>(port.Value.WireGuids),
                });
            }
            
            foreach (var port in OutputPins.Where(port => !port.Value.IsExecutePin))
            {
                dto.OutputPins.Add(new BlueprintPinDto
                {
                    PinName = port.Key,
                    PinType = port.Value.Type,
                    Content = null,
                    WireGuids = new List<string>(port.Value.WireGuids),
                });
            }
            return dto;
        }
        
        public virtual BlueprintDesignNodeDto BeginSerialize()
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

    public class EntryNode : NodeModelBase
    {
        public EntryNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            Name = "Entry";
        }

        public override void BuildPins()
        {
            var outSlot = new BlueprintPin(this, PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty);
            OutputPins.Add(PinNames.EXECUTE_OUT, outSlot);

            foreach (var argument in Method.Arguments)
            {
                if (argument.IsReturn || argument.IsOut)
                {
                    continue;
                }
                        
                var slot = new BlueprintPin(this, argument.DisplayName, PinDirection.Out, argument.Type, false)
                    .WithAllowMultipleWires();
                OutputPins.Add(argument.DisplayName, slot);
            }
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
        public Dictionary<Type, BlueprintPin> GenericArgumentPortMap { get; } = new();

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
        
        public MethodNodeModel(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
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

        public override void BuildPins()
        {
            var paramInfos = MethodInfo.GetParameters();
            var genericArgs = MethodInfo.GetGenericArguments();
            bool hasOutParameter = paramInfos.Any(p => p.IsOut);
            var callableAttribute = MethodInfo.GetCustomAttribute<BlueprintCallableAttribute>();

            var nodeName = MethodInfo.IsSpecialName ? GetMethodSignature().ToTitleCase() : GetMethodSignature();
            nodeName = MethodInfo.IsStatic ? $"{MethodInfo.DeclaringType!.Name}.{nodeName}" : nodeName;
            Name = callableAttribute == null || callableAttribute.NodeName.EmptyOrNull() ? nodeName : callableAttribute.NodeName;

            if (!IsPure)
            {
                var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                    .WithDisplayName(string.Empty)
                    .WithAllowMultipleWires();
                InputPins.Add(PinNames.EXECUTE_IN, inSlot);
                var outSlot = new BlueprintPin(this, PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                    .WithDisplayName(string.Empty);
                OutputPins.Add(PinNames.EXECUTE_OUT, outSlot);
            }

            if (!MethodInfo.IsStatic)
            {
                var slot = new BlueprintPin(this, PinNames.OWNER, PinDirection.In, MethodInfo.DeclaringType, false);
                InputPins.Add(PinNames.OWNER, slot);
            }

            if (MethodInfo.ReturnType != typeof(void))
            {
                var retParam = MethodInfo.ReturnParameter;
                if (retParam is { IsRetval: true })
                {
                    // Out Ports
                    var slot = new BlueprintPin(this, PinNames.RETURN, PinDirection.Out, retParam.ParameterType, false)
                        .WithAllowMultipleWires();
                    OutputPins.Add(PinNames.RETURN, slot);

                    if (slot.IsGenericPin)
                    {
                        slot.GenericPinType = retParam.ParameterType; //genericArgs[0];
                        GenericArgumentPortMap.Add(retParam.ParameterType, slot);
                    }
                }
            }

            // int genArgCount = GenericArgumentPortMap.Count;
            foreach (var pi in paramInfos)
            {
                if (pi.IsOut)
                {
                    // Out Ports
                    var paramAttribute = pi.GetCustomAttribute<BlueprintParamAttribute>();
                    bool isWildcard = false;
                    string portName = pi.Name;
                    var displayName = ObjectNames.NicifyVariableName(pi.Name);

                    if (paramAttribute != null)
                    {
                        isWildcard = paramAttribute.WildcardTypes != null;
                        if (!paramAttribute.Name.EmptyOrNull())
                        {
                            displayName = paramAttribute.Name;
                        }
                    }

                    var type = pi.ParameterType;
                    if (type.IsByRef)
                    {
                        type = type.GetElementType();
                    }

                    var slot = new BlueprintPin(this, portName, PinDirection.Out, type, false)
                        .WithDisplayName(displayName)
                        .WithAllowMultipleWires();
                    if (isWildcard)
                    {
                        slot.WithWildcardTypes(paramAttribute.WildcardTypes);
                    }

                    OutputPins.Add(portName, slot);
                    if (slot.IsGenericPin)
                    {
                        slot.GenericPinType = type;
                        GenericArgumentPortMap.Add(type, slot);
                    }
                }
                else
                {
                    // In Ports
                    var paramAttribute = pi.GetCustomAttribute<BlueprintParamAttribute>();
                    bool isWildcard = false;
                    string portName = pi.Name;
                    var displayName = ObjectNames.NicifyVariableName(pi.Name);

                    if (paramAttribute != null)
                    {
                        isWildcard = paramAttribute.WildcardTypes != null;
                        if (!paramAttribute.Name.EmptyOrNull())
                        {
                            displayName = paramAttribute.Name;
                        }
                    }

                    var slot = new BlueprintPin(this, portName, PinDirection.In, pi.ParameterType, false)
                        .WithDisplayName(displayName)
                        .WithIsOptional();
                    if (isWildcard)
                    {
                        slot.WithWildcardTypes(paramAttribute.WildcardTypes);
                    }

                    if (pi.HasDefaultValue && slot.HasInlineValue)
                    {
                        slot.SetDefaultValue(pi.DefaultValue);
                    }

                    InputPins.Add(portName, slot);
                    if (slot.IsGenericPin)
                    {
                        slot.GenericPinType = pi.ParameterType;
                        GenericArgumentPortMap.Add(pi.ParameterType, slot);
                    }
                }
            }
        }


        public override BlueprintDesignNodeDto BeginSerialize()
        {
            var dto = base.BeginSerialize();
            dto.Properties.TryAdd(NodePropertyNames.K_METHOD_DECLARING_TYPE, (typeof(Type), MethodType));
            dto.Properties.TryAdd(NodePropertyNames.K_METHOD_NAME, (typeof(string), MethodName));
            dto.Properties.TryAdd(NodePropertyNames.K_METHOD_PARAMETER_TYPES, (typeof(string[]), MethodParameters));
            return dto;
        }
        
        private string GetMethodSignature()
        {
            if (MethodInfo == null)
            {
                return string.Empty;
            }
                
            string methodName = MethodInfo.Name;
            if (!MethodInfo.IsGenericMethod)
            {
                return $"{methodName}";
            }

            var genericArgs = MethodInfo.GetGenericArguments();
            var genArgString = genericArgs.Select(t => GenericArgumentPortMap.TryGetValue(t, out var pin) ? pin.Type.Name : t.Name);
            string genericArgsStr = string.Join(", ", genArgString);
            methodName += $"<{genericArgsStr}>";
            return $"{methodName}";
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
        
        public MemberNodeModel(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
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

        public override void BuildPins()
        {
            if (FieldInfo != null)
            {
                if (VariableAccess == VariableAccessType.Get)
                {
                    Name = ObjectNames.NicifyVariableName(FieldInfo.Name);

                    // In Pin
                    if (!FieldInfo.IsStatic)
                    {
                        var ownerPin = new BlueprintPin(this, PinNames.OWNER, PinDirection.In, FieldInfo.DeclaringType, true);
                        InputPins.Add(PinNames.OWNER, ownerPin);
                    }

                    // Out Pin
                    var returnPin = new BlueprintPin(this, PinNames.GET_OUT, PinDirection.Out, FieldInfo.FieldType, false)
                        .WithAllowMultipleWires();
                    OutputPins.Add(PinNames.GET_OUT, returnPin);
                }
                else
                {
                    Name = ObjectNames.NicifyVariableName(FieldInfo.Name);

                    var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                        .WithDisplayName(string.Empty)
                        .WithAllowMultipleWires();
                    InputPins.Add(PinNames.EXECUTE_IN, inSlot);
                    var outSlot = new BlueprintPin(this, PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                        .WithDisplayName(string.Empty);
                    OutputPins.Add(PinNames.EXECUTE_OUT, outSlot);

                    // In Pin
                    if (!FieldInfo.IsStatic)
                    {
                        var ownerPin = new BlueprintPin(this, PinNames.OWNER, PinDirection.In, FieldInfo.DeclaringType, true);
                        InputPins.Add(PinNames.OWNER, ownerPin);
                    }

                    // Set Pin
                    var setterPin = new BlueprintPin(this, PinNames.SET_IN, PinDirection.In, FieldInfo.FieldType, false);
                    InputPins.Add(PinNames.SET_IN, setterPin);

                    // Out Pin
                    var returnPin = new BlueprintPin(this, PinNames.GET_OUT, PinDirection.Out, FieldInfo.FieldType, false)
                        .WithAllowMultipleWires();
                    OutputPins.Add(PinNames.GET_OUT, returnPin);
                }
            }
            else
            {
                if (VariableAccess == VariableAccessType.Get)
                {
                    if (!Method.TryGetVariable(VariableScope, VariableName, out var tempData))
                    {
                        Debug.LogError($"{VariableName} not found in graph, was the variable deleted?");
                        SetError($"{VariableName} not found in graph, was the variable deleted?");
                        Name = $"Get <b><i>{VariableName}</i></b>";
                        return;
                    }

                    Name = $"Get <b><i>{tempData.Name}</i></b>";

                    var slot = new BlueprintPin(this, PinNames.GET_OUT, PinDirection.Out, tempData.Type, false)
                        .WithDisplayName(string.Empty)
                        .WithAllowMultipleWires();
                    OutputPins.Add(PinNames.GET_OUT, slot);
                }
                else
                {
                    if (!Method.TryGetVariable(VariableScope, VariableName, out var tempData))
                    {
                        Debug.LogError($"{VariableName} not found in graph, was the variable deleted?");
                        SetError($"{VariableName} not found in graph, was the variable deleted?");
                        Name = $"Set <b><i>{VariableName}</i></b>";
                        return;
                    }

                    Name = $"Set <b><i>{tempData.Name}</i></b>";

                    var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                        .WithDisplayName(string.Empty)
                        .WithAllowMultipleWires();
                    InputPins.Add(PinNames.EXECUTE_IN, inSlot);

                    var inData = new BlueprintPin(this, PinNames.SET_IN, PinDirection.In, tempData.Type, false)
                        .WithDisplayName(string.Empty);
                    InputPins.Add(PinNames.SET_IN, inData);

                    var outSlot = new BlueprintPin(this, PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                        .WithDisplayName(string.Empty);
                    OutputPins.Add(PinNames.EXECUTE_OUT, outSlot);

                    var os = new BlueprintPin(this, PinNames.GET_OUT, PinDirection.Out, tempData.Type, false)
                        .WithDisplayName(string.Empty)
                        .WithAllowMultipleWires();
                    OutputPins.Add(PinNames.GET_OUT, os);
                }
            }
        }

        public override BlueprintDesignNodeDto BeginSerialize()
        {
            var dto = base.BeginSerialize();
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

    public class ReturnNode : NodeModelBase
    {
        public ReturnNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            Name = "Return";
        }

        public override void BuildPins()
        {
            var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName("")
                .WithAllowMultipleWires();
            InputPins.Add(PinNames.EXECUTE_IN, inSlot);

            foreach (var argument in Method.Arguments)
            {
                if (!argument.IsReturn && !argument.IsOut)
                {
                    continue;
                }

                var slot = new BlueprintPin(this, argument.DisplayName, PinDirection.In, argument.Type, false);
                InputPins.Add(argument.DisplayName, slot);
            }
        }
    }

    public class BranchNode : NodeModelBase
    {
        public BranchNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            Name = "Branch";
        }

        public override void BuildPins()
        {
            var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName("")
                .WithAllowMultipleWires();
            InputPins.Add(PinNames.EXECUTE_IN, inSlot);

            var slot = new BlueprintPin(this, PinNames.VALUE_IN, PinDirection.In, typeof(bool), false);
            InputPins.Add(PinNames.VALUE_IN, slot);

            var trueSlot = new BlueprintPin(this, PinNames.TRUE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(PinNames.TRUE_OUT);
            OutputPins.Add(PinNames.TRUE_OUT, trueSlot);

            var falseSlot = new BlueprintPin(this, PinNames.FALSE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(PinNames.FALSE_OUT);
            OutputPins.Add(PinNames.FALSE_OUT, falseSlot);
        }
    }

    public class SwitchNode : NodeModelBase
    {
        public SwitchNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            Name = "Switch";
        }

        public override void BuildPins()
        {
            var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName("")
                .WithAllowMultipleWires();
            InputPins.Add(PinNames.EXECUTE_IN, inSlot);

            var slot = new BlueprintPin(this, PinNames.VALUE_IN, PinDirection.In, typeof(Enum), false).WithWildcardTypes(new[] { typeof(Enum), typeof(int), typeof(string) });
            InputPins.Add(PinNames.VALUE_IN, slot);
                    
            var defaultSlot = new BlueprintPin(this, PinNames.DEFAULT_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(PinNames.DEFAULT_OUT);
            OutputPins.Add(PinNames.DEFAULT_OUT, defaultSlot);
        }

        public override void PostBuildData()
        {
            var wire = InputWires.FirstOrDefault(w => w.RightSidePin.PinName == PinNames.VALUE_IN);
            if (!wire.IsValid())
            {
                Validate();
                return;
            }
            
            var node = Method.Nodes.FirstOrDefault(n => n.Value.Guid == wire.LeftSidePin.NodeGuid).Value;
            if (node == null)
            {
                Validate();
                return;
            }

            var pin = node.OutputPins[wire.LeftSidePin.PinName];
            if (!pin.Type.IsEnum)
            {
                Validate();
                return;
            }
                        
            var enumNames = pin.Type.GetEnumNames();
            foreach (var name in enumNames)
            {
                var enumSlot = new BlueprintPin(this, name, PinDirection.Out, typeof(ExecutePin), false)
                    .WithDisplayName(name);
                OutputPins.Add(name, enumSlot);
            }
            
            Validate();
        }
    }

    public class SequenceNode : NodeModelBase
    {
        public int SequenceCount { get; protected set; }

        public event Action<SequenceNode> SequenceCountChanged;
        
        public SequenceNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            Name = "Sequence";
            SequenceCount = dto.GetProperty<int>(NodePropertyNames.DATA_VALUE);
        }

        public override void BuildPins()
        {
            var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName("")
                .WithAllowMultipleWires();
            InputPins.Add(PinNames.EXECUTE_IN, inSlot);

            for (int i = 0; i < SequenceCount; i++)
            {
                string formattedName = $"{PinNames.SEQUENCE_OUT}_{i}";
                var pin = new BlueprintPin(this, formattedName, PinDirection.Out, typeof(ExecutePin), false)
                    .WithDisplayName(formattedName);
                OutputPins.Add(formattedName, pin);
            }
        }

        public void SetSequenceCount(int count)
        {
            SequenceCount = count;
            SequenceCountChanged?.Invoke(this);
        }
    }

    public class ForNode : NodeModelBase
    {
        public ForNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            Name = "For";
        }

        public override void BuildPins()
        {
            var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            InputPins.Add(PinNames.EXECUTE_IN, inSlot);

            var arraySlot = new BlueprintPin(this, PinNames.ARRAY_IN, PinDirection.In, typeof(ICollection), false)
                .WithDisplayName("Array");
            InputPins.Add(PinNames.ARRAY_IN, arraySlot);

            var startIndexPin = new BlueprintPin(this, PinNames.START_INDEX_IN, PinDirection.In, typeof(int), false)
                .WithDisplayName("Start");
            InputPins.Add(PinNames.START_INDEX_IN, startIndexPin);

            var endIndexPin = new BlueprintPin(this, PinNames.LENGTH_IN, PinDirection.In, typeof(int), false)
                .WithDisplayName("Length");
            InputPins.Add(PinNames.LENGTH_IN, endIndexPin);

            var loopSlot = new BlueprintPin(this, PinNames.LOOP_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("Loop");
            OutputPins.Add(PinNames.LOOP_OUT, loopSlot);

            var indexSlot = new BlueprintPin(this, PinNames.INDEX_OUT, PinDirection.Out, typeof(int), false)
                .WithDisplayName("Index");
            OutputPins.Add(PinNames.INDEX_OUT, indexSlot);

            var elementSlot = new BlueprintPin(this, PinNames.ELEMENT_OUT, PinDirection.Out, typeof(object), false)
                .WithDisplayName("Element");
            OutputPins.Add(PinNames.ELEMENT_OUT, elementSlot);

            var completedSlot = new BlueprintPin(this, PinNames.COMPLETE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("Complete");
            OutputPins.Add(PinNames.COMPLETE_OUT, completedSlot);
        }
    }

    public class ForEachNode : NodeModelBase
    {
        public ForEachNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            Name = "ForEach";
        }

        public override void BuildPins()
        {
            var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            InputPins.Add(PinNames.EXECUTE_IN, inSlot);

            var arraySlot = new BlueprintPin(this, PinNames.ARRAY_IN, PinDirection.In, typeof(IEnumerable), false)
                .WithDisplayName("Array");
            InputPins.Add(PinNames.ARRAY_IN, arraySlot);

            var loopSlot = new BlueprintPin(this, PinNames.LOOP_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("Loop");
            OutputPins.Add(PinNames.LOOP_OUT, loopSlot);

            var indexSlot = new BlueprintPin(this, PinNames.INDEX_OUT, PinDirection.Out, typeof(int), false)
                .WithDisplayName("Index");
            OutputPins.Add(PinNames.INDEX_OUT, indexSlot);

            var elementSlot = new BlueprintPin(this, PinNames.ELEMENT_OUT, PinDirection.Out, typeof(object), false)
                .WithDisplayName("Element");
            OutputPins.Add(PinNames.ELEMENT_OUT, elementSlot);

            var completedSlot = new BlueprintPin(this, PinNames.COMPLETE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("Complete");
            OutputPins.Add(PinNames.COMPLETE_OUT, completedSlot);
        }
    }

    public class WhileNode : NodeModelBase
    {
        public WhileNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            Name = "While";
        }

        public override void BuildPins()
        {
            var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            InputPins.Add(PinNames.EXECUTE_IN, inSlot);

            var whileTruePin = new BlueprintPin(this, PinNames.VALUE_IN, PinDirection.In, typeof(bool), false)
                .WithDisplayName("Condition");
            InputPins.Add(PinNames.VALUE_IN, whileTruePin);

            var loopSlot = new BlueprintPin(this, PinNames.LOOP_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("Loop");
            OutputPins.Add(PinNames.LOOP_OUT, loopSlot);

            var completedSlot = new BlueprintPin(this, PinNames.COMPLETE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("Complete");
            OutputPins.Add(PinNames.COMPLETE_OUT, completedSlot);
        }
    }

    public class BreakNode : NodeModelBase
    {
        public BreakNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            Name = "Break";
        }

        public override void BuildPins()
        {
            var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            InputPins.Add(PinNames.EXECUTE_IN, inSlot);
        }
    }

    public class ContinueNode : NodeModelBase
    {
        public ContinueNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            Name = "Continue";
        }

        public override void BuildPins()
        {
            var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            InputPins.Add(PinNames.EXECUTE_IN, inSlot);
        }
    }

    public class ConversionNode : NodeModelBase
    {
        public Type TypeIn { get; protected set; }
        public Type TypeOut { get; protected set; }
        
        public ConversionNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            Name = string.Empty;
            var types = dto.GetProperty<(Type, Type)>(NodePropertyNames.DATA_VALUE);
            TypeIn = types.Item1;
            TypeOut = types.Item2;
        }

        public override void BuildPins()
        {
            var slot = new BlueprintPin(this, PinNames.SET_IN, PinDirection.In, TypeIn, false)
                .WithDisplayName(string.Empty);
            InputPins.Add(PinNames.SET_IN, slot);

            var outSlot = new BlueprintPin(this, PinNames.GET_OUT, PinDirection.Out, TypeOut, false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            OutputPins.Add(PinNames.GET_OUT, outSlot);
        }
    }

    public class CastNode : NodeModelBase
    {
        public CastNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            Name = "Cast";
        }

        public override void BuildPins()
        {
            var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            InputPins.Add(PinNames.EXECUTE_IN, inSlot);

            var valuePin = new BlueprintPin(this, PinNames.VALUE_IN, PinDirection.In, typeof(object), false)
                .WithDisplayName(PinNames.VALUE_IN);
            InputPins.Add(PinNames.VALUE_IN, valuePin);

            var validPin = new BlueprintPin(this, PinNames.VALID_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(PinNames.VALID_OUT);
            OutputPins.Add(PinNames.VALID_OUT, validPin);

            var invalidPin = new BlueprintPin(this, PinNames.INVALID_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(PinNames.INVALID_OUT);
            OutputPins.Add(PinNames.INVALID_OUT, invalidPin);
                    
            var castedValuePin = new BlueprintPin(this, PinNames.AS_OUT, PinDirection.Out, typeof(GenericPin), false)
                .WithDisplayName(PinNames.AS_OUT);
            OutputPins.Add(PinNames.AS_OUT, castedValuePin);
        }

        public override void PostBuildData()
        {
            var castName = OutputPins[PinNames.AS_OUT].Type != typeof(UndefinedPin) ? TypeSelectorField.GetReadableTypeName(OutputPins[PinNames.AS_OUT].Type) : "T";
            Name = $"Cast<{castName}>";
            
            Validate();
        }
    }

    public class RedirectNode : NodeModelBase
    {
        public Type RedirectType { get; protected set; }
        
        public RedirectNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            Name = string.Empty;
            RedirectType = dto.GetProperty<Type>(NodePropertyNames.DATA_VALUE);
        }

        public override void BuildPins()
        {
            if (RedirectType != typeof(ExecutePin))
            {
                var slot = new BlueprintPin(this, PinNames.SET_IN, PinDirection.In, RedirectType, false)
                    .WithDisplayName(string.Empty);
                InputPins.Add(PinNames.SET_IN, slot);

                var outSlot = new BlueprintPin(this, PinNames.GET_OUT, PinDirection.Out, RedirectType, false)
                    .WithDisplayName(string.Empty)
                    .WithAllowMultipleWires();
                OutputPins.Add(PinNames.GET_OUT, outSlot);
            }
            else
            {
                var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                    .WithDisplayName(string.Empty)
                    .WithAllowMultipleWires();
                InputPins.Add(PinNames.EXECUTE_IN, inSlot);
                var outSlot = new BlueprintPin(this, PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                    .WithDisplayName(string.Empty);
                OutputPins.Add(PinNames.EXECUTE_OUT, outSlot);
            }
        }
    }

    public class InlineNode : NodeModelBase
    {
        public Type InlineType { get; protected set; }
        
        public InlineNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            InlineType = dto.GetProperty<Type>(NodePropertyNames.DATA_VALUE);
            Name = $"Make <b><i>{InlineType.Name}</i></b>";
        }

        public override void BuildPins()
        {
            var inData = new BlueprintPin(this, PinNames.IGNORE, PinDirection.In, InlineType, false)
                .WithDisplayName(string.Empty);
            InputPins.Add(PinNames.IGNORE, inData);

            var os = new BlueprintPin(this, PinNames.RETURN, PinDirection.Out, InlineType, false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            OutputPins.Add(PinNames.RETURN, os);
        }
    }

    // public class DataNodeModel<T> : NodeModelBase
    // {
    //     // Serialized
    //     public T Data { get; set; }
    //     
    //     public DataNodeModel(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
    //     {
    //         if(dto.Properties.TryGetValue(NodePropertyNames.DATA_VALUE, out var val))
    //         {
    //             Data = (T)TypeUtility.CastToType(val.Item2, val.Item1);
    //         }
    //     }
    //
    //     public override BlueprintDesignNodeDto Serialize()
    //     {
    //         var dto = base.Serialize();
    //         dto.Properties.TryAdd(NodePropertyNames.DATA_VALUE, (typeof(T), Data));
    //         return dto;
    //     }
    // }
    
    public static class NodeFactory
    {
        public static NodeModelBase Build(NodeType nodeType, Vector2 position, BlueprintMethodGraph graph, params ValueTuple<string, object>[] parameters)
        {
            switch (nodeType)
            {
                case NodeType.Entry:
                    return new EntryNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                case NodeType.Method:
                    var mi = FindParam<MethodInfo>(parameters, SearchModelParams.METHOD_INFO_PARAM);
                    return new MethodNodeModel(graph, BlueprintDesignNodeDto.New(nodeType, position)
                        .WithProperty(NodePropertyNames.K_METHOD_DECLARING_TYPE, mi.DeclaringType)
                        .WithProperty(NodePropertyNames.K_METHOD_NAME, mi.Name)
                        .WithProperty(NodePropertyNames.K_METHOD_PARAMETER_TYPES, mi.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName ?? p.ParameterType.Name).ToArray()));
                case NodeType.MemberAccess:
                    var access = FindParam<VariableAccessType>(parameters, SearchModelParams.VARIABLE_ACCESS_PARAM);
                    if (HasParam(parameters, SearchModelParams.FIELD_INFO_PARAM))
                    {
                        var fi = FindParam<FieldInfo>(parameters, SearchModelParams.FIELD_INFO_PARAM);
                        return new MemberNodeModel(graph, BlueprintDesignNodeDto.New(nodeType, position)
                            .WithProperty(NodePropertyNames.FIELD_TYPE, fi.DeclaringType)
                            .WithProperty(NodePropertyNames.FIELD_NAME, fi.Name)
                            .WithProperty(NodePropertyNames.VARIABLE_ACCESS, access));
                    }

                    var varName = FindParam<string>(parameters, SearchModelParams.VARIABLE_NAME_PARAM);
                    var scope = FindParam<VariableScopeType>(parameters, SearchModelParams.VARIABLE_SCOPE_PARAM);
                    return new MemberNodeModel(graph, BlueprintDesignNodeDto.New(nodeType, position)
                        .WithProperty(NodePropertyNames.VARIABLE_NAME, varName)
                        .WithProperty(NodePropertyNames.VARIABLE_ACCESS, access)
                        .WithProperty(NodePropertyNames.VARIABLE_SCOPE, scope));
                case NodeType.Return:
                    return new ReturnNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                case NodeType.Branch:
                    return new BranchNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                case NodeType.Switch:
                    return new SwitchNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                case NodeType.Sequence:
                    return new SequenceNode(graph, BlueprintDesignNodeDto.New(nodeType, position)
                        .WithProperty(NodePropertyNames.DATA_VALUE, 1));
                case NodeType.For:
                    return new ForNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                case NodeType.ForEach:
                    return new ForEachNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                case NodeType.While:
                    return new WhileNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                case NodeType.Break:
                    return new BreakNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                case NodeType.Continue:
                    return new ContinueNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                case NodeType.Conversion:
                    var tuple = FindParam<(Type, Type)>(parameters, SearchModelParams.DATA_TYPE_PARAM);
                    return new ConversionNode(graph, BlueprintDesignNodeDto.New(nodeType, position)
                        .WithProperty(NodePropertyNames.DATA_VALUE, tuple));
                case NodeType.Cast:
                    return new CastNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                case NodeType.Redirect:
                {
                    var t = FindParam<Type>(parameters, SearchModelParams.DATA_TYPE_PARAM);
                    return new RedirectNode(graph, BlueprintDesignNodeDto.New(nodeType, position)
                        .WithProperty(NodePropertyNames.DATA_VALUE, t));
                }
                case NodeType.Inline:
                {
                    var t = FindParam<Type>(parameters, SearchModelParams.DATA_TYPE_PARAM);
                    return new InlineNode(graph, BlueprintDesignNodeDto.New(nodeType, position)
                        .WithProperty(NodePropertyNames.DATA_VALUE, t));
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, null);
            }
        }

        public static NodeModelBase Build(BlueprintDesignNodeDto dataTransferObject, BlueprintMethodGraph graph)
        {
            NodeModelBase node = dataTransferObject.NodeEnumType switch
            {
                NodeType.Entry => new EntryNode(graph, dataTransferObject),
                NodeType.Method => new MethodNodeModel(graph, dataTransferObject),
                NodeType.MemberAccess => new MemberNodeModel(graph, dataTransferObject),
                NodeType.Return => new ReturnNode(graph, dataTransferObject),
                NodeType.Branch => new BranchNode(graph, dataTransferObject),
                NodeType.Switch => new SwitchNode(graph, dataTransferObject),
                NodeType.Sequence => new SequenceNode(graph, dataTransferObject),
                NodeType.For => new ForNode(graph, dataTransferObject),
                NodeType.ForEach => new ForEachNode(graph, dataTransferObject),
                NodeType.While => new WhileNode(graph, dataTransferObject),
                NodeType.Break => new BreakNode(graph, dataTransferObject),
                NodeType.Continue => new ContinueNode(graph, dataTransferObject),
                NodeType.Conversion => new ConversionNode(graph, dataTransferObject),
                NodeType.Cast => new CastNode(graph, dataTransferObject),
                NodeType.Redirect => new RedirectNode(graph, dataTransferObject),
                NodeType.Inline => new InlineNode(graph, dataTransferObject),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            node.BuildPins();
            
            foreach (var pinDto in dataTransferObject.InputPins)
            {
                var content = TypeUtility.CastToType(pinDto.Content, pinDto.PinType);
                Debug.Log($"Pin: {pinDto.PinName} Content: {content}");
                if (!node.InputPins.TryGetValue(pinDto.PinName, out var inPort) || !inPort.HasInlineValue)
                {
                    continue;
                }

                inPort.Type = pinDto.PinType;
                inPort.SetDefaultValue(content);
                inPort.WireGuids.AddRange(pinDto.WireGuids);
            }

            foreach (var pinDto in dataTransferObject.OutputPins)
            {
                // var content = TypeUtility.CastToType(pinDto.Content, pinDto.PinType);
                // Debug.Log($"Pin: {pinDto.PinName} Content: {content}");
                if (!node.OutputPins.TryGetValue(pinDto.PinName, out var outPort))
                {
                    continue;
                }
                outPort.Type = pinDto.PinType;
                outPort.WireGuids.AddRange(pinDto.WireGuids);
            }

            return node;
        }

        private static bool HasParam(ValueTuple<string, object>[] parameters, string paramName)
        {
            return parameters.Any(p => p.Item1 == paramName);
        }

        private static T FindParam<T>(ValueTuple<string, object>[] parameters, string paramName)
        {
            return (T)parameters.First(p => p.Item1 == paramName).Item2;
        }
    }
}
