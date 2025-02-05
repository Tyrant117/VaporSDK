using System;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    [Serializable, ArrayEntryName("@GetArrayName")]
    public struct BlueprintWireReference : IEquatable<BlueprintWireReference>
    {
        public BlueprintPinReference LeftSidePin;
        public BlueprintPinReference RightSidePin;
        
        public BlueprintWireReference(BlueprintPinReference leftSidePin, BlueprintPinReference rightSidePin)
        {
            LeftSidePin = leftSidePin;
            RightSidePin = rightSidePin;
        }

        private string GetArrayName() => $"{LeftSidePin.PinName} -> {RightSidePin.PinName}";

        public override bool Equals(object obj)
        {
            return obj is BlueprintWireReference connection && Equals(connection);
        }

        public bool Equals(BlueprintWireReference other)
        {
            return LeftSidePin.Equals(other.LeftSidePin) &&
                   RightSidePin.Equals(other.RightSidePin);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(LeftSidePin, RightSidePin);
        }
        
        public bool IsValid() => LeftSidePin.IsValid() && RightSidePin.IsValid();
    }
}