using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Vapor.Inspector;
using Vapor.NewtonsoftConverters;
using Vapor.VisualScripting;

namespace Vapor.Blueprints
{
    public enum PinDirection
    {
        In,
        Out
    }

    [Blueprintable]
    public enum BlueprintNodeType
    {
        Method,
        Entry,
        Return,
        IfElse,
        ForEach,
        Getter,
        Setter,
        Reroute,
        Converter,
        Graph,
        FieldGetter,
        FieldSetter,
    }

    public static class PinNames
    {
        public const string EXECUTE_IN = "IN";
        public const string EXECUTE_OUT = "OUT";

        public const string OWNER = "Owner";
        public const string RETURN = "Return";
    }

    [Serializable, ArrayEntryName("@GetArrayName")]
    public class BlueprintNodeDataModel
    {
        private static Color s_DefaultTextColor = new(0.7568628f, 0.7568628f, 0.7568628f);
        private string GetArrayName() => $"{NodeType}: {MethodName}";

        [SerializeField] private string _guid;

        public string Guid
        {
            get
            {
                if (!_guid.EmptyOrNull())
                {
                    return _guid;
                }

                _guid = System.Guid.NewGuid().ToString();
                return _guid;
            }
        }

        [SerializeField] private BlueprintNodeType _nodeType;

        public BlueprintNodeType NodeType
        {
            get => _nodeType;
            set => _nodeType = value;
        }

        [SerializeField] private string _assemblyQualifiedType;

        public string AssemblyQualifiedType
        {
            get => _assemblyQualifiedType;
            set => _assemblyQualifiedType = value;
        }

        [SerializeField] private string _methodName;

        public string MethodName
        {
            get => _methodName;
            set => _methodName = value;
        }

        [SerializeField] private List<string> _parameterTypeNames = new();
        public List<string> ParameterTypeNames => _parameterTypeNames;

        [SerializeField] private int _intData;

        public int IntData
        {
            get => _intData;
            set => _intData = value;
        }

        [SerializeField] private Rect _position;

        public Rect Position
        {
            get => _position;
            set => _position = value;
        }

        [SerializeField] private List<BlueprintWireReference> _inEdges = new();
        public List<BlueprintWireReference> InEdges => _inEdges;

        [SerializeField] private List<BlueprintWireReference> _outEdges = new();
        public List<BlueprintWireReference> OutEdges => _outEdges;

        [SerializeField] private string _inputPortData;

        public string InputPortDataJson
        {
            get => _inputPortData;
            set => _inputPortData = value;
        }

        // Reflection Helpers
        [NonSerialized] private MethodInfo _methodInfo;

        public MethodInfo MethodInfo
        {
            get
            {
                if (_methodInfo != null)
                {
                    return _methodInfo;
                }

                Type type = Type.GetType(AssemblyQualifiedType);
                if (type == null)
                {
                    return _methodInfo;
                }

                if (ParameterTypeNames.Count > 0)
                {
                    var paramTypes = new Type[ParameterTypeNames.Count];
                    for (int i = 0; i < ParameterTypeNames.Count; i++)
                    {
                        paramTypes[i] = Type.GetType(ParameterTypeNames[i]);
                    }

                    _methodInfo = type.GetMethod(_methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, paramTypes, null);
                }
                else
                {
                    _methodInfo = type.GetMethod(_methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                }

                return _methodInfo;
            }
        }

        [NonSerialized] private FieldInfo _fieldInfo;

        public FieldInfo FieldInfo
        {
            get
            {
                if (_fieldInfo != null)
                {
                    return _fieldInfo;
                }

                Type type = Type.GetType(AssemblyQualifiedType);
                if (type == null)
                {
                    return _fieldInfo;
                }

                _fieldInfo = type.GetField(_methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                return _fieldInfo;
            }
        }

        public string NodeName { get; internal set; }
        public Dictionary<string, BlueprintPin> InPorts { get; protected set; } = new();
        public Dictionary<string, BlueprintPin> OutPorts { get; protected set; } = new();

        public (string, Color) GetNodeName() => (NodeName, s_DefaultTextColor);
        public (Sprite, Color) GetNodeNameIcon() => (null, Color.white);

        public void Validate()
        {
            _ = Guid;
            switch (NodeType)
            {
                //Everything but Entry, Return, Getter, Setter, Update Here.
                case BlueprintNodeType.Method:
                    BlueprintNodeDataModelUtility.CreateOrUpdateMethodNode(this);
                    break;
                case BlueprintNodeType.IfElse:
                    BlueprintNodeDataModelUtility.CreateOrUpdateIfElseNode(this);
                    break;
                case BlueprintNodeType.ForEach:
                    BlueprintNodeDataModelUtility.CreateOrUpdateForEachNode(this);
                    break;
                case BlueprintNodeType.Reroute:
                    BlueprintNodeDataModelUtility.CreateOrUpdateRerouteNode(this, Type.GetType(MethodName));
                    break;
                case BlueprintNodeType.Converter:
                    BlueprintNodeDataModelUtility.CreateOrUpdateConverterNode(this);
                    break;
                case BlueprintNodeType.Graph:
                    BlueprintNodeDataModelUtility.CreateOrUpdateGraphNode(this, MethodName);
                    break;
                case BlueprintNodeType.FieldGetter:
                    BlueprintNodeDataModelUtility.CreateOrUpdateFieldGetterNode(this);
                    break;
                case BlueprintNodeType.FieldSetter:
                    BlueprintNodeDataModelUtility.CreateOrUpdateFieldSetterNode(this);
                    break;
            }

            if (InputPortDataJson.EmptyOrNull())
            {
                return;
            }

            var serializerSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new FieldsOnlyContractResolver(),
                Converters = new List<JsonConverter>
                {
                    new Vector2Converter(), new Vector2IntConverter(), new Vector3Converter(), new Vector3IntConverter(), new Vector4Converter(),
                    new ColorConverter(), new RectConverter(), new RectIntConverter(), new BoundsConverter(), new BoundsIntConverter(),
                    new LayerMaskConverter(), new RenderingLayerMaskConverter(),
                    new AnimationCurveConverter(), new KeyframeConverter(),
                    new GradientConverter(), new GradientColorKeyConverter(), new GradientAlphaKeyConverter(),
                    new Hash128Converter()
                },
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
                Error = (sender, args) => { args.ErrorContext.Handled = true; }
            };
            var data = JsonConvert.DeserializeObject<List<BlueprintPinData>>(InputPortDataJson, serializerSettings);

            var jsonSerializer = JsonSerializer.Create(serializerSettings);
            foreach (var d in data)
            {
                var content = d.Content;
                // if (content is JObject jObject)
                // {
                //     content = jObject.ToObject(d.PinType, jsonSerializer);
                // }
                //
                // if (d.PinType == typeof(LayerMask))
                // {
                //     if (content != null) content = (LayerMask)(long)content;
                // }
                //
                // if (d.PinType == typeof(RenderingLayerMask))
                // {
                //     if (content != null) content = (RenderingLayerMask)(uint)(long)content;
                // }
                //
                // if (d.PinType == typeof(Hash128))
                // {
                //     if (content != null) content = Hash128.Parse((string)content);
                // }
                //
                // if (d.PinType is { IsEnum: true })
                // {
                //     if (content != null) content = Enum.ToObject(d.PinType, Convert.ToInt32(content));
                // }

                if (InPorts.TryGetValue(d.PinName, out var inPort) && inPort.HasInlineValue)
                {
                    inPort.SetDefaultValue(content);
                    //var converted =  BlueprintNodeDataModelUtility.CastToType(content, inPort.ContentType);
                    // if(converted != null)
                    // {
                    //     inPort.SetDefaultValue(converted);
                    // }
                    // else
                    // {
                    //     if (inPort.ContentType == typeof(string))
                    //     {
                    //         var newInstance = string.Empty;
                    //         inPort.SetDefaultValue(newInstance);
                    //     }
                    //     else
                    //     {
                    //         var newInstance = Activator.CreateInstance(inPort.ContentType);
                    //         inPort.SetDefaultValue(newInstance);
                    //     }
                    // }
                }
            }
        }

        public void Serialize()
        {
            List<BlueprintPinData> data = new();
            foreach (var port in InPorts)
            {
                if (port.Value.IsExecutePin)
                {
                    continue;
                }

                if (!port.Value.HasInlineValue)
                {
                    continue;
                }

                data.Add(new BlueprintPinData()
                {
                    PinName = port.Key,
                    PinType = port.Value.InlineValue.GetPinType(),
                    Content = port.Value.InlineValue,
                });
            }

            _inputPortData = JsonConvert.SerializeObject(data, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new FieldsOnlyContractResolver(),
                Converters = new List<JsonConverter>
                {
                    new Vector2Converter(), new Vector2IntConverter(), new Vector3Converter(), new Vector3IntConverter(), new Vector4Converter(),
                    new ColorConverter(), new RectConverter(), new RectIntConverter(), new BoundsConverter(), new BoundsIntConverter(),
                    new LayerMaskConverter(), new RenderingLayerMaskConverter(),
                    new AnimationCurveConverter(), new KeyframeConverter(),
                    new GradientConverter(), new GradientColorKeyConverter(), new GradientAlphaKeyConverter(),
                    new Hash128Converter()
                },
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented
            });
        }

        public BlueprintBaseNode Compile()
        {
            return NodeType switch
            {
                BlueprintNodeType.Method => new BlueprintMethodNode(this),
                BlueprintNodeType.Entry => new BlueprintEntryNode(this),
                BlueprintNodeType.Return => new BlueprintReturnNode(this),
                BlueprintNodeType.IfElse => new BlueprintIfElseNode(this),
                BlueprintNodeType.ForEach => new BlueprintForEachNode(this),
                BlueprintNodeType.Getter => new BlueprintGetterNode(this),
                BlueprintNodeType.Setter => new BlueprintSetterNode(this),
                BlueprintNodeType.Reroute => new BlueprintRedirectNode(this),
                BlueprintNodeType.Converter => new BlueprintConverterNode(this),
                BlueprintNodeType.Graph => new BlueprintGraphNode(this),
                BlueprintNodeType.FieldGetter => new BlueprintFieldGetterNode(this),
                BlueprintNodeType.FieldSetter => new BlueprintFieldSetterNode(this),
                _ => null
            };
        }
    }

    public static class BlueprintNodeDataModelUtility
    {

        public static BlueprintNodeDataModel CreateOrUpdateEntryNode(BlueprintNodeDataModel entry, List<BlueprintIOParameter> parameters)
        {
            entry ??= new BlueprintNodeDataModel();
            entry.NodeType = BlueprintNodeType.Entry;
            entry.AssemblyQualifiedType = string.Empty;
            entry.MethodName = string.Empty;
            entry.NodeName = "Entry";
            entry.OutPorts.Clear();

            var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("");
            entry.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);

            foreach (var parameter in parameters)
            {
                var tuple = parameter.ToParameter();
                var slot = new BlueprintPin(tuple.Item1, PinDirection.Out, tuple.Item2, false)
                    .WithAllowMultipleWires();
                entry.OutPorts.Add(tuple.Item1, slot);
            }

            return entry;
        }

        public static BlueprintNodeDataModel CreateOrUpdateReturnNode(BlueprintNodeDataModel ret, List<BlueprintIOParameter> parameters)
        {
            ret ??= new BlueprintNodeDataModel();
            ret.NodeType = BlueprintNodeType.Return;
            ret.AssemblyQualifiedType = string.Empty;
            ret.MethodName = string.Empty;
            ret.NodeName = "Return";
            ret.InPorts.Clear();

            var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName("")
                .WithAllowMultipleWires();
            ret.InPorts.Add(PinNames.EXECUTE_IN, inSlot);

            foreach (var parameter in parameters)
            {
                var tuple = parameter.ToParameter();
                var slot = new BlueprintPin(tuple.Item1, PinDirection.In, tuple.Item2, false);
                ret.InPorts.Add(tuple.Item1, slot);
            }

            return ret;
        }

        public static BlueprintNodeDataModel CreateOrUpdateMethodNode(BlueprintNodeDataModel node, string assemblyQualifiedName = null, string methodName = null, string[] parameterTypes = null)
        {
            node ??= new BlueprintNodeDataModel();
            node.NodeType = BlueprintNodeType.Method;
            node.AssemblyQualifiedType = assemblyQualifiedName.EmptyOrNull() ? node.AssemblyQualifiedType : assemblyQualifiedName;
            node.MethodName = methodName.EmptyOrNull() ? node.MethodName : methodName;
            if (parameterTypes != null)
            {
                node.ParameterTypeNames.Clear();
                node.ParameterTypeNames.AddRange(parameterTypes);
            }

            node.InPorts.Clear();
            node.OutPorts.Clear();
            var methodInfo = node.MethodInfo;
            var nodeName = methodInfo.IsSpecialName ? ToTitleCase(methodInfo.Name) : methodInfo.Name;
#if UNITY_EDITOR
            node.NodeName = UnityEditor.ObjectNames.NicifyVariableName(nodeName);
#endif

            var paramInfos = methodInfo.GetParameters();
            bool hasOutParameter = paramInfos.Any(p => p.IsOut);
            var callableAttribute = methodInfo.GetCustomAttribute<BlueprintCallableAttribute>();
#if UNITY_EDITOR
            node.NodeName = callableAttribute == null || callableAttribute.NodeName.EmptyOrNull() ? node.NodeName : callableAttribute.NodeName;
#endif
            if (methodInfo.ReturnType == typeof(void) || hasOutParameter)
            {
                var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                    .WithDisplayName(string.Empty)
                    .WithAllowMultipleWires();
                node.InPorts.Add(PinNames.EXECUTE_IN, inSlot);
                var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                    .WithDisplayName(string.Empty);
                node.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);
            }

            if (!methodInfo.IsStatic)
            {
                var slot = new BlueprintPin(PinNames.OWNER, PinDirection.In, methodInfo.DeclaringType, false);
                node.InPorts.Add(PinNames.OWNER, slot);
            }

            if (methodInfo.ReturnType != typeof(void))
            {
                var retParam = methodInfo.ReturnParameter;
                if (retParam is { IsRetval: true })
                {
                    // Out Ports
                    var slot = new BlueprintPin(PinNames.RETURN, PinDirection.Out, retParam.ParameterType, false)
                        .WithAllowMultipleWires();
                    node.OutPorts.Add(PinNames.RETURN, slot);
                }
            }

            foreach (var pi in paramInfos)
            {
                if (pi.IsOut)
                {
                    // Out Ports
                    var paramAttribute = pi.GetCustomAttribute<BlueprintParamAttribute>();
                    string portName = pi.Name;
                    string displayName = pi.Name;
#if UNITY_EDITOR
                    displayName = UnityEditor.ObjectNames.NicifyVariableName(displayName);
#endif
                    if (paramAttribute != null)
                    {
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
                    node.OutPorts.Add(portName, slot);
                }
                else
                {
                    // In Ports
                    var paramAttribute = pi.GetCustomAttribute<BlueprintParamAttribute>();
                    string portName = pi.Name;
                    string displayName = pi.Name;
#if UNITY_EDITOR
                    displayName = UnityEditor.ObjectNames.NicifyVariableName(displayName);
#endif
                    if (paramAttribute != null)
                    {
                        if (!paramAttribute.Name.EmptyOrNull())
                        {
                            displayName = paramAttribute.Name;
                        }
                    }

                    var slot = new BlueprintPin(portName, PinDirection.In, pi.ParameterType, false)
                        .WithDisplayName(displayName)
                        .WithIsOptional();
                    if (pi.HasDefaultValue && slot.HasInlineValue)
                    {
                        slot.SetDefaultValue(pi.DefaultValue);
                    }

                    node.InPorts.Add(portName, slot);
                }
            }

            return node;
        }

        public static BlueprintNodeDataModel CreateOrUpdateIfElseNode(BlueprintNodeDataModel node)
        {
            node ??= new BlueprintNodeDataModel();
            node.NodeType = BlueprintNodeType.IfElse;
            node.AssemblyQualifiedType = string.Empty;
            node.MethodName = string.Empty;
            node.NodeName = "Branch";
            node.InPorts.Clear();
            node.OutPorts.Clear();

            var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName("")
                .WithAllowMultipleWires();
            node.InPorts.Add(PinNames.EXECUTE_IN, inSlot);

            var slot = new BlueprintPin("Value", PinDirection.In, typeof(bool), false);
            node.InPorts.Add("Value", slot);

            var trueSlot = new BlueprintPin("True", PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("True");
            node.OutPorts.Add("True", trueSlot);

            var falseSlot = new BlueprintPin("False", PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("False");
            node.OutPorts.Add("False", falseSlot);

            return node;
        }

        public static BlueprintNodeDataModel CreateOrUpdateForEachNode(BlueprintNodeDataModel node)
        {
            node ??= new BlueprintNodeDataModel();
            node.NodeType = BlueprintNodeType.ForEach;
            node.AssemblyQualifiedType = string.Empty;
            node.MethodName = string.Empty;
            node.NodeName = "For Each";
            node.InPorts.Clear();
            node.OutPorts.Clear();

            var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            node.InPorts.Add(PinNames.EXECUTE_IN, inSlot);

            var breakSlot = new BlueprintPin("Break", PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName("Break")
                .WithAllowMultipleWires();
            node.InPorts.Add("Break", breakSlot);

            var arraySlot = new BlueprintPin("Array", PinDirection.In, typeof(object), false)
                .WithDisplayName("Array");
            node.InPorts.Add("Array", arraySlot);

            var loopSlot = new BlueprintPin("Loop", PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("Loop");
            node.OutPorts.Add("Loop", loopSlot);

            var indexSlot = new BlueprintPin("Index", PinDirection.Out, typeof(int), false)
                .WithDisplayName("Index");
            node.OutPorts.Add("Index", indexSlot);

            var elementSlot = new BlueprintPin("Element", PinDirection.Out, typeof(object), false)
                .WithDisplayName("Element");
            node.OutPorts.Add("Element", elementSlot);

            var completedSlot = new BlueprintPin("Complete", PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName("Complete");
            node.OutPorts.Add("Complete", completedSlot);

            return node;
        }

        public static BlueprintNodeDataModel CreateOrUpdateGetterNode(BlueprintNodeDataModel get, BlueprintIOParameter parameter)
        {
            get ??= new BlueprintNodeDataModel();
            get.NodeType = BlueprintNodeType.Getter;
            get.AssemblyQualifiedType = string.Empty;
            var tuple = parameter.ToParameter();
            get.MethodName = tuple.Item1;
            get.NodeName = $"Get <b><i>{tuple.Item1}</i></b>";
            get.OutPorts.Clear();

            // var edgeToUpdateIdx = get.OutEdges.FindIndex(e => e.LeftSidePort.PortName == parameter.PreviousName);
            // if (edgeToUpdateIdx != -1)
            // {
            //     var oldEdge = get.OutEdges[edgeToUpdateIdx];
            //     var oldPort = oldEdge.LeftSidePort;
            //     var newPort = new BlueprintPortReference(tuple.Item1, oldEdge.LeftSidePort.NodeGuid, oldEdge.LeftSidePort.IsTransitionPort);
            //     get.OutEdges[edgeToUpdateIdx] = new BlueprintEdgeConnection(
            //         newPort,
            //         oldEdge.RightSidePort);
            //     RenamePortCallback?.Invoke(oldPort, newPort);
            // }
            // parameter.PreviousName = tuple.Item1;

            var slot = new BlueprintPin(tuple.Item1, PinDirection.Out, tuple.Item2, false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            get.OutPorts.Add(tuple.Item1, slot);

            return get;
        }

        public static BlueprintNodeDataModel CreateOrUpdateSetterNode(BlueprintNodeDataModel set, BlueprintIOParameter parameter)
        {
            set ??= new BlueprintNodeDataModel();
            set.NodeType = BlueprintNodeType.Setter;
            set.AssemblyQualifiedType = string.Empty;
            var tuple = parameter.ToParameter();
            set.MethodName = tuple.Item1;
            set.NodeName = $"Set <b><i>{tuple.Item1}</i></b>";
            set.InPorts.Clear();
            set.OutPorts.Clear();

            var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            set.InPorts.Add(PinNames.EXECUTE_IN, inSlot);

            var inData = new BlueprintPin(tuple.Item1, PinDirection.In, tuple.Item2, false)
                .WithDisplayName(string.Empty);
            set.InPorts.Add(tuple.Item1, inData);

            var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty);
            set.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);

            var os = new BlueprintPin(tuple.Item1, PinDirection.Out, tuple.Item2, false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            set.OutPorts.Add(tuple.Item1, os);

            return set;
        }

        public static BlueprintNodeDataModel CreateOrUpdateRerouteNode(BlueprintNodeDataModel node, Type rerouteType)
        {
            node ??= new BlueprintNodeDataModel();
            node.NodeType = BlueprintNodeType.Reroute;
            node.AssemblyQualifiedType = string.Empty;
            node.MethodName = rerouteType.AssemblyQualifiedName;
            node.NodeName = string.Empty;
            node.InPorts.Clear();
            node.OutPorts.Clear();

            if (rerouteType != typeof(ExecutePin))
            {
                var slot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, rerouteType, false)
                    .WithDisplayName(string.Empty);
                node.InPorts.Add(PinNames.EXECUTE_IN, slot);

                var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, rerouteType, false)
                    .WithDisplayName(string.Empty)
                    .WithAllowMultipleWires();
                node.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);
            }
            else
            {
                var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                    .WithDisplayName(string.Empty)
                    .WithAllowMultipleWires();
                node.InPorts.Add(PinNames.EXECUTE_IN, inSlot);
                var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                    .WithDisplayName(string.Empty);
                node.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);
            }

            return node;
        }

        public static BlueprintNodeDataModel CreateOrUpdateConverterNode(BlueprintNodeDataModel node, string assemblyQualifiedName = null, string methodName = null)
        {
            node ??= new BlueprintNodeDataModel();
            node.NodeType = BlueprintNodeType.Converter;
            node.AssemblyQualifiedType = assemblyQualifiedName.EmptyOrNull() ? node.AssemblyQualifiedType : assemblyQualifiedName;
            node.MethodName = methodName.EmptyOrNull() ? node.MethodName : methodName;
            node.NodeName = string.Empty;
            node.InPorts.Clear();
            node.OutPorts.Clear();
            var methodInfo = node.MethodInfo;
            Assert.IsTrue(methodInfo.IsDefined(typeof(BlueprintPinConverterAttribute)), $"Converter Node Method [{node.MethodName}] Must Have BlueprintPinConverterAttribute");

            var atr = methodInfo.GetCustomAttribute<BlueprintPinConverterAttribute>();

            var slot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, atr.SourceType, false)
                .WithDisplayName(string.Empty);
            node.InPorts.Add(PinNames.EXECUTE_IN, slot);

            var outSlot = new BlueprintPin(PinNames.RETURN, PinDirection.Out, atr.TargetType, false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            node.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);

            return node;
        }

        public static BlueprintNodeDataModel CreateOrUpdateGraphNode(BlueprintNodeDataModel node, string assetGuid)
        {
            node ??= new BlueprintNodeDataModel();
            node.NodeType = BlueprintNodeType.Graph;
            node.AssemblyQualifiedType = string.Empty;
            node.MethodName = assetGuid;
            node.NodeName = string.Empty;
            node.InPorts.Clear();
            node.OutPorts.Clear();

#if UNITY_EDITOR
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
            var found = UnityEditor.AssetDatabase.LoadAssetAtPath<BlueprintGraphSo>(path);
            Assert.IsTrue(found, $"Graph With Guid [{assetGuid}] Not Found");
            node.IntData = found.Key;

            node.NodeName = UnityEditor.ObjectNames.NicifyVariableName(found.DisplayName);
#endif

            // Execute Pins
            var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            node.InPorts.Add(PinNames.EXECUTE_IN, inSlot);
            var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty);
            node.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);

            // Value Pins

            // Input
            foreach (var inputParameter in found.InputParameters)
            {
                string portName = inputParameter.FieldName;
                string displayName = inputParameter.FieldName;
#if UNITY_EDITOR
                displayName = UnityEditor.ObjectNames.NicifyVariableName(inputParameter.FieldName);
#endif

                var tuple = inputParameter.ToParameter();
                var slot = new BlueprintPin(portName, PinDirection.In, tuple.Item2, false)
                    .WithDisplayName(displayName);
                node.InPorts.Add(portName, slot);
            }

            // Output
            foreach (var outputParameter in found.OutputParameters)
            {
                string portName = outputParameter.FieldName;
                string displayName = outputParameter.FieldName;
#if UNITY_EDITOR
                displayName = UnityEditor.ObjectNames.NicifyVariableName(outputParameter.FieldName);
#endif
                var tuple = outputParameter.ToParameter();
                var type = tuple.Item2;
                if (type.IsByRef)
                {
                    type = type.GetElementType();
                }

                var slot = new BlueprintPin(portName, PinDirection.Out, type, false)
                    .WithDisplayName(displayName)
                    .WithAllowMultipleWires();
                node.OutPorts.Add(portName, slot);
            }

            return node;
        }

        public static BlueprintNodeDataModel CreateOrUpdateFieldGetterNode(BlueprintNodeDataModel node, string assemblyQualifiedName = null, string fieldName = null)
        {
            node ??= new BlueprintNodeDataModel();
            node.NodeType = BlueprintNodeType.FieldGetter;
            node.AssemblyQualifiedType = assemblyQualifiedName.EmptyOrNull() ? node.AssemblyQualifiedType : assemblyQualifiedName;
            node.MethodName = fieldName.EmptyOrNull() ? node.MethodName : fieldName;

            node.InPorts.Clear();
            node.OutPorts.Clear();
            var fieldInfo = node.FieldInfo;
            var nodeName = fieldInfo.Name;
#if UNITY_EDITOR
            node.NodeName = UnityEditor.ObjectNames.NicifyVariableName(nodeName);
#endif

            // In Pin
            var ownerPin = new BlueprintPin(PinNames.OWNER, PinDirection.In, fieldInfo.DeclaringType, true);
            node.InPorts.Add(PinNames.OWNER, ownerPin);

            // Out Pin
            var returnPin = new BlueprintPin(PinNames.RETURN, PinDirection.Out, fieldInfo.FieldType, false)
                .WithAllowMultipleWires();
            node.OutPorts.Add(PinNames.RETURN, returnPin);

            return node;
        }

        public static BlueprintNodeDataModel CreateOrUpdateFieldSetterNode(BlueprintNodeDataModel node, string assemblyQualifiedName = null, string fieldName = null)
        {
            node ??= new BlueprintNodeDataModel();
            node.NodeType = BlueprintNodeType.FieldSetter;
            node.AssemblyQualifiedType = assemblyQualifiedName.EmptyOrNull() ? node.AssemblyQualifiedType : assemblyQualifiedName;
            node.MethodName = fieldName.EmptyOrNull() ? node.MethodName : fieldName;

            node.InPorts.Clear();
            node.OutPorts.Clear();
            var fieldInfo = node.FieldInfo;
            var nodeName = fieldInfo.Name;
#if UNITY_EDITOR
            node.NodeName = UnityEditor.ObjectNames.NicifyVariableName(nodeName);
#endif

            var inSlot = new BlueprintPin(PinNames.EXECUTE_IN, PinDirection.In, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty)
                .WithAllowMultipleWires();
            node.InPorts.Add(PinNames.EXECUTE_IN, inSlot);
            var outSlot = new BlueprintPin(PinNames.EXECUTE_OUT, PinDirection.Out, typeof(ExecutePin), false)
                .WithDisplayName(string.Empty);
            node.OutPorts.Add(PinNames.EXECUTE_OUT, outSlot);

            // In Pin
            var ownerPin = new BlueprintPin(PinNames.OWNER, PinDirection.In, fieldInfo.DeclaringType, true);
            node.InPorts.Add(PinNames.OWNER, ownerPin);

            var setterPin = new BlueprintPin(fieldInfo.Name, PinDirection.In, fieldInfo.FieldType, false);
            node.InPorts.Add(fieldInfo.Name, setterPin);

            return node;
        }

        public static string ToTitleCase(string input)
        {
            if (input.EmptyOrNull())
            {
                return input;
            }

            // Step 1: Replace underscores with spaces
            input = input.Replace("_", " ");

            // Step 2: Insert space before uppercase letters (excluding first character)
            input = Regex.Replace(input, "(?<!^)([A-Z])", " $1");

            // Step 3: Convert to Title Case
            TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
            return textInfo.ToTitleCase(input.ToLowerInvariant());
        }

        private static bool IsArrayOrList(Type type)
        {
            // Check if the type is an array
            if (type.IsArray)
            {
                return true;
            }

            // Check if the type is a List<> or a derived type
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        private static bool IsDictionary(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        internal static object CastToType(object obj, Type targetType)
        {
            if (obj == null)
            {
                if (targetType.IsClass || Nullable.GetUnderlyingType(targetType) != null)
                {
                    return null; // Null is a valid value for reference types and nullable types
                }

                throw new ArgumentNullException(nameof(obj), "Cannot cast null to a non-nullable value type.");
            }

            // Check if the object is already of the target type
            if (targetType.IsAssignableFrom(obj.GetType()))
            {
                return obj; // No casting needed
            }

            return Convert.ChangeType(obj, targetType);
        }
    }

    [Serializable]
    internal class BlueprintPinData
    {
        public string PinName;
        public Type PinType;
        public object Content;
    }
}