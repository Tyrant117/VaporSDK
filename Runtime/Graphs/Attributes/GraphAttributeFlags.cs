using UnityEngine;

namespace Vapor.Graphs
{
    [System.Flags]
    public enum GraphAttributeFlags
    {
        ReadOnly = 1 << 0,
        Pure = 1 << 1,
    }
}
