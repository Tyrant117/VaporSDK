using Vapor.Keys;

namespace Vapor.Graphs
{
    public class NamedKeyValueNode : UnityObjectValueNode<NamedKeySo>
    {
        public virtual int FromID { get; } = -1;
    }
}
