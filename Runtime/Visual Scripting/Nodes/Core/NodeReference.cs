using System;

namespace Vapor.VisualScripting
{
    [Serializable]
    public struct NodeReference : IEquatable<NodeReference>
    {
        public string Guid;
        public string PortName;
        public int PortIndex;

        public NodeReference(string guid, string portName, int portIndex)
        {
            Guid = guid;
            PortName = portName;
            PortIndex = portIndex;
        }

        public readonly bool Equals(NodeReference other) => Guid == other.Guid;

        public override readonly bool Equals(object obj) => obj is NodeReference reff && Equals(reff);

        public override readonly int GetHashCode() => HashCode.Combine(Guid);

        public override readonly string ToString() => $"{Guid} - {PortName} - {PortIndex}";
    }
}
