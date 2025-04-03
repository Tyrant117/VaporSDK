using System;
using System.Collections.Generic;
using System.Linq;

namespace Vapor.Blueprints
{
    public abstract class BlueprintBaseNode
    {
        protected IBlueprintGraph Graph { get; set; }
        public string Guid { get; protected set; }
        public uint Uuid { get; protected set; }
        protected List<BlueprintWireReference> InputWires { get; set; }
        protected List<BlueprintWireReference> OutputWires { get; set; }
        
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

        public virtual bool TryGetOutputValue(string outPortName, out object outputValue)
        {
            return OutPortValues.TryGetValue(outPortName, out outputValue);
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
            OutPortValues = new Dictionary<string, object>(dto.OutputWires.Count);
            foreach (var outPort in dto.OutputWires)
            {
                OutPortValues[outPort.LeftSidePin.PinName] = null;
            }
        }

        protected static string GetNodeGuidForPinName(BlueprintDesignNodeDto dto, string pinName = PinNames.EXECUTE_OUT)
        {
            var wire = dto.OutputWires.FirstOrDefault(w => w.LeftSidePin.PinName == pinName);
            if (wire.IsValid())
            {
                return wire.RightSidePin.NodeGuid;
            }

            return null;
        }

        protected void GetAllInputPinValues()
        {
            foreach (var edge in InputWires)
            {
                if (edge.IsExecuteWire)
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
        }
    }
}