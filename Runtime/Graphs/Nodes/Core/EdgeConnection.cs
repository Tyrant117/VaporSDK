using System;

namespace Vapor.Graphs
{
    [Serializable]
    public class EdgeConnection : IEquatable<EdgeConnection>
    {
        public string InPortName;
        public string OutPortName;
        public string Guid;

        public EdgeConnection(string inPortName, string outPortName, string guid)
        {
            InPortName = inPortName;
            OutPortName = outPortName;
            Guid = guid;
        }

        public bool GuidMatches(string guid) => Guid == guid;

        public override bool Equals(object obj)
        {
            return obj is EdgeConnection connection && Equals(connection);
        }

        public bool Equals(EdgeConnection other)
        {
            if (ReferenceEquals(this, other))
                return true;

            return Guid == other.Guid &&
                   OutPortName == other.OutPortName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Guid, OutPortName);
        }
    }
}
