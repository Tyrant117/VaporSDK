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
    public abstract class NodeModelBase : IBlueprintGraphModel
    {
        private static readonly Color s_DefaultTextColor = new(0.7568628f, 0.7568628f, 0.7568628f);
        private static readonly Color s_ErrorTextColor = new(0.7568628f, 0f, 0f);
        
        public enum ChangeType
        {
            Deleted,
            Renamed,
            ReTyped,
            InputPins,
            OutputPins,
        }
        
        public BlueprintMethodGraph Method { get; }
        public string Guid { get; private set; }
        public uint Uuid { get; private set; }
        public NodeType NodeType { get; }
        public Rect Position { get; set; }
        // public List<BlueprintWireReference> InputWires { get; }
        // public List<BlueprintWireReference> OutputWires { get; }
        
        // Non-Serialized
        public string Name { get; protected set; }
        public Dictionary<string, BlueprintPin> InputPins { get; } = new();
        public Dictionary<string, BlueprintPin> OutputPins { get; } = new();
        
        // Errors
        public bool HasError { get; protected set; }
        public string ErrorText { get; protected set; }
        
        // Events
        public event Action<NodeModelBase, ChangeType> Changed;

        protected NodeModelBase(BlueprintMethodGraph method, BlueprintDesignNodeDto dto)
        {
            Method = method;
            Guid = dto.Guid;
            Uuid = Guid.GetStableHashU32();
            NodeType = dto.NodeEnumType;
            Position = dto.Position;
        }


        public virtual void BuildPins(){}

        public virtual void PostBuildData()
        {
            Validate();
        }
        
        public virtual void PostConnectWires(){}

        #region - Settings -

        public virtual (string, Color) GetNodeName() => HasError ? (Name, s_ErrorTextColor) : (Name, s_DefaultTextColor);
        public virtual (string, Color, string) GetNodeNameIcon() => HasError ? ("Error", Color.white, ErrorText) : (null, Color.white, string.Empty);
        
        public void SetName(string name)
        {
            Name = name;
            Edited(ChangeType.Renamed);
        }

        public void ResetGuid()
        {
            Guid = System.Guid.NewGuid().ToString();
            Uuid = Guid.GetStableHashU32();
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
            if (!InputPins[PinNames.VALUE_IN].TryGetWire(out var wire))
            {
                return false;
            }
                
            // var wire = InputWires.FirstOrDefault(w => w.RightSidePin.PinName == PinNames.VALUE_IN);
            if (!wire.IsConnected())
            {
                return false;
            }

            if (!Method.Nodes.TryGetValue(wire.LeftGuid, out var node))
            {
                return false;
            }
            
            // var node = Method.Nodes.FirstOrDefault(n => n.Value.Guid == wire.LeftGuid).Value;
            if (node == null)
            {
                return false;
            }
        
            var pin = node.OutputPins[wire.LeftName];
            if (!pin.Type.IsEnum)
            {
                return false;
            }
        
            OutputPins.Clear();
            // OutputWires.Clear();
                        
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
            
            foreach (var port in InputPins.Where(port => !port.Value.IsExecutePin))
            {
                dto.InputPins.Add(new BlueprintPinDto
                {
                    PinName = port.Key,
                    PinType = port.Value.HasInlineValue ? port.Value.InlineValue.GetResolvedType() : port.Value.Type,
                    Content = port.Value.HasInlineValue ? port.Value.InlineValue.Get() : null,
                });
            }

            foreach (var port in OutputPins.Where(port => !port.Value.IsExecutePin))
            {
                dto.OutputPins.Add(new BlueprintPinDto
                {
                    PinName = port.Key,
                    PinType = port.Value.Type,
                    Content = null,
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
                InputPins = new List<BlueprintPinDto>(),
                OutputPins = new List<BlueprintPinDto>(),
                Properties = new Dictionary<string, (Type, object)>(),
            };
            return dto;
        }

        protected void Edited(ChangeType changeType)
        {
            Changed?.Invoke(this, changeType);
            Method.OnUpdateNode(this);
        }

        public void Delete()
        {
            Changed?.Invoke(this, ChangeType.Deleted);
            Method.RemoveNode(this);
        }

        public void OnWireRemoved(BlueprintWire wire)
        {
            foreach (var pin in InputPins.Values)
            {
                pin.Wires.RemoveAll(m => m == wire);
            }

            foreach (var pin in OutputPins.Values)
            {
                pin.Wires.RemoveAll(m => m == wire);
            }
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
                        
                var slot = new BlueprintPin(this, argument.ArgumentName, PinDirection.Out, argument.Type, false)
                    .WithDisplayName(argument.DisplayName)
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
            if(dto.Properties.TryGetValue(NodePropertyNames.METHOD_DECLARING_TYPE, out var val))
            {
                MethodType = (Type)TypeUtility.CastToType(val.Item2, val.Item1);
            }
            if(dto.Properties.TryGetValue(NodePropertyNames.METHOD_NAME, out val))
            {
                MethodName = (string)TypeUtility.CastToType(val.Item2, val.Item1);
            }
            if(dto.Properties.TryGetValue(NodePropertyNames.METHOD_PARAMETER_TYPES, out val))
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
                    var retType = retParam.ParameterType;
                    if (retParam.ParameterType.IsGenericType)
                    {
                        retType = retType.GetGenericTypeDefinition();
                    }
                    
                    var slot = new BlueprintPin(this, PinNames.RETURN, PinDirection.Out, retType, false)
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

                    if (type.IsGenericType)
                    {
                        type = type.GetGenericTypeDefinition();
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

                    var piType = pi.ParameterType;
                    if (pi.ParameterType.IsGenericType)
                    {
                        piType = piType.GetGenericTypeDefinition();
                    }
                    
                    var slot = new BlueprintPin(this, portName, PinDirection.In, piType, false)
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
            dto.Properties.TryAdd(NodePropertyNames.METHOD_DECLARING_TYPE, (typeof(Type), MethodType));
            dto.Properties.TryAdd(NodePropertyNames.METHOD_NAME, (typeof(string), MethodName));
            dto.Properties.TryAdd(NodePropertyNames.METHOD_PARAMETER_TYPES, (typeof(string[]), MethodParameters));
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
        public Type MemberDeclaringType { get; }
        public string FieldName { get; }
        public string PropertyName { get; }
        public string VariableId { get; set; }
        public VariableAccessType VariableAccess { get; }
        public VariableScopeType VariableScope { get; }
        
        // Non-Serialized
        public FieldInfo FieldInfo { get; }
        public PropertyInfo PropertyInfo { get; }
        public BlueprintVariable Variable { get; }

        public MemberNodeModel(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            if (dto.Properties.TryGetValue(NodePropertyNames.VARIABLE_ID, out var val))
            {
                VariableId = (string)TypeUtility.CastToType(val.Item2, val.Item1);
            }

            if (dto.Properties.TryGetValue(NodePropertyNames.VARIABLE_ACCESS, out val))
            {
                VariableAccess = (VariableAccessType)TypeUtility.CastToType(val.Item2, val.Item1);
            }

            if (dto.Properties.TryGetValue(NodePropertyNames.VARIABLE_SCOPE, out val))
            {
                VariableScope = (VariableScopeType)TypeUtility.CastToType(val.Item2, val.Item1);
            }

            if (dto.Properties.TryGetValue(NodePropertyNames.MEMBER_DECLARING_TYPE, out val))
            {
                MemberDeclaringType = (Type)TypeUtility.CastToType(val.Item2, val.Item1);
            }

            if (dto.Properties.TryGetValue(NodePropertyNames.FIELD_NAME, out val))
            {
                FieldName = (string)TypeUtility.CastToType(val.Item2, val.Item1);
            }
            
            if (dto.Properties.TryGetValue(NodePropertyNames.PROPERTY_NAME, out val))
            {
                PropertyName = (string)TypeUtility.CastToType(val.Item2, val.Item1);
            }

            if (MemberDeclaringType != null && !FieldName.EmptyOrNull())
            {
                FieldInfo = RuntimeReflectionUtility.GetFieldInfo(MemberDeclaringType, FieldName);
            }
            
            if (MemberDeclaringType != null && !PropertyName.EmptyOrNull())
            {
                PropertyInfo = RuntimeReflectionUtility.GetPropertyInfo(MemberDeclaringType, PropertyName);
            }

            if (!VariableId.EmptyOrNull())
            {
                if (VariableScope == VariableScopeType.Method)
                {
                    if(method.Variables.TryGetValue(VariableId!, out var linked))
                    {
                        Variable = linked;
                        Variable.Changed += OnLinkedVariableChanged;
                    }
                }
                else
                {
                    if(method.ClassGraphModel.Variables.TryGetValue(VariableId!, out var linked))
                    {
                        Variable = linked;
                        Variable.Changed += OnLinkedVariableChanged;
                    }
                }
            }
        }

        private void OnLinkedVariableChanged(BlueprintVariable variable, BlueprintVariable.ChangeType changeType)
        {
            switch (changeType)
            {
                case BlueprintVariable.ChangeType.Name:
                {
                    string newName = VariableAccess == VariableAccessType.Get ? $"Get <b><i>{variable.DisplayName}</i></b>" : $"Set <b><i>{variable.DisplayName}</i></b>";
                    SetName(newName);
                    break;
                }
                case BlueprintVariable.ChangeType.Type:
                {
                    if (VariableAccess == VariableAccessType.Get)
                    {
                        OutputPins[PinNames.GET_OUT].Type = variable.Type;
                    }
                    else
                    {
                        InputPins[PinNames.GET_OUT].Type = variable.Type;
                        OutputPins[PinNames.GET_OUT].Type = variable.Type;
                    }

                    Edited(ChangeType.ReTyped);
                    break;
                }
                case BlueprintVariable.ChangeType.Delete:
                {
                    Delete();
                    break;
                }
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
            else if (PropertyInfo != null)
            {

                if (VariableAccess == VariableAccessType.Get)
                {
                    Name = ObjectNames.NicifyVariableName(PropertyInfo.Name);

                    // In Pin
                    Debug.Log(PropertyInfo.DeclaringType);
                    var ownerPin = new BlueprintPin(this, PinNames.OWNER, PinDirection.In, PropertyInfo.DeclaringType, true);
                    InputPins.Add(PinNames.OWNER, ownerPin);

                    // Out Pin
                    var returnPin = new BlueprintPin(this, PinNames.GET_OUT, PinDirection.Out, PropertyInfo.PropertyType, false)
                        .WithAllowMultipleWires();
                    OutputPins.Add(PinNames.GET_OUT, returnPin);
                }
                else
                {
                    Name = ObjectNames.NicifyVariableName(PropertyInfo.Name);

                    var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                        .WithDisplayName(string.Empty)
                        .WithAllowMultipleWires();
                    InputPins.Add(PinNames.EXECUTE_IN, inSlot);
                    var outSlot = new BlueprintPin(this, PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                        .WithDisplayName(string.Empty);
                    OutputPins.Add(PinNames.EXECUTE_OUT, outSlot);

                    // In Pin
                    var ownerPin = new BlueprintPin(this, PinNames.OWNER, PinDirection.In, FieldInfo.DeclaringType, true);
                    InputPins.Add(PinNames.OWNER, ownerPin);

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
                    if (!Method.TryGetVariable(VariableScope, VariableId, out var tempData))
                    {
                        Debug.LogError($"{VariableId} not found in graph, was the variable deleted?");
                        SetError($"{VariableId} not found in graph, was the variable deleted?");
                        Name = $"Get <b><i>{VariableId}</i></b>";
                        return;
                    }

                    Name = $"Get <b><i>{tempData.DisplayName}</i></b>";

                    var slot = new BlueprintPin(this, PinNames.GET_OUT, PinDirection.Out, tempData.Type, false)
                        .WithDisplayName(string.Empty)
                        .WithAllowMultipleWires();
                    OutputPins.Add(PinNames.GET_OUT, slot);
                }
                else
                {
                    if (!Method.TryGetVariable(VariableScope, VariableId, out var tempData))
                    {
                        Debug.LogError($"{VariableId} not found in graph, was the variable deleted?");
                        SetError($"{VariableId} not found in graph, was the variable deleted?");
                        Name = $"Set <b><i>{VariableId}</i></b>";
                        return;
                    }

                    Name = $"Set <b><i>{tempData.DisplayName}</i></b>";

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
            dto.Properties.TryAdd(NodePropertyNames.VARIABLE_ID, (typeof(string), VariableId));
            dto.Properties.TryAdd(NodePropertyNames.VARIABLE_ACCESS, (typeof(VariableAccessType), VariableAccess));
            dto.Properties.TryAdd(NodePropertyNames.VARIABLE_SCOPE, (typeof(VariableScopeType), VariableScope));

            if (FieldInfo != null)
            {
                dto.Properties.TryAdd(NodePropertyNames.MEMBER_DECLARING_TYPE, (typeof(Type), MemberDeclaringType));
                dto.Properties.TryAdd(NodePropertyNames.FIELD_NAME, (typeof(string), FieldName));
            }

            if (PropertyInfo != null)
            {
                dto.Properties.TryAdd(NodePropertyNames.MEMBER_DECLARING_TYPE, (typeof(Type), MemberDeclaringType));
                dto.Properties.TryAdd(NodePropertyNames.PROPERTY_NAME, (typeof(string), PropertyName));
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

                var slot = new BlueprintPin(this, argument.ArgumentName, PinDirection.In, argument.Type, false)
                    .WithDisplayName(argument.DisplayName);
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
        public Type CurrentEnumType { get; set; }
        public List<string> Cases { get; set; } = new();
        
        public SwitchNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            Name = "Switch";
            var tuple = dto.GetProperty<(Type, List<string>)>(NodePropertyNames.DATA_VALUE);
            CurrentEnumType = tuple.Item1;
            Cases = new List<string>(tuple.Item2);
        }

        public override void BuildPins()
        {
            var inSlot = new BlueprintPin(this, PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName("")
                .WithAllowMultipleWires();
            InputPins.Add(PinNames.EXECUTE_IN, inSlot);

            var slot = new BlueprintPin(this, PinNames.VALUE_IN, PinDirection.In, typeof(EnumPin), true)
                .WithWildcardTypes(new[] { typeof(EnumPin), typeof(int), typeof(string) });
            InputPins.Add(PinNames.VALUE_IN, slot);

            var defaultSlot = new BlueprintPin(this, PinNames.DEFAULT_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(PinNames.DEFAULT_OUT);
            OutputPins.Add(PinNames.DEFAULT_OUT, defaultSlot);

            foreach (var @case in Cases)
            {
                var casePin = new BlueprintPin(this, @case, PinDirection.Out, typeof(ExecutePin), false)
                    .WithDisplayName(ObjectNames.NicifyVariableName(@case));
                OutputPins.Add(@case, casePin);
            }
        }

        public void UpdateCases()
        {
            if (!InputPins[PinNames.VALUE_IN].TryGetWire(out var wire) || !wire.IsConnected())
            {
                return;
            }

            if (!Method.Nodes.TryGetValue(wire.LeftGuid, out var node))
            {
                return;
            }

            var pin = node.OutputPins[wire.LeftName];
            if (CurrentEnumType == pin.Type)
            {
                return;
            }
            
            InputPins[PinNames.VALUE_IN].Type = pin.Type;
            foreach (var c in Cases)
            {
                if (!OutputPins.Remove(c, out var pinRemoved))
                {
                    continue;
                }

                var copy = pinRemoved.Wires.ToArray();
                foreach (var w in copy)
                {
                    w.Delete();
                }
            }

            Cases.Clear();
            Edited(ChangeType.ReTyped);
            Edited(ChangeType.OutputPins);
        }

        public override BlueprintDesignNodeDto BeginSerialize()
        {
            var dto = base.BeginSerialize();
            dto.Properties.TryAdd(NodePropertyNames.DATA_VALUE, (typeof((Type, List<string>)), (CurrentEnumType, Cases)));
            return dto;
        }

        public void AddCase(string enumName)
        {
            if (OutputPins.ContainsKey(enumName))
            {
                return;
            }

            var enumSlot = new BlueprintPin(this, enumName, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(ObjectNames.NicifyVariableName(enumName));
            OutputPins.Add(enumName, enumSlot);
            Cases.Add(enumName);
            Edited(ChangeType.OutputPins);
        }

        public void RemoveCase(string enumName)
        {
            if (!OutputPins.Remove(enumName, out var pinRemoved))
            {
                return;
            }

            var copy = pinRemoved.Wires.ToArray();
            foreach (var w in copy)
            {
                w.Delete();
            }
            Cases.Remove(enumName);
            Edited(ChangeType.OutputPins);
        }

        public void UpdateCase(string oldCase, string newCase)
        {
            if (!OutputPins.Remove(oldCase, out var pin))
            {
                return;
            }

            pin.RenamePort(newCase);
            pin.WithDisplayName(ObjectNames.NicifyVariableName(newCase));
            var idx = Cases.IndexOf(oldCase);
            Cases[idx] = newCase;
            OutputPins[newCase] = pin;
            Edited(ChangeType.OutputPins);
        }

        public void ClearCases()
        {
            foreach (var c in Cases)
            {
                if (!OutputPins.Remove(c, out var pinRemoved))
                {
                    continue;
                }

                var copy = pinRemoved.Wires.ToArray();
                foreach (var w in copy)
                {
                    w.Delete();
                }
            }

            Cases.Clear();
            InputPins[PinNames.VALUE_IN].Type = CurrentEnumType;
            Edited(ChangeType.ReTyped);
            Edited(ChangeType.OutputPins);
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
        
        public override BlueprintDesignNodeDto BeginSerialize()
        {
            var dto = base.BeginSerialize();
            dto.Properties.TryAdd(NodePropertyNames.DATA_VALUE, (typeof(int), SequenceCount));
            return dto;
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
                .WithDisplayName("First");
            InputPins.Add(PinNames.START_INDEX_IN, startIndexPin);

            var endIndexPin = new BlueprintPin(this, PinNames.LAST_INDEX_IN, PinDirection.In, typeof(int), false)
                .WithDisplayName("Last");
            InputPins.Add(PinNames.LAST_INDEX_IN, endIndexPin);

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

        public override BlueprintDesignNodeDto BeginSerialize()
        {
            var dto = base.BeginSerialize();
            dto.Properties.TryAdd(NodePropertyNames.DATA_VALUE, (typeof((Type, Type)), (TypeIn, TypeOut)));
            return dto;
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
            var castName = OutputPins[PinNames.AS_OUT].Type != typeof(GenericPin) ? TypeSelectorField.GetReadableTypeName(OutputPins[PinNames.AS_OUT].Type) : "T";
            Name = $"Cast<{castName}>";
            
            Validate();
        }
        
        public override BlueprintDesignNodeDto BeginSerialize()
        {
            var outType = OutputPins[PinNames.AS_OUT].Type;
            var dto = base.BeginSerialize();
            dto.Properties.TryAdd(NodePropertyNames.DATA_VALUE, (typeof(Type), outType));
            return dto;
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

        public override BlueprintDesignNodeDto BeginSerialize()
        {
            var dto = base.BeginSerialize();
            dto.Properties.TryAdd(NodePropertyNames.DATA_VALUE, (typeof(Type), RedirectType));
            return dto;
        }
    }

    public class ConstructorNode : NodeModelBase
    {
        public Type TypeToConstruct { get; protected set; }
        public ConstructorInfo ConstructorInfo { get; protected set; }
        public string CurrentConstructorSignature { get; protected set; }
        public bool IsArray { get; protected set; }
        public Type FinalizedType { get; set; }
        public Dictionary<Type, BlueprintPin> GenericArgumentPortMap { get; } = new();
        
        public ConstructorNode(BlueprintMethodGraph method, BlueprintDesignNodeDto dto) : base(method, dto)
        {
            var tuple = dto.GetProperty<ConstructorSearchData>(NodePropertyNames.DATA_VALUE);
            TypeToConstruct = tuple.TypeToConstruct;
            CurrentConstructorSignature = tuple.ConstructorSignature;
            IsArray = tuple.IsArray;
            ConstructorInfo = BlueprintEditorUtility.GetConstructor(TypeToConstruct, CurrentConstructorSignature);
        }

        public override void BuildPins()
        {
            CreateConstructorPins();

            if (TypeToConstruct.IsGenericType)
            {
                var os = new BlueprintPin(this, PinNames.RETURN, PinDirection.Out, typeof(UndefinedPin), false)
                    .WithDisplayName("New")
                    .WithAllowMultipleWires();
                OutputPins.Add(PinNames.RETURN, os);
            }
            else
            {
                var os = new BlueprintPin(this, PinNames.RETURN, PinDirection.Out, TypeToConstruct, false)
                    .WithDisplayName("New")
                    .WithAllowMultipleWires();
                OutputPins.Add(PinNames.RETURN, os);
            }
        }

        public override void PostBuildData()
        {
            FinalizedType = IsArray ? OutputPins[PinNames.RETURN].Type.GetElementType() : OutputPins[PinNames.RETURN].Type;
            string arrayBrackets = IsArray ? "[]" : string.Empty;
            string nm = FinalizedType == typeof(UndefinedPin) ? BlueprintEditorUtility.FormatTypeName(TypeToConstruct) : BlueprintEditorUtility.FormatTypeName(FinalizedType);
            Name = $"Construct <b><i>{nm}{arrayBrackets}</i></b>";
            base.PostBuildData();
        }

        private void CreateConstructorPins()
        {
            InputPins.Clear();
            if (ConstructorInfo != null)
            {
                foreach(var pi in ConstructorInfo.GetParameters())
                {
                    string portName = pi.Name;
                    var displayName = ObjectNames.NicifyVariableName(pi.Name);

                    var piType = pi.ParameterType;
                    if (pi.ParameterType.IsGenericType)
                    {
                        piType = piType.GetGenericTypeDefinition();
                    }

                    var slot = new BlueprintPin(this, portName, PinDirection.In, piType, false)
                        .WithDisplayName(displayName)
                        .WithIsOptional();

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
            else
            {
                string portName = TypeToConstruct.Name;
                var displayName = ObjectNames.NicifyVariableName(BlueprintEditorUtility.FormatTypeName(TypeToConstruct));

                var piType = TypeToConstruct;
                if (TypeToConstruct.IsGenericType)
                {
                    piType = TypeToConstruct.GetGenericTypeDefinition();
                }
                
                var slot = new BlueprintPin(this, PinNames.VALUE_IN, PinDirection.In, piType, false)
                    .WithDisplayName(displayName)
                    .WithIsOptional();

                InputPins.Add(portName, slot);
                if (slot.IsGenericPin)
                {
                    slot.GenericPinType = TypeToConstruct;
                    GenericArgumentPortMap.Add(TypeToConstruct, slot);
                }
            }
        }

        public override BlueprintDesignNodeDto BeginSerialize()
        {
            var dto = base.BeginSerialize();
            var sd = new ConstructorSearchData
            {
                TypeToConstruct = TypeToConstruct,
                ConstructorSignature = CurrentConstructorSignature,
                IsArray = IsArray,
            };
            dto.Properties.TryAdd(NodePropertyNames.DATA_VALUE, (typeof(ConstructorSearchData), sd));
            return dto;
        }

        public void UpdateConstructor(string constructorSignature, ConstructorInfo constructor)
        {
            if (CurrentConstructorSignature == constructorSignature)
            {
                return;
            }
            
            CurrentConstructorSignature = constructorSignature;
            if (CurrentConstructorSignature == "Default(T)")
            {
                ConstructorInfo = null;
            }
            else
            {
                ConstructorInfo = constructor;
            }
            CreateConstructorPins();
        }

        public void SetIsArray(bool isArray)
        {
            IsArray = isArray;
        }

        public Type GetConstructedType()
        {
            if (IsArray)
            {
                return FinalizedType.MakeArrayType();
            }
            else
            {
                return FinalizedType;
            }
        }
    }
    
    public static class NodeFactory
    {
        public static NodeModelBase Build(NodeType nodeType, Vector2 position, BlueprintMethodGraph graph, object userData)
        {
            NodeModelBase returnNode = null;
            switch (nodeType)
            {
                case NodeType.Entry:
                {
                    returnNode = new EntryNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                    break;
                }
                case NodeType.Method:
                {
                    var mi = (MethodInfo)userData;
                    returnNode = new MethodNodeModel(graph, BlueprintDesignNodeDto.New(nodeType, position)
                        .WithProperty(NodePropertyNames.METHOD_DECLARING_TYPE, mi.DeclaringType)
                        .WithProperty(NodePropertyNames.METHOD_NAME, mi.Name)
                        .WithProperty(NodePropertyNames.METHOD_PARAMETER_TYPES, mi.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName ?? p.ParameterType.Name).ToArray()));
                    break;
                }
                case NodeType.MemberAccess:
                {
                    var data = (MemberSearchData)userData;
                    if (data.FieldInfo != null)
                    {
                        var fi = data.FieldInfo;
                        returnNode = new MemberNodeModel(graph, BlueprintDesignNodeDto.New(nodeType, position)
                            .WithProperty(NodePropertyNames.MEMBER_DECLARING_TYPE, fi.DeclaringType)
                            .WithProperty(NodePropertyNames.FIELD_NAME, fi.Name)
                            .WithProperty(NodePropertyNames.VARIABLE_ACCESS, data.AccessType));
                    }
                    else if (data.PropertyInfo != null)
                    {
                        var pi = data.PropertyInfo;
                        returnNode = new MemberNodeModel(graph, BlueprintDesignNodeDto.New(nodeType, position)
                            .WithProperty(NodePropertyNames.MEMBER_DECLARING_TYPE, pi.DeclaringType)
                            .WithProperty(NodePropertyNames.PROPERTY_NAME, pi.Name)
                            .WithProperty(NodePropertyNames.VARIABLE_ACCESS, data.AccessType));
                    }
                    else
                    {

                        var varName = data.Id;
                        var scope = data.ScopeType;
                        returnNode = new MemberNodeModel(graph, BlueprintDesignNodeDto.New(nodeType, position)
                            .WithProperty(NodePropertyNames.VARIABLE_ID, varName)
                            .WithProperty(NodePropertyNames.VARIABLE_ACCESS, data.AccessType)
                            .WithProperty(NodePropertyNames.VARIABLE_SCOPE, scope));
                    }

                    break;
                }
                case NodeType.Return:
                {
                    returnNode = new ReturnNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                    break;
                }
                case NodeType.Branch:
                {
                    returnNode = new BranchNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                    break;
                }
                case NodeType.Switch:
                {
                    returnNode = new SwitchNode(graph, BlueprintDesignNodeDto.New(nodeType, position)
                        .WithProperty(NodePropertyNames.DATA_VALUE, (typeof(EnumPin), new List<string>())));
                    break;
                }
                case NodeType.Sequence:
                {
                    returnNode = new SequenceNode(graph, BlueprintDesignNodeDto.New(nodeType, position)
                        .WithProperty(NodePropertyNames.DATA_VALUE, 1));
                    break;
                }
                case NodeType.For:
                {
                    returnNode = new ForNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                    break;
                }
                case NodeType.ForEach:
                {
                    returnNode = new ForEachNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                    break;
                }
                case NodeType.While:
                {
                    returnNode = new WhileNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                    break;
                }
                case NodeType.Break:
                {
                    returnNode = new BreakNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                    break;
                }
                case NodeType.Continue:
                {
                    returnNode = new ContinueNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                    break;
                }
                case NodeType.Conversion:
                {
                    var tuple = (ValueTuple<Type, Type>)userData;
                    Debug.Log(tuple);
                    returnNode = new ConversionNode(graph, BlueprintDesignNodeDto.New(nodeType, position)
                        .WithProperty(NodePropertyNames.DATA_VALUE, tuple));
                    break;
                }
                case NodeType.Cast:
                {
                    returnNode = new CastNode(graph, BlueprintDesignNodeDto.New(nodeType, position));
                    break;
                }
                case NodeType.Redirect:
                {
                    var t = (Type)userData;
                    returnNode = new RedirectNode(graph, BlueprintDesignNodeDto.New(nodeType, position)
                        .WithProperty(NodePropertyNames.DATA_VALUE, t));
                    break;
                }
                case NodeType.Constructor:
                {
                    var t = (ConstructorSearchData)userData;
                    returnNode = new ConstructorNode(graph, BlueprintDesignNodeDto.New(nodeType, position)
                        .WithProperty(NodePropertyNames.DATA_VALUE, t));
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, null);
            }
            
            returnNode.BuildPins();
            return returnNode;
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
                NodeType.Constructor => new ConstructorNode(graph, dataTransferObject),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            node.BuildPins();
            
            foreach (var pinDto in dataTransferObject.InputPins)
            {
                if (!node.InputPins.TryGetValue(pinDto.PinName, out var inPort))
                {
                    continue;
                }
                
                inPort.Type = pinDto.PinType;
                if (inPort.HasInlineValue)
                {
                    var content = TypeUtility.CastToType(pinDto.Content, pinDto.PinType);
                    Debug.Log($"Pin: {pinDto.PinName} Type: {pinDto.PinType} Content: {content}");
                    inPort.SetDefaultValue(content);
                }
                else
                {
                    Debug.Log($"Pin: {pinDto.PinName} Type: {pinDto.PinType}");
                }
            }

            foreach (var pinDto in dataTransferObject.OutputPins)
            {
                if (!node.OutputPins.TryGetValue(pinDto.PinName, out var outPort))
                {
                    continue;
                }
                outPort.Type = pinDto.PinType;
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
