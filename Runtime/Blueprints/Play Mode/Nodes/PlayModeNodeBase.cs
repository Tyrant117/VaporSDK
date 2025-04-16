using System;
using System.Collections.Generic;
using System.Linq;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    public abstract class PlayModeNodeBase
    {
        protected PlayModeClass Class { get; set; }
        protected PlayModeMethod Method { get; set; }
        public string Guid { get; protected set; }
        public uint Uuid { get; protected set; }

        protected readonly List<BlueprintWireDto> InputWires = new();
        protected readonly List<BlueprintWireDto> OutputWires = new();
        protected Dictionary<string, object> InPortValues;
        protected Dictionary<string, object> OutPortValues;

        public virtual void Init(PlayModeClass playModeClass, PlayModeMethod playModeMethod)
        {
            Class = playModeClass;
            Method = playModeMethod;
        }

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

        public virtual bool TryGetOutputValue(string outPortName, out object outputValue)
        {
            return OutPortValues.TryGetValue(outPortName, out outputValue);
        }

        protected void SetupWires(List<BlueprintWireDto> wires)
        {
            foreach (var wire in wires)
            {
                if (wire.RightGuid == Guid)
                {
                    InputWires.Add(wire);
                }

                if (wire.LeftGuid == Guid)
                {
                    OutputWires.Add(wire);
                }
            }
        }
        
        protected void SetupInputPins(BlueprintDesignNodeDto dto, Action<BlueprintPinDto> visited = null)
        {
            InPortValues = new Dictionary<string, object>(dto.InputPins.Count);
            foreach (var pin in dto.InputPins)
            {
                var val = TypeUtility.CastToType(pin.Content, pin.PinType);
                InPortValues[pin.PinName] = val;
                visited?.Invoke(pin);
            }
        }

        protected void SetupOutputPins(BlueprintDesignNodeDto dto)
        {
            OutPortValues = new Dictionary<string, object>(dto.OutputPins.Count);
            foreach (var outPort in dto.OutputPins)
            {
                OutPortValues[outPort.PinName] = null;
            }
        }

        protected static string GetOutputNodeGuidForPinName(List<BlueprintWireDto> wires, string pinName = PinNames.EXECUTE_OUT)
        {
            var wire = wires.FirstOrDefault(w => w.LeftName == pinName);
            return wire.Guid.EmptyOrNull() ? null : wire.RightGuid;
        }

        protected static string GetInputNodeGuidForPinName(List<BlueprintWireDto> wires, string pinName = PinNames.EXECUTE_IN)
        {
            var wire = wires.FirstOrDefault(w => w.RightName == pinName);
            return wire.Guid.EmptyOrNull() ? null : wire.LeftGuid;
        }

        protected void GetAllInputPinValues()
        {
            foreach (var edge in InputWires)
            {
                if (edge.IsExecuteWire)
                {
                    continue;
                }

                var leftNode = Method.GetNode(edge.LeftGuid);
                leftNode.Invoke();
                if (leftNode.TryGetOutputValue(edge.LeftName, out var outputValue))
                {
                    InPortValues[edge.RightName] = outputValue;
                }
            }
        }
    }
}