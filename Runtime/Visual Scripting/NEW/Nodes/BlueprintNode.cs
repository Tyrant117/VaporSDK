using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;
using Vapor.Inspector;
using Vapor.VisualScripting;

namespace Vapor.Blueprints
{
    public enum PortDirection
    {
        In,
        Out
    }

    public enum BlueprintNodeType
    {
        Method,
        Entry,
        Return,
        IfElse,
        ForEach,
        Getter,
        Setter,
    }

    public abstract class BlueprintBaseNode
    {
        protected IBlueprintGraph Graph { get; set; }
        public string Guid { get; protected set; }
        protected List<BlueprintEdgeConnection> InEdges { get; set; }
        
        protected Dictionary<string, object> InPortValues;
        protected Dictionary<string, object> OutPortValues;

        public abstract void Init(IBlueprintGraph graph);

        public void Invoke()
        {
            CacheInputValues();
            WriteOutputValues();
        }

        public virtual void InvokeAndContinue()
        {
            Invoke();
            Continue();
        }
        protected abstract void CacheInputValues();
        protected abstract void WriteOutputValues();
        protected abstract void Continue();

        public bool TryGetOutputValue(string outPortName, out object outputValue)
        {
            return OutPortValues.TryGetValue(outPortName, out outputValue);
        }
    }

    public class BlueprintMethodNode : BlueprintBaseNode
    {
        private readonly Delegate _function;
        private readonly bool _hasReturnValue;
        private readonly ParameterInfo[] _parameters;
        private readonly object[] _parameterValues;

        private readonly string _nextNodeGuid;
        private BlueprintBaseNode _nextNode;

        public BlueprintMethodNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            _function = MathLibrary.GetDelegateForMethod(dataModel.MethodInfo);
            InEdges = dataModel.InEdges;
            
            _hasReturnValue = dataModel.MethodInfo.ReturnType != typeof(void);
            _parameters = dataModel.MethodInfo.GetParameters();
            _parameterValues = new object[_parameters.Length];
            
            InPortValues = new Dictionary<string, object>(dataModel.InPorts.Count);
            foreach (var inPort in dataModel.InPorts.Values)
            {
                if (inPort.HasContent)
                {
                    InPortValues[inPort.PortName] = inPort.Content;
                }
            }

            OutPortValues = new Dictionary<string, object>(dataModel.OutPorts.Count);
            foreach (var outPort in dataModel.OutPorts.Values)
            {
                if (!outPort.IsTransitionPort)
                {
                    OutPortValues[outPort.PortName] = null;
                }
            }

            var outEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePort.PortName == "OUT");
            if (outEdge.RightSidePort.IsValid())
            {
                _nextNodeGuid = outEdge.RightSidePort.NodeGuid;
            }
        }

        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
            if (!_nextNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_nextNodeGuid, out _nextNode);
            }
        }

        protected override void CacheInputValues()
        {
            foreach (var edge in InEdges)
            {
                if (edge.LeftSidePort.IsTransitionPort)
                {
                    continue;
                }
                
                if (!Graph.TryGetNode(edge.LeftSidePort.NodeGuid, out var leftNode))
                {
                    continue;
                }

                leftNode.Invoke();
                if (leftNode.TryGetOutputValue(edge.LeftSidePort.PortName, out var outputValue))
                {
                    InPortValues[edge.RightSidePort.PortName] = outputValue;
                }
            }
        }

        protected override void WriteOutputValues()
        {
            if (_function == null)
            {
                return;
            }

            // if the method isn't static the first parameter needs to be the assigned owner
            // then custom data
            // then the other in port values
            // then the out ports
            for (int i = 0; i < _parameters.Length; i++)
            {
                _parameterValues[i] = _parameters[i].IsOut ? null : InPortValues[_parameters[i].Name];
            }

            var retVal = _function.DynamicInvoke(_parameterValues);
            if (_hasReturnValue)
            {
                OutPortValues["Return"] = retVal;
            }

            for (int i = 0; i < _parameters.Length; i++)
            {
                if (_parameters[i].IsOut)
                {
                    OutPortValues[_parameters[i].Name] = _parameterValues[i];
                }
            }
        }

        protected override void Continue()
        {
            if (!Graph.IsEvaluating)
            {
                return;
            }
            
            _nextNode?.InvokeAndContinue();
        }
    }

    public class BlueprintEntryNode : BlueprintBaseNode
    {
        
        private readonly string _nextNodeGuid;
        private BlueprintBaseNode _nextNode;
        
        public BlueprintEntryNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            
            OutPortValues = new Dictionary<string, object>(dataModel.OutPorts.Count);
            foreach (var outPort in dataModel.OutPorts.Values)
            {
                if (!outPort.IsTransitionPort)
                {
                    OutPortValues[outPort.PortName] = null;
                }
            }

            var outEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePort.PortName == "OUT");
            if (outEdge.RightSidePort.IsValid())
            {
                _nextNodeGuid = outEdge.RightSidePort.NodeGuid;
            }
        }
        
        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
            if (!_nextNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_nextNodeGuid, out _nextNode);
            }
        }

        protected override void CacheInputValues()
        {
        }

        protected override void WriteOutputValues()
        {
            foreach (var param in Graph.GetParameters())
            {
                if (OutPortValues.ContainsKey(param.Key))
                {
                    OutPortValues[param.Key] = param.Value;
                }
                else
                {
                    Debug.LogError($"Failed to get output value for {param.Key}");
                }
            }
        }

        protected override void Continue()
        {
            if (!Graph.IsEvaluating)
            {
                return;
            }
            
            _nextNode?.InvokeAndContinue();
        }
    }

    public class BlueprintReturnNode : BlueprintBaseNode
    {
        public BlueprintReturnNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            InEdges = dataModel.InEdges;
            
            InPortValues = new Dictionary<string, object>(dataModel.InPorts.Count);
            foreach (var inPort in dataModel.InPorts.Values)
            {
                if (inPort.HasContent)
                {
                    InPortValues[inPort.PortName] = inPort.Content;
                }
            }
        }
        
        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
        }

        protected override void CacheInputValues()
        {
            foreach (var edge in InEdges)
            {
                if (edge.LeftSidePort.IsTransitionPort)
                {
                    continue;
                }
                
                if (!Graph.TryGetNode(edge.LeftSidePort.NodeGuid, out var leftNode))
                {
                    continue;
                }

                leftNode.Invoke();
                if (leftNode.TryGetOutputValue(edge.LeftSidePort.PortName, out var outputValue))
                {
                    InPortValues[edge.RightSidePort.PortName] = outputValue;
                }
            }
        }

        protected override void WriteOutputValues()
        {
            Graph.WriteReturnValues(InPortValues);
        }

        protected override void Continue()
        {
            if (!Graph.IsEvaluating)
            {
                return;
            }
            
            Graph.Return();
        }
    }

    public class BlueprintIfElseNode : BlueprintBaseNode
    {
        private readonly string _trueNodeGuid;
        private BlueprintBaseNode _trueNode;
        private readonly string _falseNodeGuid;
        private BlueprintBaseNode _falseNode;
        private bool _true;
        
        public BlueprintIfElseNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            InEdges = dataModel.InEdges;
            
            InPortValues = new Dictionary<string, object>(dataModel.InPorts.Count);
            foreach (var inPort in dataModel.InPorts.Values)
            {
                if (inPort.HasContent)
                {
                    InPortValues[inPort.PortName] = inPort.Content;
                }
            }
            
            var trueEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePort.PortName == "True");
            if (trueEdge.RightSidePort.IsValid())
            {
                _trueNodeGuid = trueEdge.RightSidePort.NodeGuid;
            }
            
            var falseEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePort.PortName == "False");
            if (falseEdge.RightSidePort.IsValid())
            {
                _falseNodeGuid = falseEdge.RightSidePort.NodeGuid;
            }
        }
        
        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
            if (!_trueNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_trueNodeGuid, out _trueNode);
            }
            if (!_falseNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_falseNodeGuid, out _falseNode);
            }
        }

        protected override void CacheInputValues()
        {
            foreach (var edge in InEdges)
            {
                if (edge.LeftSidePort.IsTransitionPort)
                {
                    continue;
                }
                
                if (!Graph.TryGetNode(edge.LeftSidePort.NodeGuid, out var leftNode))
                {
                    continue;
                }

                leftNode.Invoke();
                if (leftNode.TryGetOutputValue(edge.LeftSidePort.PortName, out var outputValue))
                {
                    InPortValues[edge.RightSidePort.PortName] = outputValue;
                }
            }
        }

        protected override void WriteOutputValues()
        {
            _true = (bool)InPortValues["Value"];
        }

        protected override void Continue()
        {
            if (!Graph.IsEvaluating)
            {
                return;
            }
            
            if (_true)
            {
                _trueNode?.InvokeAndContinue();
            }
            else
            {
                _falseNode?.InvokeAndContinue();
            }
        }
    }

    public class BlueprintForEachNode : BlueprintBaseNode
    {
        private bool _looping;
        
        private readonly string _loopNodeGuid;
        private BlueprintBaseNode _loopNode;

        private readonly string _completedNodeGuid;
        private BlueprintBaseNode _completedNode;
        
        public BlueprintForEachNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            InEdges = dataModel.InEdges;
            
            InPortValues = new Dictionary<string, object>(dataModel.InPorts.Count);
            foreach (var inPort in dataModel.InPorts.Values)
            {
                if (inPort.HasContent)
                {
                    InPortValues[inPort.PortName] = inPort.Content;
                }
            }
            
            OutPortValues = new Dictionary<string, object>(dataModel.OutPorts.Count);
            foreach (var outPort in dataModel.OutPorts.Values)
            {
                if (!outPort.IsTransitionPort)
                {
                    OutPortValues[outPort.PortName] = null;
                }
            }
            
            
            var trueEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePort.PortName == "Loop");
            if (trueEdge.RightSidePort.IsValid())
            {
                _loopNodeGuid = trueEdge.RightSidePort.NodeGuid;
            }
            
            var falseEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePort.PortName == "Complete");
            if (falseEdge.RightSidePort.IsValid())
            {
                _completedNodeGuid = falseEdge.RightSidePort.NodeGuid;
            }
        }
        
        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
            if (!_loopNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_loopNodeGuid, out _loopNode);
            }
            if (!_completedNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_completedNodeGuid, out _completedNode);
            }
        }

        public override void InvokeAndContinue()
        {
            if (_looping)
            {
                _looping = false;
            }
            else
            {
                base.InvokeAndContinue();
            }
        }

        protected override void CacheInputValues()
        {
            foreach (var edge in InEdges)
            {
                if (edge.LeftSidePort.IsTransitionPort)
                {
                    continue;
                }
                
                if (!Graph.TryGetNode(edge.LeftSidePort.NodeGuid, out var leftNode))
                {
                    continue;
                }

                leftNode.Invoke();
                if (leftNode.TryGetOutputValue(edge.LeftSidePort.PortName, out var outputValue))
                {
                    InPortValues[edge.RightSidePort.PortName] = outputValue;
                }
            }
        }

        protected override void WriteOutputValues()
        {
            if (InPortValues.TryGetValue("Array", out var array))
            {
                var arr = (IEnumerable)array;
                int idx = 0;
                _looping = true;
                foreach (var a in arr)
                {
                    OutPortValues["Element"] = a;
                    int i = idx;
                    OutPortValues["Index"] = i;
                    _loopNode?.InvokeAndContinue();
                    if (!_looping || !Graph.IsEvaluating)
                    {
                        break;
                    }
                    idx++;
                }
            }
        }

        protected override void Continue()
        {
            if (!Graph.IsEvaluating)
            {
                return;
            }
            
            _completedNode?.InvokeAndContinue();
        }
    }

    public class BlueprintGetterNode : BlueprintBaseNode
    {
        private readonly string _tempFieldName;
        public BlueprintGetterNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            _tempFieldName = dataModel.MethodName;
            
            OutPortValues = new Dictionary<string, object>(dataModel.OutPorts.Count);
            foreach (var outPort in dataModel.OutPorts.Values)
            {
                if (!outPort.IsTransitionPort)
                {
                    OutPortValues[outPort.PortName] = null;
                }
            }
        }

        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
        }

        protected override void CacheInputValues()
        {
        }

        protected override void WriteOutputValues()
        {
            Graph.TryGetTempValue(_tempFieldName, out var temp);
            if (OutPortValues.ContainsKey(_tempFieldName))
            {
                OutPortValues[_tempFieldName] = temp;
            }
            else
            {
                Debug.LogError($"Failed to get output value for {_tempFieldName}");
            }
        }

        protected override void Continue()
        {
        }
    }

    public class BlueprintSetterNode : BlueprintBaseNode
    {
        private readonly string _tempFieldName;
        
        private readonly string _nextNodeGuid;
        private BlueprintBaseNode _nextNode;
        
        public BlueprintSetterNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            _tempFieldName = dataModel.MethodName;
            InEdges = dataModel.InEdges;
            
            InPortValues = new Dictionary<string, object>(dataModel.InPorts.Count);
            foreach (var inPort in dataModel.InPorts.Values)
            {
                if (inPort.HasContent)
                {
                    InPortValues[inPort.PortName] = inPort.Content;
                }
            }
            
            OutPortValues = new Dictionary<string, object>(dataModel.OutPorts.Count);
            foreach (var outPort in dataModel.OutPorts.Values)
            {
                if (!outPort.IsTransitionPort)
                {
                    OutPortValues[outPort.PortName] = null;
                }
            }
            
            var outEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePort.PortName == "OUT");
            if (outEdge.RightSidePort.IsValid())
            {
                _nextNodeGuid = outEdge.RightSidePort.NodeGuid;
            }
        }

        public override void Init(IBlueprintGraph graph)
        {
            Graph = graph;
            if (!_nextNodeGuid.EmptyOrNull())
            {
                Graph.TryGetNode(_nextNodeGuid, out _nextNode);
            }
        }

        protected override void CacheInputValues()
        {
            foreach (var edge in InEdges)
            {
                if (edge.LeftSidePort.IsTransitionPort)
                {
                    continue;
                }

                if (!Graph.TryGetNode(edge.LeftSidePort.NodeGuid, out var leftNode))
                {
                    continue;
                }

                leftNode.Invoke();
                if (leftNode.TryGetOutputValue(edge.LeftSidePort.PortName, out var outputValue))
                {
                    InPortValues[edge.RightSidePort.PortName] = outputValue;
                }
            }

            foreach (var ipv in InPortValues.Values)
            {
                Graph.TrySetTempValue(_tempFieldName, ipv);
            }
        }

        protected override void WriteOutputValues()
        {
            Graph.TryGetTempValue(_tempFieldName, out var temp);
            if (OutPortValues.ContainsKey(_tempFieldName))
            {
                OutPortValues[_tempFieldName] = temp;
            }
            else
            {
                Debug.LogError($"Failed to get output value for {_tempFieldName}");
            }
        }

        protected override void Continue()
        {
            if (!Graph.IsEvaluating)
            {
                return;
            }
            
            _nextNode?.InvokeAndContinue();
        }
    }

    [Serializable]
    public class BlueprintNodeDataModel
    {
        private static Color s_DefaultTextColor = new(0.7568628f, 0.7568628f, 0.7568628f);
        
        [SerializeField]
        private string _guid;
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
        
        [SerializeField]
        private BlueprintNodeType _nodeType;
        public BlueprintNodeType NodeType
        {
            get => _nodeType;
            set => _nodeType = value;
        }
        
        [SerializeField]
        private string _assemblyQualifiedType;
        public string AssemblyQualifiedType 
        {
            get => _assemblyQualifiedType;
            set => _assemblyQualifiedType = value;
        }
        
        [SerializeField]
        private string _methodName;
        public string MethodName
        {
            get => _methodName;
            set => _methodName = value;
        }
        
        [SerializeField]
        private Rect _position;
        public Rect Position
        {
            get => _position;
            set => _position = value;
        }
        
        [SerializeField]
        private List<BlueprintEdgeConnection> _inEdges = new();
        public List<BlueprintEdgeConnection> InEdges => _inEdges;
        
        [SerializeField]
        private List<BlueprintEdgeConnection> _outEdges = new();
        public List<BlueprintEdgeConnection> OutEdges => _outEdges;
        
        [SerializeField] 
        private string _inputPortData;
        public string InputPortDataJson
        {
            get => _inputPortData;
            set => _inputPortData = value;
        }

        // Reflection Helpers
        [NonSerialized] 
        private MethodInfo _methodInfo;
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

                _methodInfo = type.GetMethod(_methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                BuildSlots(_methodInfo);
                return _methodInfo;
            }    
        }

        public string NodeName { get; internal set; }
        public Dictionary<string, BlueprintPortSlot> InPorts { get; protected set; } = new();
        public Dictionary<string, BlueprintPortSlot> OutPorts { get; protected set; } = new();
        
        public (string, Color) GetNodeName() => (NodeName, s_DefaultTextColor);
        public (Sprite, Color) GetNodeNameIcon() => (null, Color.white);
        
        [NonSerialized]
        public Action RenameNode;
        public void OnRenameNode()
        {
            RenameNode?.Invoke();
        }
        
        private void BuildSlots(MethodInfo methodInfo)
        {
            if(methodInfo == null)
            {
                return;
            }
            
            var callableAttribute = methodInfo.GetCustomAttribute<BlueprintCallableAttribute>();
            if (callableAttribute != null)
            {
#if UNITY_EDITOR
                NodeName = callableAttribute.NodeName.EmptyOrNull() ? UnityEditor.ObjectNames.NicifyVariableName(methodInfo.Name) : callableAttribute.NodeName;
#endif
                var inSlot = new BlueprintPortSlot("IN", PortDirection.In, typeof(BlueprintNodeDataModel), false)
                    .WithDisplayName("")
                    .WithAllowMultiple();
                InPorts.Add("IN", inSlot);
                var outSlot = new BlueprintPortSlot("OUT", PortDirection.Out, typeof(BlueprintNodeDataModel), false)
                    .WithDisplayName("");
                OutPorts.Add("OUT", outSlot);
            }

            var pureAttribute = methodInfo.GetCustomAttribute<BlueprintPureAttribute>();
            if (pureAttribute != null)
            {
#if UNITY_EDITOR
                NodeName = pureAttribute.NodeName.EmptyOrNull() ? UnityEditor.ObjectNames.NicifyVariableName(methodInfo.Name) : pureAttribute.NodeName;
#endif
            }

            if (!methodInfo.IsStatic)
            {
                var slot = new BlueprintPortSlot("Owner", PortDirection.In, typeof(object), false);
                InPorts.Add("Owner", slot);
            }

            if (methodInfo.ReturnType != typeof(void))
            {
                var retParam = methodInfo.ReturnParameter;
                if (retParam is { IsRetval: true })
                {
                    // Out Ports
                    var slot = new BlueprintPortSlot("Return", PortDirection.Out, retParam.ParameterType, false)
                        .WithAllowMultiple();
                    OutPorts.Add("Return", slot);
                }
            }
            
            var paramInfos = methodInfo.GetParameters();
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

                    var slot = new BlueprintPortSlot(portName, PortDirection.Out, type, false)
                        .WithDisplayName(displayName)
                        .WithAllowMultiple();
                    OutPorts.Add(portName, slot);
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

                    var slot = new BlueprintPortSlot(portName, PortDirection.In, pi.ParameterType, pi.IsOptional)
                        .WithDisplayName(displayName);
                    if(pi.HasDefaultValue)
                    {
                        slot.SetDefaultValue(pi.DefaultValue);
                    }

                    InPorts.Add(portName, slot);
                }
            }
        }

        public void Validate()
        {
            var guid = Guid;
            var mi = MethodInfo;
            switch (NodeType)
            {
                //Everything but Method, Entry, Return Update Here.
                case BlueprintNodeType.IfElse:
                    BlueprintNodeDataModelUtility.CreateOrUpdateIfElseNode(this);
                    break;
                case BlueprintNodeType.ForEach:
                    BlueprintNodeDataModelUtility.CreateOrUpdateForEachNode(this);
                    break;
            }

            if (_inputPortData.EmptyOrNull())
            {
                return;
            }
            
            var serializerSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new FieldsOnlyContractResolver(),
                Converters = new List<JsonConverter> { new RectConverter() },
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
                Error = (sender, args) =>
                {
                    args.ErrorContext.Handled = true;
                }
            };
            var data = JsonConvert.DeserializeObject<List<BlueprintInputDataContainer>>(_inputPortData, serializerSettings);

            foreach (var d in data)
            {
                if (InPorts.TryGetValue(d.PortName, out var inPort) && inPort.HasContent)
                {
                    var converted = BlueprintNodeDataModelUtility.CastToType(d.Content, inPort.ContentType);
                    if(converted != null)
                    {
                        inPort.SetDefaultValue(converted);
                    }
                    else
                    {
                        if (inPort.ContentType == typeof(string))
                        {
                            var newInstance = string.Empty;
                            inPort.SetDefaultValue(newInstance);
                        }
                        else
                        {
                            var newInstance = Activator.CreateInstance(inPort.ContentType);
                            inPort.SetDefaultValue(newInstance);
                        }
                    }
                }
            }
        }

        public void Serialize()
        {
            List<BlueprintInputDataContainer> data = new();
            foreach (var port in InPorts)
            {
                if (port.Value.IsTransitionPort)
                {
                    continue;
                }
                
                data.Add(new BlueprintInputDataContainer()
                {
                    PortName = port.Key,
                    Content = port.Value.Content,
                });
            }
            
            _inputPortData = JsonConvert.SerializeObject(data, new JsonSerializerSettings()
            { 
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new FieldsOnlyContractResolver(),
                Converters = new List<JsonConverter> { new RectConverter() },
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
                _ => null
            };
        }
    }

    public static class BlueprintNodeDataModelUtility
    {
        internal static BlueprintNodeDataModel CreateOrUpdateEntryNode(BlueprintNodeDataModel entry, List<BlueprintIOParameter> parameters)
        {
            entry ??= new BlueprintNodeDataModel();
            entry.NodeType = BlueprintNodeType.Entry;
            entry.AssemblyQualifiedType = "";
            entry.MethodName = "";
            entry.NodeName = "Entry";
            entry.OutPorts.Clear();

            var outSlot = new BlueprintPortSlot("OUT", PortDirection.Out, typeof(BlueprintNodeDataModel), false)
                .WithDisplayName("");
            entry.OutPorts.Add("OUT", outSlot);

            foreach (var parameter in parameters)
            {
                var tuple = parameter.ToTuple();
                var slot = new BlueprintPortSlot(tuple.Item1, PortDirection.Out, tuple.Item2, false)
                    .WithAllowMultiple();
                entry.OutPorts.Add(tuple.Item1, slot);
            }

            return entry;
        }

        public static BlueprintNodeDataModel CreateOrUpdateReturnNode(BlueprintNodeDataModel ret, List<BlueprintIOParameter> parameters)
        {
            ret ??= new BlueprintNodeDataModel();
            ret.NodeType = BlueprintNodeType.Return;
            ret.AssemblyQualifiedType = "";
            ret.MethodName = "";
            ret.NodeName = "Return";
            ret.InPorts.Clear();

            var inSlot = new BlueprintPortSlot("IN", PortDirection.In, typeof(BlueprintNodeDataModel), false)
                .WithDisplayName("")
                .WithAllowMultiple();
            ret.InPorts.Add("IN", inSlot);

            foreach (var parameter in parameters)
            {
                var tuple = parameter.ToTuple();
                var slot = new BlueprintPortSlot(tuple.Item1, PortDirection.In, tuple.Item2, false);
                ret.InPorts.Add(tuple.Item1, slot);
            }

            return ret;
        }

        public static BlueprintNodeDataModel CreateOrUpdateIfElseNode(BlueprintNodeDataModel node)
        {
            node ??= new BlueprintNodeDataModel();
            node.NodeType = BlueprintNodeType.IfElse;
            node.AssemblyQualifiedType = "";
            node.MethodName = "";
            node.NodeName = "Branch";
            node.InPorts.Clear();
            node.OutPorts.Clear();
            
            var inSlot = new BlueprintPortSlot("IN", PortDirection.In, typeof(BlueprintNodeDataModel), false)
                .WithDisplayName("")
                .WithAllowMultiple();
            node.InPorts.Add("IN", inSlot);
            
            var slot = new BlueprintPortSlot("Value", PortDirection.In, typeof(bool), false);
            node.InPorts.Add("Value", slot);
            
            var trueSlot = new BlueprintPortSlot("True", PortDirection.Out, typeof(BlueprintNodeDataModel), false)
                .WithDisplayName("True");
            node.OutPorts.Add("True", trueSlot);
            
            var falseSlot = new BlueprintPortSlot("False", PortDirection.Out, typeof(BlueprintNodeDataModel), false)
                .WithDisplayName("False");
            node.OutPorts.Add("False", falseSlot);
            
            return node;
        }

        public static BlueprintNodeDataModel CreateOrUpdateForEachNode(BlueprintNodeDataModel node)
        {
            node ??= new BlueprintNodeDataModel();
            node.NodeType = BlueprintNodeType.ForEach;
            node.AssemblyQualifiedType = "";
            node.MethodName = "";
            node.NodeName = "For Each";
            node.InPorts.Clear();
            node.OutPorts.Clear();
            
            var inSlot = new BlueprintPortSlot("IN", PortDirection.In, typeof(BlueprintNodeDataModel), false)
                .WithDisplayName("")
                .WithAllowMultiple();
            node.InPorts.Add("IN", inSlot);
            
            var breakSlot = new BlueprintPortSlot("Break", PortDirection.In, typeof(BlueprintNodeDataModel), false)
                .WithDisplayName("Break")
                .WithAllowMultiple();
            node.InPorts.Add("Break", breakSlot);
            
            var arraySlot = new BlueprintPortSlot("Array", PortDirection.In, typeof(object), false)
                .WithDisplayName("Array");
            node.InPorts.Add("Array", arraySlot);
            
            var loopSlot = new BlueprintPortSlot("Loop", PortDirection.Out, typeof(BlueprintNodeDataModel), false)
                .WithDisplayName("Loop");
            node.OutPorts.Add("Loop", loopSlot);
            
            var indexSlot = new BlueprintPortSlot("Index", PortDirection.Out, typeof(int), false)
                .WithDisplayName("Index");
            node.OutPorts.Add("Index", indexSlot);
            
            var elementSlot = new BlueprintPortSlot("Element", PortDirection.Out, typeof(object), false)
                .WithDisplayName("Element");
            node.OutPorts.Add("Element", elementSlot);
            
            var completedSlot = new BlueprintPortSlot("Complete", PortDirection.Out, typeof(BlueprintNodeDataModel), false)
                .WithDisplayName("Complete");
            node.OutPorts.Add("Complete", completedSlot);
            
            return node;
        }

        public static BlueprintNodeDataModel CreateOrUpdateGetterNode(BlueprintNodeDataModel get, BlueprintIOParameter parameter, Action<BlueprintPortReference,BlueprintPortReference> RenamePortCallback = null)
        {
            get ??= new BlueprintNodeDataModel();
            get.NodeType = BlueprintNodeType.Getter;
            get.AssemblyQualifiedType = "";
            var tuple = parameter.ToTuple();
            get.MethodName = tuple.Item1;
            get.NodeName = $"Get <b><i>{tuple.Item1}</i></b>";
            get.OutPorts.Clear();

            var edgeToUpdateIdx = get.OutEdges.FindIndex(e => e.LeftSidePort.PortName == parameter.PreviousName);
            if (edgeToUpdateIdx != -1)
            {
                var oldEdge = get.OutEdges[edgeToUpdateIdx];
                var oldPort = oldEdge.LeftSidePort;
                var newPort = new BlueprintPortReference(tuple.Item1, oldEdge.LeftSidePort.NodeGuid, oldEdge.LeftSidePort.IsTransitionPort);
                get.OutEdges[edgeToUpdateIdx] = new BlueprintEdgeConnection(
                    newPort,
                    oldEdge.RightSidePort);
                RenamePortCallback?.Invoke(oldPort, newPort);
            }
            parameter.PreviousName = tuple.Item1;
            
            var slot = new BlueprintPortSlot(tuple.Item1, PortDirection.Out, tuple.Item2, false)
                .WithDisplayName("")
                .WithAllowMultiple();
            get.OutPorts.Add(tuple.Item1, slot);

            return get;
        }

        public static BlueprintNodeDataModel CreateOrUpdateSetterNode(BlueprintNodeDataModel set, BlueprintIOParameter parameter)
        {
            set ??= new BlueprintNodeDataModel();
            set.NodeType = BlueprintNodeType.Setter;
            set.AssemblyQualifiedType = "";
            var tuple = parameter.ToTuple();
            set.MethodName = tuple.Item1;
            set.NodeName = $"Set <b><i>{tuple.Item1}</i></b>";
            set.InPorts.Clear();
            set.OutPorts.Clear();
            
            var inSlot = new BlueprintPortSlot("IN", PortDirection.In, typeof(BlueprintNodeDataModel), false)
                .WithDisplayName("")
                .WithAllowMultiple();
            set.InPorts.Add("IN", inSlot);
            
            var inData = new BlueprintPortSlot(tuple.Item1, PortDirection.In, tuple.Item2, false)
                .WithDisplayName("");
            set.InPorts.Add(tuple.Item1, inData);
            
            var outSlot = new BlueprintPortSlot("OUT", PortDirection.Out, typeof(BlueprintNodeDataModel), false)
                .WithDisplayName("");
            set.OutPorts.Add("OUT", outSlot);
            
            var os = new BlueprintPortSlot(tuple.Item1, PortDirection.Out, tuple.Item2, false)
                .WithDisplayName("")
                .WithAllowMultiple();
            set.OutPorts.Add(tuple.Item1, os);
            
            return set;
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
    
    public class BlueprintPortSlot
    {
        private static readonly HashSet<Type> s_ValidTypes = new()
        {
            // Primitive Types
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(char),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),

            // Other Common Value Types
            typeof(string), // Immutable reference type but often treated like a value type

            // Unity Serializable Value Types
            typeof(Vector2),
            typeof(Vector2Int),
            typeof(Vector3),
            typeof(Vector3Int),
            typeof(Vector4),
            typeof(Quaternion),
            typeof(Color),
            typeof(Color32),
            typeof(Rect),
            typeof(RectInt),
            typeof(Bounds),
            typeof(BoundsInt),
            typeof(Matrix4x4),
            typeof(AnimationCurve),
            typeof(LayerMask),
            typeof(Gradient),
        };
        
        public string PortName { get; }
        public string DisplayName { get; private set; }
        public PortDirection Direction { get; }
        public Type Type { get; }
        public bool IsTransitionPort { get; }
        public bool IsOptional { get; }
        public bool AllowMultiple { get; private set; }

        public bool HasContent { get; }
        public Type ContentType { get; }
        public object Content;

        [NonSerialized]
        private FieldInfo _contentFieldInfo;
        public FieldInfo ContentFieldInfo
        {
            get
            {
                if (_contentFieldInfo != null)
                {
                    return _contentFieldInfo;
                }

                _contentFieldInfo = GetType().GetField("Content", BindingFlags.Public | BindingFlags.Instance);
                return _contentFieldInfo;
            }
        }
        
        public BlueprintPortSlot(string portName, PortDirection direction, Type type, bool isOptional)
        {
            PortName = portName;
            DisplayName = portName;
            Direction = direction;
            Type = type;
            IsOptional = isOptional;
            if (type == typeof(BlueprintNodeDataModel))
            {
                IsTransitionPort = true;
                return;
            }

            if (!s_ValidTypes.Contains(type))
            {
                return;
            }

            HasContent = true;
            ContentType = type;
            if (type == typeof(string))
            {
                Content = string.Empty;
            }
            else
            {
                Content = Activator.CreateInstance(type);
            }
        }

        public BlueprintPortSlot WithDisplayName(string displayName)
        {
            DisplayName = displayName;
            return this;
        }

        public BlueprintPortSlot WithAllowMultiple()
        {
            AllowMultiple = true;
            return this;
        }
        
        public void SetDefaultValue(object value)
        {
            Assert.IsTrue(value.GetType() == ContentType, $"Value type [{value.GetType()}] is not the content type [{ContentType}]");
            Content = value;
        }
    }
    
    [Serializable]
    public struct BlueprintPortReference : IEquatable<BlueprintPortReference>
    {
        public string PortName;
        public string NodeGuid;
        public bool IsTransitionPort;

        public BlueprintPortReference(string portName, string nodeGuid, bool transitionPort)
        {
            PortName = portName;
            NodeGuid = nodeGuid;
            IsTransitionPort = transitionPort;
        }

        public override bool Equals(object obj)
        {
            return obj is BlueprintPortReference slot && Equals(slot);
        }

        public bool Equals(BlueprintPortReference other)
        {
            return NodeGuid == other.NodeGuid && PortName == other.PortName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(NodeGuid, PortName);
        }

        public bool IsValid()
        {
            if (NodeGuid.EmptyOrNull())
            {
                return false;
            }
            return !PortName.EmptyOrNull();
        }
    }

    [Serializable]
    public struct BlueprintEdgeConnection : IEquatable<BlueprintEdgeConnection>
    {
        public BlueprintPortReference LeftSidePort;
        public BlueprintPortReference RightSidePort;

        public BlueprintEdgeConnection(BlueprintPortReference leftSidePort, BlueprintPortReference rightSidePort)
        {
            LeftSidePort = leftSidePort;
            RightSidePort = rightSidePort;
        }

        public bool RightGuidMatches(string guid) => RightSidePort.NodeGuid == guid;
        public bool LeftGuidMatches(string guid) => LeftSidePort.NodeGuid == guid;

        public override bool Equals(object obj)
        {
            return obj is BlueprintEdgeConnection connection && Equals(connection);
        }

        public bool Equals(BlueprintEdgeConnection other)
        {
            return LeftSidePort.Equals(other.LeftSidePort) &&
                   RightSidePort.Equals(other.RightSidePort);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(LeftSidePort, RightSidePort);
        }
    }

    [Serializable]
    internal class BlueprintInputDataContainer
    {
        public string PortName;
        public object Content;
    }
}