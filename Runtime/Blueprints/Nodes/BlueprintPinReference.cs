using System;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    [Serializable, DrawWithVapor(UIGroupType.Title)]
    public struct BlueprintPinReference : IEquatable<BlueprintPinReference>
    {
        public string PinName;
        public string NodeGuid;
        public bool IsExecutePin;

        public BlueprintPinReference(string pinName, string nodeGuid, bool executePin)
        {
            PinName = pinName;
            NodeGuid = nodeGuid;
            IsExecutePin = executePin;
        }

        public override bool Equals(object obj)
        {
            return obj is BlueprintPinReference slot && Equals(slot);
        }

        public bool Equals(BlueprintPinReference other)
        {
            return NodeGuid == other.NodeGuid && PinName == other.PinName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(NodeGuid, PinName);
        }

        public bool IsValid()
        {
            if (NodeGuid.EmptyOrNull())
            {
                return false;
            }
            return !PinName.EmptyOrNull();
        }
    }
}