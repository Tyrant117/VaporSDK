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

        public BlueprintGraphNode(BlueprintDesignNodeDto dto)
        {
            Guid = dto.Guid;
            InputWires = dto.InputWires;
            OutputWires = dto.OutputWires;
            
            // dto.Properties.TryGetValue(INodeType.NAME_DATA_PARAM, out var assetGuid);
            // dto.Properties.TryGetValue(INodeType.KEY_DATA_PARAM, out var graphKey);
            
#if UNITY_EDITOR
            // if (Application.isPlaying)
            // {
            //     _graph = RuntimeDataStore<IBlueprintGraph>.Get((int)graphKey.Item2);
            // }
            // else
            // {
            //     var path = UnityEditor.AssetDatabase.GUIDToAssetPath((string)assetGuid.Item2);
            //     var found = UnityEditor.AssetDatabase.LoadAssetAtPath<BlueprintGraphSo>(path);
            //     found.OpenGraph();
            //     _graph = new BlueprintFunctionGraph(found);
            // }
#else
            _graph = RuntimeDataStore<IBlueprintGraph>.Get(graphKey);
#endif

            SetupInputPins(dto);
            SetupOutputPins(dto);
            
            _parameterValues = new object[dto.InputPins.Count];
            _nextNodeGuid = GetNodeGuidForPinName(dto);
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
            GetAllInputPinValues();

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