using System;

namespace Vapor.Graphs
{
    [Serializable]
    public class EdgeConnection
    {
        public int InPortIndex;
        public int OutPortIndex;
        public string Guid;

        public EdgeConnection(int inPortIndex, int outPortIndex, string guid)
        {
            InPortIndex = inPortIndex;
            OutPortIndex = outPortIndex;
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
                   OutPortIndex == other.OutPortIndex;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Guid, OutPortIndex);
        }
    }
}
