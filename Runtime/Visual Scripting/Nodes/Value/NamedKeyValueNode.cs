using Vapor.Keys;

namespace Vapor.VisualScripting
{
    public class NamedKeyValueNode : UnityObjectValueNode<NamedKeySo>
    {
        public virtual int FromID { get; } = -1;
    }
}
