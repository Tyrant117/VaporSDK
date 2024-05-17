using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Keys;

namespace Vapor.GraphTools
{
    public class IntegerKeyValueNodeSo : ValueNodeSo<IntegerKeySo>
    {
        public virtual int FromID { get; } = -1;
    }
}
