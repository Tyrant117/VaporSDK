using System;
using System.Runtime.CompilerServices;
using Vapor.Keys;

namespace Vapor.GraphTools
{
    public class NamedKeyValueNodeSo : ValueNodeSo<NamedKeySo>
    {
        [PortOut("Out", 0, true, typeof(int))]
        public NodeSo Out;
        public int OutConnectedPort_Out;

        public virtual int FromID { get; } = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Type GetValueType() => typeof(NamedKeySo);
    }
}
