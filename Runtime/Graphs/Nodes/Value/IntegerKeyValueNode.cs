using Vapor.Keys;

namespace Vapor.Graphs
{
    public class IntegerKeyValueNode : UnityObjectValueNode<IntegerKeySo>
    {      
        public virtual int FromID { get; } = -1;
    }
}
