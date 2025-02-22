using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vapor.Inspector;
using Vapor.Keys;

namespace Vapor.Blueprints
{
    public class BlueprintGraphNode : BlueprintBaseNode
    {
        private readonly IBlueprintGraph _graph;
        private readonly object[] _parameterValues;
        
        private readonly string _nextNodeGuid;
        private BlueprintBaseNode _nextNode;

        public BlueprintGraphNode(BlueprintNodeDataModel dataModel)
        {
            Guid = dataModel.Guid;
            InEdges = dataModel.InEdges;
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                _graph = RuntimeDataStore<IBlueprintGraph>.Get(dataModel.IntData);
            }
            else
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(dataModel.MethodName);
                var found = UnityEditor.AssetDatabase.LoadAssetAtPath<BlueprintGraphSo>(path);
                found.Validate();
                _graph = new BlueprintFunctionGraph(found);
            }
#else
            _graph = RuntimeDataStore<IBlueprintGraph>.Get(dataModel.IntData);
#endif
            
            InPortValues = new Dictionary<string, object>(dataModel.InPorts.Count);
            int paramCount = 0;
            foreach (var inPort in dataModel.InPorts.Values)
            {
                if (inPort.HasInlineValue)
                {
                    InPortValues[inPort.PortName] = inPort.GetContent();
                }

                if (!inPort.IsExecutePin)
                {
                    paramCount++;
                }
            }
            _parameterValues = new object[paramCount];
            
            OutPortValues = new Dictionary<string, object>(dataModel.OutPorts.Count);
            foreach (var outPort in dataModel.OutPorts.Values)
            {
                if (!outPort.IsExecutePin)
                {
                    OutPortValues[outPort.PortName] = null;
                }
            }
            
            var outEdge = dataModel.OutEdges.FirstOrDefault(x => x.LeftSidePin.PinName == "OUT");
            if (outEdge.RightSidePin.IsValid())
            {
                _nextNodeGuid = outEdge.RightSidePin.NodeGuid;
            }
        }

        public BlueprintGraphNode(BlueprintCompiledNodeDto dto, int graphKey, string assetGuid, string nextNodeGuid)
        {
            Guid = dto.Guid;
            InEdges = dto.InputWires;
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                _graph = RuntimeDataStore<IBlueprintGraph>.Get(graphKey);
            }
            else
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
                var found = UnityEditor.AssetDatabase.LoadAssetAtPath<BlueprintGraphSo>(path);
                found.Validate();
                _graph = new BlueprintFunctionGraph(found);
            }
#else
            _graph = RuntimeDataStore<IBlueprintGraph>.Get(graphKey);
#endif

            InPortValues = new Dictionary<string, object>(dto.InputPinValues.Count);
            int paramCount = 0;
            foreach (var (key, tuple) in dto.InputPinValues)
            {
                if (!key.EmptyOrNull())
                {
                    var val = Convert.ChangeType(tuple.Item2, tuple.Item1);
                    InPortValues[key] = val;
                }

                paramCount++;
            }

            _parameterValues = new object[paramCount];

            OutPortValues = new Dictionary<string, object>(dto.OutputPinNames.Count);
            foreach (var outPort in dto.OutputPinNames)
            {
                OutPortValues[outPort] = null;
            }

            _nextNodeGuid = nextNodeGuid;
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
                if (edge.LeftSidePin.IsExecutePin)
                {
                    continue;
                }
                
                if (!Graph.TryGetNode(edge.LeftSidePin.NodeGuid, out var leftNode))
                {
                    continue;
                }

                leftNode.Invoke();
                if (leftNode.TryGetOutputValue(edge.LeftSidePin.PinName, out var outputValue))
                {
                    InPortValues[edge.RightSidePin.PinName] = outputValue;
                }
            }

            int idx = 0;
            foreach (var param in InPortValues.Values)
            {
                _parameterValues[idx] = param;
                idx++;
            }

            _graph.Invoke(_parameterValues, null);
        }

        protected override void WriteOutputValues()
        {
            foreach (var param in _graph.GetResults())
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
}