using System;

namespace Vapor.VisualScripting
{
    [Serializable]
    public readonly struct SlotReference : IEquatable<SlotReference>
    {
        public readonly string SlotName;
        public readonly string NodeGuid;
        public readonly bool IsValid;

        public SlotReference(string slotName, string nodeGuid)
        {
            SlotName = slotName;
            NodeGuid = nodeGuid;
            IsValid = true;
        }

        public override bool Equals(object obj)
        {
            return obj is SlotReference slot && Equals(slot);
        }

        public bool Equals(SlotReference other)
        {
            return SlotName == other.SlotName && NodeGuid == other.NodeGuid;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SlotName, NodeGuid, IsValid);
        }
    }

    [Serializable]
    public readonly struct EdgeConnection : IEquatable<EdgeConnection>
    {
        public readonly SlotReference InSlot;
        public readonly SlotReference OutSlot;
        public readonly bool IsValid;

        public EdgeConnection(SlotReference inSlot, SlotReference outSlot)
        {
            InSlot = inSlot;
            OutSlot = outSlot;
            IsValid = true;
        }

        public bool OutputGuidMatches(string guid) => OutSlot.IsValid && OutSlot.NodeGuid == guid;
        public bool InputGuidMatches(string guid) => InSlot.IsValid && InSlot.NodeGuid == guid;

        public override bool Equals(object obj)
        {
            return obj is EdgeConnection connection && Equals(connection);
        }

        public bool Equals(EdgeConnection other)
        {
            return InSlot.Equals(other.InSlot) &&
                   OutSlot.Equals(other.OutSlot);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(InSlot, OutSlot, IsValid);
        }
    }
}
