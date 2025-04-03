using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Vapor.Inspector;
using VaporEditor.Blueprints;
using VaporEditor.Inspector;

namespace Vapor.Blueprints
{
    public class BlueprintNodeController
    {
        private static readonly Color s_DefaultTextColor = new(0.7568628f, 0.7568628f, 0.7568628f);
        private static readonly Color s_ErrorTextColor = new(0.7568628f, 0f, 0f);
        public BlueprintMethodGraph Graph { get; protected set; }
        public NodeModelBase Model { get; protected set; }
        public string NodeName { get; set; }
        public Dictionary<string, BlueprintPin> InPorts { get; } = new();
        public Dictionary<string, BlueprintPin> OutPorts { get; } = new();
        public Dictionary<Type, BlueprintPin> GenericArgumentPortMap { get; } = new();

        // Validation
        public bool HasError { get; private set; }
        public string ErrorText { get; private set; }

        public virtual void PostBuild()
        {
            Validate();
        }

        public (string, Color) GetNodeName()
        {
            return HasError ? (NodeName, s_ErrorTextColor) : (NodeName, s_DefaultTextColor);
        }
        public (string, Color, string) GetNodeNameIcon() => HasError ? ("Error", Color.white, ErrorText) : (null, Color.white, string.Empty);

        public void SetError(string errorText)
        {
            HasError = true;
            ErrorText = errorText;
        }

        public void ClearError()
        {
            HasError = false;
            ErrorText = string.Empty;
        }

        public T ModelAs<T>() where T : NodeModelBase
        {
            return (T)Model;
        }

        protected void Validate()
        {
            Model.InputWires.RemoveAll(w => Graph.Nodes.FindIndex(n => n.Model.Guid == w.LeftSidePin.NodeGuid) == -1);
            Model.OutputWires.RemoveAll(w => Graph.Nodes.FindIndex(n => n.Model.Guid == w.RightSidePin.NodeGuid) == -1);
        }

        #region - Serialization -
        public BlueprintDesignNodeDto Serialize()
        {
            var dto = Model.Serialize();
            foreach (var port in InPorts.Where(port => !port.Value.IsExecutePin)
                         .Where(port => port.Value.HasInlineValue))
            {
                dto.InputPins.Add(new BlueprintPinDto
                {
                    PinName = port.Key,
                    PinType = port.Value.InlineValue.GetResolvedType(),
                    Content = port.Value.InlineValue.Get(),
                });
            }
            
            foreach (var port in OutPorts.Where(port => !port.Value.IsExecutePin))
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

        public BlueprintBaseNode ConvertToRuntime()
        {
            var dto = Serialize();
            return RuntimeCompiler.Compile(dto);
        }
        #endregion

        #region - Helpers -
        public string FormatWithUuid(string prefix) => $"{prefix}_{Model.Uuid}";
        
        #endregion

        public bool OnEnumChanged()
        {
            var wire = Model.InputWires.FirstOrDefault(w => w.RightSidePin.PinName == PinNames.VALUE_IN);
            if (!wire.IsValid())
            {
                return false;
            }
            
            var node = Graph.Nodes.FirstOrDefault(n => n.Model.Guid == wire.LeftSidePin.NodeGuid);
            if (node == null)
            {
                return false;
            }

            var pin = node.OutPorts[wire.LeftSidePin.PinName];
            if (!pin.Type.IsEnum)
            {
                return false;
            }

            OutPorts.Clear();
            Model.OutputWires.Clear();
                        
            var defaultSlot = new BlueprintPin(PinNames.DEFAULT_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(PinNames.DEFAULT_OUT);
            OutPorts.Add(PinNames.DEFAULT_OUT, defaultSlot);
                        
            var enumNames = pin.Type.GetEnumNames();
            foreach (var name in enumNames)
            {
                var enumSlot = new BlueprintPin(name, PinDirection.Out, typeof(ExecutePin), false)
                    .WithDisplayName(name);
                OutPorts.Add(name, enumSlot);
            }
            return true;
        }
    }
    public class BlueprintNodeController<T> : BlueprintNodeController where T : NodeModelBase
    {
        
        
        public BlueprintNodeController(T model, BlueprintMethodGraph graph)
        {
            Model = model;
            Graph = graph;
            
            GenerateData();
        }

        #region - Data -
        private void GenerateData()
        {
            switch (Model.NodeType)
            {
                case NodeType.Entry:
                {
                    NodeName = "Entry";
                    var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                        .WithDisplayName("");
                    OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);

                    foreach (var parameter in Graph.InputArguments)
                    {
                        var slot = new BlueprintPin(parameter.Name, PinDirection.Out, parameter.Type, false)
                            .WithAllowMultipleWires();
                        OutPorts.Add(parameter.Name, slot);
                    }
                    break;
                }
                case NodeType.Method:
                {
                    var methodInfo = ModelAs<MethodNodeModel>().MethodInfo;
                    var paramInfos = methodInfo.GetParameters();
                    var genericArgs = methodInfo.GetGenericArguments();
                    bool hasOutParameter = paramInfos.Any(p => p.IsOut);
                    var callableAttribute = methodInfo.GetCustomAttribute<BlueprintCallableAttribute>();

                    var nodeName = methodInfo.IsSpecialName ? GetMethodSignature().ToTitleCase() : GetMethodSignature();
                    nodeName = methodInfo.IsStatic ? $"{methodInfo.DeclaringType!.Name}.{nodeName}" : nodeName;
                    NodeName = callableAttribute == null || callableAttribute.NodeName.EmptyOrNull() ? nodeName : callableAttribute.NodeName;

                    if (!ModelAs<MethodNodeModel>().IsPure)
                    {
                        var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                            .WithDisplayName(string.Empty)
                            .WithAllowMultipleWires();
                        InPorts.Add(PinNames.EXECUTE_IN, inSlot);
                        var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                            .WithDisplayName(string.Empty);
                        OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);
                    }

                    if (!methodInfo.IsStatic)
                    {
                        var slot = new BlueprintPin(PinNames.OWNER, PinDirection.In, methodInfo.DeclaringType, false);
                        InPorts.Add(PinNames.OWNER, slot);
                    }

                    if (methodInfo.ReturnType != typeof(void))
                    {
                        var retParam = methodInfo.ReturnParameter;
                        if (retParam is { IsRetval: true })
                        {
                            // Out Ports
                            var slot = new BlueprintPin(PinNames.RETURN, PinDirection.Out, retParam.ParameterType, false)
                                .WithAllowMultipleWires();
                            OutPorts.Add(PinNames.RETURN, slot);

                            if (slot.IsGenericPin)
                            {
                                slot.GenericPinType = retParam.ParameterType;//genericArgs[0];
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

                            var slot = new BlueprintPin(portName, PinDirection.Out, type, false)
                                .WithDisplayName(displayName)
                                .WithAllowMultipleWires();
                            if (isWildcard)
                            {
                                slot.WithWildcardTypes(paramAttribute.WildcardTypes);
                            }

                            OutPorts.Add(portName, slot);
                            if (slot.IsGenericPin)
                            {
                                slot.GenericPinType = type;//genericArgs[genArgCount];
                                GenericArgumentPortMap.Add(type, slot);
                                // genArgCount++;
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

                            var slot = new BlueprintPin(portName, PinDirection.In, pi.ParameterType, false)
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

                            InPorts.Add(portName, slot);
                            if (slot.IsGenericPin)
                            {
                                slot.GenericPinType = pi.ParameterType;//genericArgs[genArgCount];
                                GenericArgumentPortMap.Add(pi.ParameterType, slot);
                                // genArgCount++;
                            }
                        }
                    }
                    break;
                }
                case NodeType.MemberAccess:
                {
                    var model = ModelAs<MemberNodeModel>();
                    if (model.FieldInfo != null)
                    {
                        if (model.VariableAccess == VariableAccessType.Get)
                        {
                            NodeName = ObjectNames.NicifyVariableName(model.FieldInfo.Name);

                            // In Pin
                            if (!model.FieldInfo.IsStatic)
                            {
                                var ownerPin = new BlueprintPin(PinNames.OWNER, PinDirection.In, model.FieldInfo.DeclaringType, true);
                                InPorts.Add(PinNames.OWNER, ownerPin);
                            }

                            // Out Pin
                            var returnPin = new BlueprintPin(PinNames.GET_OUT, PinDirection.Out, model.FieldInfo.FieldType, false)
                                .WithAllowMultipleWires();
                            OutPorts.Add(PinNames.GET_OUT, returnPin);
                        }
                        else
                        {
                            NodeName = ObjectNames.NicifyVariableName(model.FieldInfo.Name);
            
                            var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                                .WithDisplayName(string.Empty)
                                .WithAllowMultipleWires();
                            InPorts.Add(PinNames.EXECUTE_IN, inSlot);
                            var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                                .WithDisplayName(string.Empty);
                            OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);

                            // In Pin
                            if(!model.FieldInfo.IsStatic)
                            {
                                var ownerPin = new BlueprintPin(PinNames.OWNER, PinDirection.In, model.FieldInfo.DeclaringType, true);
                                InPorts.Add(PinNames.OWNER, ownerPin);
                            }

                            // Set Pin
                            var setterPin = new BlueprintPin(PinNames.SET_IN, PinDirection.In, model.FieldInfo.FieldType, false);
                            InPorts.Add(PinNames.SET_IN, setterPin);
                            
                            // Out Pin
                            var returnPin = new BlueprintPin(PinNames.GET_OUT, PinDirection.Out, model.FieldInfo.FieldType, false)
                                .WithAllowMultipleWires();
                            OutPorts.Add(PinNames.GET_OUT, returnPin);
                        }
                    }
                    else
                    {
                        if (model.VariableAccess == VariableAccessType.Get)
                        {
                            if (!Graph.TryGetVariable(model.VariableScope, model.VariableName, out var tempData))
                            {
                                Debug.LogError($"{model.VariableName} not found in graph, was the variable deleted?");
                                SetError($"{model.VariableName} not found in graph, was the variable deleted?");
                                NodeName = $"Get <b><i>{model.VariableName}</i></b>";
                                return;
                            }

                            NodeName = $"Get <b><i>{tempData.Name}</i></b>";

                            var slot = new BlueprintPin(PinNames.GET_OUT, PinDirection.Out, tempData.Type, false)
                                .WithDisplayName(string.Empty)
                                .WithAllowMultipleWires();
                            OutPorts.Add(PinNames.GET_OUT, slot);
                        }
                        else
                        {
                            if (!Graph.TryGetVariable(model.VariableScope, model.VariableName, out var tempData))
                            {
                                Debug.LogError($"{model.VariableName} not found in graph, was the variable deleted?");
                                SetError($"{model.VariableName} not found in graph, was the variable deleted?");
                                NodeName = $"Set <b><i>{model.VariableName}</i></b>";
                                return;
                            }
            
                            NodeName = $"Set <b><i>{tempData.Name}</i></b>";
            
                            var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                                .WithDisplayName(string.Empty)
                                .WithAllowMultipleWires();
                            InPorts.Add(PinNames.EXECUTE_IN, inSlot);

                            var inData = new BlueprintPin(PinNames.SET_IN, PinDirection.In, tempData.Type, false)
                                .WithDisplayName(string.Empty);
                            InPorts.Add(PinNames.SET_IN, inData);

                            var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                                .WithDisplayName(string.Empty);
                            OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);

                            var os = new BlueprintPin(PinNames.GET_OUT, PinDirection.Out, tempData.Type, false)
                                .WithDisplayName(string.Empty)
                                .WithAllowMultipleWires();
                            OutPorts.Add(PinNames.GET_OUT, os);
                        }
                    }
                    break;
                }
                case NodeType.Return:
                {
                    NodeName = "Return";
                    var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                        .WithDisplayName("")
                        .WithAllowMultipleWires();
                    InPorts.Add(PinNames.EXECUTE_IN, inSlot);

                    foreach (var parameter in Graph.OutputArguments)
                    {
                        var slot = new BlueprintPin(parameter.Name, PinDirection.In, parameter.Type, false);
                        InPorts.Add(parameter.Name, slot);
                    }
                    break;
                }
                case NodeType.Branch:
                {
                    NodeName = "Branch";
                    var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                        .WithDisplayName("")
                        .WithAllowMultipleWires();
                    InPorts.Add(PinNames.EXECUTE_IN, inSlot);

                    var slot = new BlueprintPin(PinNames.VALUE_IN, PinDirection.In, typeof(bool), false);
                    InPorts.Add(PinNames.VALUE_IN, slot);

                    var trueSlot = new BlueprintPin(PinNames.TRUE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                        .WithDisplayName(PinNames.TRUE_OUT);
                    OutPorts.Add(PinNames.TRUE_OUT, trueSlot);

                    var falseSlot = new BlueprintPin(PinNames.FALSE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                        .WithDisplayName(PinNames.FALSE_OUT);
                    OutPorts.Add(PinNames.FALSE_OUT, falseSlot);
                    break;
                }
                case NodeType.Switch:
                {
                    NodeName = "Switch";
                    var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                        .WithDisplayName("")
                        .WithAllowMultipleWires();
                    InPorts.Add(PinNames.EXECUTE_IN, inSlot);

                    var slot = new BlueprintPin(PinNames.VALUE_IN, PinDirection.In, typeof(Enum), false).WithWildcardTypes(new[] { typeof(Enum), typeof(int), typeof(string) });
                    InPorts.Add(PinNames.VALUE_IN, slot);
                    
                    var defaultSlot = new BlueprintPin(PinNames.DEFAULT_OUT, PinDirection.Out, typeof(ExecutePin), false)
                        .WithDisplayName(PinNames.DEFAULT_OUT);
                    OutPorts.Add(PinNames.DEFAULT_OUT, defaultSlot);
                    break;
                }
                case NodeType.Sequence:
                {
                    NodeName = "Sequence";
                    var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                        .WithDisplayName("")
                        .WithAllowMultipleWires();
                    InPorts.Add(PinNames.EXECUTE_IN, inSlot);

                    var count = ModelAs<DataNodeModel<int>>().Data;
                    for (int i = 0; i < count; i++)
                    {
                        string formattedName = $"{PinNames.SEQUENCE_OUT}_{i}";
                        var pin = new BlueprintPin(formattedName, PinDirection.Out, typeof(ExecutePin), false)
                            .WithDisplayName(formattedName);
                        OutPorts.Add(formattedName, pin);
                    }
                    break;
                }
                case NodeType.For:
                {
                    NodeName = "For";
                    var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                        .WithDisplayName(string.Empty)
                        .WithAllowMultipleWires();
                    InPorts.Add(PinNames.EXECUTE_IN, inSlot);

                    var arraySlot = new BlueprintPin(PinNames.ARRAY_IN, PinDirection.In, typeof(ICollection), false)
                        .WithDisplayName("Array");
                    InPorts.Add(PinNames.ARRAY_IN, arraySlot);
                    
                    var startIndexPin = new BlueprintPin(PinNames.START_INDEX_IN, PinDirection.In, typeof(int), false)
                        .WithDisplayName("Start");
                    InPorts.Add(PinNames.START_INDEX_IN, startIndexPin);
                    
                    var endIndexPin = new BlueprintPin(PinNames.LENGTH_IN, PinDirection.In, typeof(int), false)
                        .WithDisplayName("Length");
                    InPorts.Add(PinNames.LENGTH_IN, endIndexPin);

                    var loopSlot = new BlueprintPin(PinNames.LOOP_OUT, PinDirection.Out, typeof(ExecutePin), false)
                        .WithDisplayName("Loop");
                    OutPorts.Add(PinNames.LOOP_OUT, loopSlot);

                    var indexSlot = new BlueprintPin(PinNames.INDEX_OUT, PinDirection.Out, typeof(int), false)
                        .WithDisplayName("Index");
                    OutPorts.Add(PinNames.INDEX_OUT, indexSlot);

                    var elementSlot = new BlueprintPin(PinNames.ELEMENT_OUT, PinDirection.Out, typeof(object), false)
                        .WithDisplayName("Element");
                    OutPorts.Add(PinNames.ELEMENT_OUT, elementSlot);

                    var completedSlot = new BlueprintPin(PinNames.COMPLETE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                        .WithDisplayName("Complete");
                    OutPorts.Add(PinNames.COMPLETE_OUT, completedSlot);
                    break;
                }
                case NodeType.ForEach:
                {
                    NodeName = "ForEach";
                    var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                        .WithDisplayName(string.Empty)
                        .WithAllowMultipleWires();
                    InPorts.Add(PinNames.EXECUTE_IN, inSlot);

                    var arraySlot = new BlueprintPin(PinNames.ARRAY_IN, PinDirection.In, typeof(IEnumerable), false)
                        .WithDisplayName("Array");
                    InPorts.Add(PinNames.ARRAY_IN, arraySlot);

                    var loopSlot = new BlueprintPin(PinNames.LOOP_OUT, PinDirection.Out, typeof(ExecutePin), false)
                        .WithDisplayName("Loop");
                    OutPorts.Add(PinNames.LOOP_OUT, loopSlot);

                    var indexSlot = new BlueprintPin(PinNames.INDEX_OUT, PinDirection.Out, typeof(int), false)
                        .WithDisplayName("Index");
                    OutPorts.Add(PinNames.INDEX_OUT, indexSlot);

                    var elementSlot = new BlueprintPin(PinNames.ELEMENT_OUT, PinDirection.Out, typeof(object), false)
                        .WithDisplayName("Element");
                    OutPorts.Add(PinNames.ELEMENT_OUT, elementSlot);

                    var completedSlot = new BlueprintPin(PinNames.COMPLETE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                        .WithDisplayName("Complete");
                    OutPorts.Add(PinNames.COMPLETE_OUT, completedSlot);
                    break;
                }
                case NodeType.While:
                {
                    NodeName = "While";
                    var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                        .WithDisplayName(string.Empty)
                        .WithAllowMultipleWires();
                    InPorts.Add(PinNames.EXECUTE_IN, inSlot);

                    var whileTruePin = new BlueprintPin(PinNames.VALUE_IN, PinDirection.In, typeof(bool), false)
                        .WithDisplayName("Condition");
                    InPorts.Add(PinNames.VALUE_IN, whileTruePin);

                    var loopSlot = new BlueprintPin(PinNames.LOOP_OUT, PinDirection.Out, typeof(ExecutePin), false)
                        .WithDisplayName("Loop");
                    OutPorts.Add(PinNames.LOOP_OUT, loopSlot);

                    var completedSlot = new BlueprintPin(PinNames.COMPLETE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                        .WithDisplayName("Complete");
                    OutPorts.Add(PinNames.COMPLETE_OUT, completedSlot);
                    break;
                }
                case NodeType.Break:
                {
                    NodeName = "Break";
                    var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                        .WithDisplayName(string.Empty)
                        .WithAllowMultipleWires();
                    InPorts.Add(PinNames.EXECUTE_IN, inSlot);
                    break;
                }
                case NodeType.Continue:
                {
                    NodeName = "Continue";
                    var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                        .WithDisplayName(string.Empty)
                        .WithAllowMultipleWires();
                    InPorts.Add(PinNames.EXECUTE_IN, inSlot);
                    break;
                }
                case NodeType.Conversion:
                {
                    NodeName = string.Empty;
                    var data = ModelAs<DataNodeModel<(Type, Type)>>().Data;

                    var slot = new BlueprintPin(PinNames.SET_IN, PinDirection.In, data.Item1, false)
                        .WithDisplayName(string.Empty);
                    InPorts.Add(PinNames.SET_IN, slot);

                    var outSlot = new BlueprintPin(PinNames.GET_OUT, PinDirection.Out, data.Item2, false)
                        .WithDisplayName(string.Empty)
                        .WithAllowMultipleWires();
                    OutPorts.Add(PinNames.GET_OUT, outSlot);
                    break;
                }
                case NodeType.Cast:
                {
                    NodeName = "Cast";
                    var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                        .WithDisplayName(string.Empty)
                        .WithAllowMultipleWires();
                    InPorts.Add(PinNames.EXECUTE_IN, inSlot);

                    var valuePin = new BlueprintPin(PinNames.VALUE_IN, PinDirection.In, typeof(object), false)
                        .WithDisplayName(PinNames.VALUE_IN);
                    InPorts.Add(PinNames.VALUE_IN, valuePin);
                    
                    // var typePin = new BlueprintPin(PinNames.TYPE_IN, PinDirection.In, typeof(Type), false)
                    //     .WithDisplayName(PinNames.TYPE_IN);
                    // InPorts.Add(PinNames.TYPE_IN, typePin);

                    var validPin = new BlueprintPin(PinNames.VALID_OUT, PinDirection.Out, typeof(ExecutePin), false)
                        .WithDisplayName(PinNames.VALID_OUT);
                    OutPorts.Add(PinNames.VALID_OUT, validPin);

                    var invalidPin = new BlueprintPin(PinNames.INVALID_OUT, PinDirection.Out, typeof(ExecutePin), false)
                        .WithDisplayName(PinNames.INVALID_OUT);
                    OutPorts.Add(PinNames.INVALID_OUT, invalidPin);
                    
                    var castedValuePin = new BlueprintPin(PinNames.AS_OUT, PinDirection.Out, typeof(GenericPin), false)
                        .WithDisplayName(PinNames.AS_OUT);
                    OutPorts.Add(PinNames.AS_OUT, castedValuePin);
                    break;
                }
                case NodeType.Redirect:
                {
                    NodeName = string.Empty;

                    var rerouteType = ModelAs<DataNodeModel<Type>>().Data;
                    if (rerouteType != typeof(ExecutePin))
                    {
                        var slot = new BlueprintPin(PinNames.SET_IN, PinDirection.In, rerouteType, false)
                            .WithDisplayName(string.Empty);
                        InPorts.Add(PinNames.SET_IN, slot);

                        var outSlot = new BlueprintPin(PinNames.GET_OUT, PinDirection.Out, rerouteType, false)
                            .WithDisplayName(string.Empty)
                            .WithAllowMultipleWires();
                        OutPorts.Add(PinNames.GET_OUT, outSlot);
                    }
                    else
                    {
                        var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                            .WithDisplayName(string.Empty)
                            .WithAllowMultipleWires();
                        InPorts.Add(PinNames.EXECUTE_IN, inSlot);
                        var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                            .WithDisplayName(string.Empty);
                        OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);
                    }

                    break;
                }
                case NodeType.Inline:
                {
                    var makeType = ModelAs<DataNodeModel<Type>>().Data;
                    NodeName = $"Make <b><i>{makeType.Name}</i></b>";

                    var inData = new BlueprintPin(PinNames.IGNORE, PinDirection.In, makeType, false)
                        .WithDisplayName(string.Empty);
                    InPorts.Add(PinNames.IGNORE, inData);

                    var os = new BlueprintPin(PinNames.RETURN, PinDirection.Out, makeType, false)
                        .WithDisplayName(string.Empty)
                        .WithAllowMultipleWires();
                    OutPorts.Add(PinNames.RETURN, os);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void PostBuild()
        {
            switch (Model.NodeType)
            {
                case NodeType.Switch:
                {
                    var wire = Model.InputWires.FirstOrDefault(w => w.RightSidePin.PinName == PinNames.VALUE_IN);
                    if (!wire.IsValid())
                    {
                        return;
                    }
            
                    var node = Graph.Nodes.FirstOrDefault(n => n.Model.Guid == wire.LeftSidePin.NodeGuid);
                    if (node == null)
                    {
                        return;
                    }

                    var pin = node.OutPorts[wire.LeftSidePin.PinName];
                    if (!pin.Type.IsEnum)
                    {
                        return;
                    }
                        
                    var enumNames = pin.Type.GetEnumNames();
                    foreach (var name in enumNames)
                    {
                        var enumSlot = new BlueprintPin(name, PinDirection.Out, typeof(ExecutePin), false)
                            .WithDisplayName(name);
                        OutPorts.Add(name, enumSlot);
                    }
                    break;
                }
                case NodeType.Cast:
                    var castName = OutPorts[PinNames.AS_OUT].Type != typeof(UndefinedPin) ? TypeSelectorField.GetReadableTypeName(OutPorts[PinNames.AS_OUT].Type) : "T";
                    NodeName = $"Cast<{castName}>";
                    break;
            }
            
            Validate();
        }

        #endregion

        #region - Helpers -

        private string GetMethodSignature()
        {
            if (Model.NodeType != NodeType.Method)
            {
                return string.Empty;
            }

            var mi = ModelAs<MethodNodeModel>().MethodInfo;
            if (mi == null)
            {
                return string.Empty;
            }
                
            string methodName = mi.Name;
            if (!mi.IsGenericMethod)
            {
                return $"{methodName}";
            }

            var genericArgs = mi.GetGenericArguments();
            var genArgString = genericArgs.Select(t => GenericArgumentPortMap.TryGetValue(t, out var pin) ? pin.Type.Name : t.Name);
            string genericArgsStr = string.Join(", ", genArgString);
            methodName += $"<{genericArgsStr}>";
            return $"{methodName}";
        }

        private static string GetMethodSignature(MethodInfo method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            string methodName = method.Name;

            // Handle generic method parameters (e.g., Method<T1, T2>)
            if (method.IsGenericMethod)
            {
                var genericArgs = method.GetGenericArguments();
                string genericArgsStr = string.Join(", ", genericArgs.Select(t => t.Name));
                methodName += $"<{genericArgsStr}>";
            }

            // // Handle method parameters (e.g., (int, string, List<T1>))
            // var parameters = method.GetParameters();
            // string paramStr = string.Join(", ", parameters.Select(p => GetTypeName(p.ParameterType)));
            // return $"{methodName}({paramStr})";
            
            return $"{methodName}";
        }

        private static string GetTypeName(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.Name;
            }

            string typeName = type.Name.Split('`')[0]; // Remove arity (`1, `2)
            string genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetTypeName));
            return $"{typeName}<{genericArgs}>";
        }

        #endregion
    }

    public static class BlueprintNodeControllerFactory
    {
        public static BlueprintNodeController Build(NodeType nodeType, Vector2 position, BlueprintMethodGraph graph, params ValueTuple<string, object>[] parameters)
        {
            switch (nodeType)
            {
                case NodeType.Entry:
                    return new BlueprintNodeController<NodeModel>(new NodeModel(nodeType, position), graph);
                case NodeType.Method:
                    return new BlueprintNodeController<MethodNodeModel>(new MethodNodeModel(nodeType, position, FindParam<MethodInfo>(parameters, SearchModelParams.METHOD_INFO_PARAM)), graph);
                case NodeType.MemberAccess:
                    if (HasParam(parameters, SearchModelParams.FIELD_INFO_PARAM))
                    {
                        return new BlueprintNodeController<MemberNodeModel>(new MemberNodeModel(nodeType, position,
                            FindParam<FieldInfo>(parameters, SearchModelParams.FIELD_INFO_PARAM),
                            FindParam<VariableAccessType>(parameters, SearchModelParams.VARIABLE_ACCESS_PARAM)), graph);
                    }

                    return new BlueprintNodeController<MemberNodeModel>(new MemberNodeModel(nodeType, position,
                        FindParam<string>(parameters, SearchModelParams.VARIABLE_NAME_PARAM),
                        FindParam<VariableAccessType>(parameters, SearchModelParams.VARIABLE_ACCESS_PARAM),
                        FindParam<VariableScopeType>(parameters, SearchModelParams.VARIABLE_SCOPE_PARAM)), graph);
                case NodeType.Return:
                    return new BlueprintNodeController<NodeModel>(new NodeModel(nodeType, position), graph);
                case NodeType.Branch:
                    return new BlueprintNodeController<NodeModel>(new NodeModel(nodeType, position), graph);
                case NodeType.Switch:
                    return new BlueprintNodeController<NodeModel>(new NodeModel(nodeType, position), graph);
                case NodeType.Sequence:
                    return new BlueprintNodeController<DataNodeModel<int>>(new DataNodeModel<int>(nodeType, position, 1), graph);
                case NodeType.For:
                    return new BlueprintNodeController<NodeModel>(new NodeModel(nodeType, position), graph);
                case NodeType.ForEach:
                    return new BlueprintNodeController<NodeModel>(new NodeModel(nodeType, position), graph);
                case NodeType.While:
                    return new BlueprintNodeController<NodeModel>(new NodeModel(nodeType, position), graph);
                case NodeType.Break:
                    return new BlueprintNodeController<NodeModel>(new NodeModel(nodeType, position), graph);
                case NodeType.Continue:
                    return new BlueprintNodeController<NodeModel>(new NodeModel(nodeType, position), graph);
                case NodeType.Conversion:
                    return new BlueprintNodeController<DataNodeModel<(Type, Type)>>(new DataNodeModel<(Type, Type)>(nodeType, position, FindParam<(Type, Type)>(parameters, SearchModelParams.DATA_TYPE_PARAM)), graph);
                case NodeType.Cast:
                    return new BlueprintNodeController<NodeModel>(new NodeModel(nodeType, position), graph);
                case NodeType.Redirect:
                    return new BlueprintNodeController<DataNodeModel<Type>>(new DataNodeModel<Type>(nodeType, position, FindParam<Type>(parameters, SearchModelParams.DATA_TYPE_PARAM)), graph);
                case NodeType.Inline:
                    return new BlueprintNodeController<DataNodeModel<Type>>(new DataNodeModel<Type>(nodeType, position, FindParam<Type>(parameters, SearchModelParams.DATA_TYPE_PARAM)), graph);
                default:
                    return new BlueprintNodeController<NodeModel>(new NodeModel(nodeType, position), graph);
            }

        }

        public static BlueprintNodeController Build(BlueprintDesignNodeDto dataTransferObject, BlueprintMethodGraph graph)
        {
            BlueprintNodeController controller = dataTransferObject.NodeEnumType switch
            {
                NodeType.Entry => new BlueprintNodeController<NodeModel>(new NodeModel(dataTransferObject), graph),
                NodeType.Method => new BlueprintNodeController<MethodNodeModel>(new MethodNodeModel(dataTransferObject), graph),
                NodeType.MemberAccess => new BlueprintNodeController<MemberNodeModel>(new MemberNodeModel(dataTransferObject), graph),
                NodeType.Return => new BlueprintNodeController<NodeModel>(new NodeModel(dataTransferObject), graph),
                NodeType.Branch => new BlueprintNodeController<NodeModel>(new NodeModel(dataTransferObject), graph),
                NodeType.Switch => new BlueprintNodeController<NodeModel>(new NodeModel(dataTransferObject), graph),
                NodeType.Sequence => new BlueprintNodeController<DataNodeModel<int>>(new DataNodeModel<int>(dataTransferObject), graph),
                NodeType.For => new BlueprintNodeController<NodeModel>(new NodeModel(dataTransferObject), graph),
                NodeType.ForEach => new BlueprintNodeController<NodeModel>(new NodeModel(dataTransferObject), graph),
                NodeType.While => new BlueprintNodeController<NodeModel>(new NodeModel(dataTransferObject), graph),
                NodeType.Break => new BlueprintNodeController<NodeModel>(new NodeModel(dataTransferObject), graph),
                NodeType.Continue => new BlueprintNodeController<NodeModel>(new NodeModel(dataTransferObject), graph),
                NodeType.Conversion => new BlueprintNodeController<DataNodeModel<(Type, Type)>>(new DataNodeModel<(Type, Type)>(dataTransferObject), graph),
                NodeType.Cast => new BlueprintNodeController<NodeModel>(new NodeModel(dataTransferObject), graph),
                NodeType.Redirect => new BlueprintNodeController<DataNodeModel<Type>>(new DataNodeModel<Type>(dataTransferObject), graph),
                NodeType.Inline => new BlueprintNodeController<DataNodeModel<Type>>(new DataNodeModel<Type>(dataTransferObject), graph),
                _ => new BlueprintNodeController<NodeModel>(new NodeModel(dataTransferObject), graph)
            };
            
            foreach (var pinDto in dataTransferObject.InputPins)
            {
                var content = TypeUtility.CastToType(pinDto.Content, pinDto.PinType);
                Debug.Log($"Pin: {pinDto.PinName} Content: {content}");
                if (!controller.InPorts.TryGetValue(pinDto.PinName, out var inPort) || !inPort.HasInlineValue)
                {
                    continue;
                }

                inPort.Type = pinDto.PinType;
                inPort.SetDefaultValue(content);
            }

            foreach (var pinDto in dataTransferObject.OutputPins)
            {
                // var content = TypeUtility.CastToType(pinDto.Content, pinDto.PinType);
                // Debug.Log($"Pin: {pinDto.PinName} Content: {content}");
                if (!controller.OutPorts.TryGetValue(pinDto.PinName, out var outPort))
                {
                    continue;
                }
                outPort.Type = pinDto.PinType;
            }

            return controller;
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