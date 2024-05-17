using System.Runtime.CompilerServices;
using Vapor.Keys;

namespace Vapor.GraphTools
{
    public class NamedKeyValueNodeSo : ValueNodeSo<NamedKeySo>
    {
        public virtual int FromID { get; } = -1;
    }
}
