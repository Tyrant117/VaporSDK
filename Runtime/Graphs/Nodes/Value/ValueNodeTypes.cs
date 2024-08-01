using System;
using UnityEngine;

namespace Vapor.Graphs
{
    #region Native Types
    [Serializable]
    public class TypeValueNode : ValueNode<Type> { }

    [Serializable]
    public class ByteValueNode : ValueNode<byte> { }

    [Serializable]
    public class ShortValueNode : ValueNode<short> { }

    [Serializable]
    public class UShortValueNode : ValueNode<ushort> { }

    [Serializable]
    public class IntValueNode : ValueNode<int> { }

    [Serializable]
    public class UIntValueNode : ValueNode<uint> { }

    [Serializable]
    public class LongValueNode : ValueNode<long> { }

    [Serializable]
    public class ULongValueNode : ValueNode<ulong> { }

    [Serializable]
    public class FloatValueNode : ValueNode<float> { }

    [Serializable]
    public class DoubleValueNode : ValueNode<double> { }

    [Serializable]
    public class StringValueNode : ValueNode<string> { }
    #endregion

    #region Unity Types
    [Serializable]
    public class Vector2ValueNode : ValueNode<Vector2> { }

    [Serializable]
    public class Vector2IntValueNode : ValueNode<Vector2Int> { }

    [Serializable]
    public class Vector3ValueNode : ValueNode<Vector3> { }

    [Serializable]
    public class Vector3IntValueNode : ValueNode<Vector3Int> { }

    [Serializable]
    public class QuaternionValueNode : ValueNode<Quaternion> { }

    [Serializable]
    public class ColorValueNode : ValueNode<Color> { }
    #endregion
}
