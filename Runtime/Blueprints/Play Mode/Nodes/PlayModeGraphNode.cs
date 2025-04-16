using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vapor.Inspector;
using Vapor.Keys;

namespace Vapor.Blueprints
{
    public class PlayModeGraphNode : PlayModeNodeBase
    {
        private readonly PlayModeClass _graph;
        private readonly object[] _parameterValues;
        
        private readonly string _nextNodeGuid;
        private PlayModeNodeBase _nextNode;

        public PlayModeGraphNode(BlueprintDesignNodeDto dto, List<BlueprintWireDto> wires)
        {
            Guid = dto.Guid;
            Uuid = Guid.GetStableHashU32();
            
            SetupWires(wires);
            SetupInputPins(dto);
            SetupOutputPins(dto);
            
            _parameterValues = new object[dto.InputPins.Count];
            _nextNodeGuid = GetOutputNodeGuidForPinName(OutputWires);
        }

        public override void Init(PlayModeClass playModeClass, PlayModeMethod playModeMethod)
        {
            base.Init(playModeClass, playModeMethod);
            if (!_nextNodeGuid.EmptyOrNull())
            {
                _nextNode = Method.GetNode(_nextNodeGuid);
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

            // _graph.Invoke(_parameterValues, null);
        }

        protected override void WriteOutputValues()
        {
            // foreach (var param in _graph.GetResults())
            // {
            //     if (OutPortValues.ContainsKey(param.Key))
            //     {
            //         OutPortValues[param.Key] = param.Value;
            //     }
            //     else
            //     {
            //         Debug.LogError($"Failed to get output value for {param.Key}");
            //     }
            // }
        }

        protected override void Continue()
        {
            if (!Method.IsEvaluating)
            {
                return;
            }
            
            _nextNode?.InvokeAndContinue();
        }
    }
}