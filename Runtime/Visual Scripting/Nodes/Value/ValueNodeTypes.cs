using System;
using UnityEngine;

namespace Vapor.VisualScripting
{
    #region Native Types
    public class TypeNode : INode, IValueNode<Type>
    {
        public uint Id { get; }
        public Type Value { get; }

        public TypeNode(string guid, Type value)
        {
            Id = guid.GetStableHashU32();
            Value = value;
        }

        public Type GetValue(int portIndex) => Value;
    }
    [Serializable]
    public class TypeValueNode : ValueNode<Type> { }

    public class BoolNode : INode, IValueNode<bool>
    {
        public uint Id { get; }
        public bool Value { get; }

        public BoolNode(string guid, bool value)
        {
            Id = guid.GetStableHashU32();
            Value = value;
        }

        public bool GetValue(int portIndex) => Value;
    }
    [Serializable]
    public class BoolValueNode : ValueNode<bool> { }

    public class ULongNode : INode, IValueNode<ulong>
    {
        public uint Id { get; }
        public ulong Value { get; }

        public ULongNode(string guid, ulong value)
        {
            Id = guid.GetStableHashU32();
            Value = value;
        }

        public ulong GetValue(int portIndex) => Value;
    }
    [Serializable]
    public class ULongValueNode : ValueNode<ulong> { }

    public class StringNode : INode, IValueNode<string>
    {
        public uint Id { get; }
        public string Value { get; }

        public StringNode(string guid, string value)
        {
            Id = guid.GetStableHashU32();
            Value = value;
        }

        public string GetValue(int portIndex) => Value;
    }
    [Serializable]
    public class StringValueNode : ValueNode<string> { }
    #endregion

    #region Unity Types
    public class Vector2Node : INode, IValueNode<Vector2>
    {
        public uint Id { get; }
        public Vector2 Value { get; }

        public Vector2Node(string guid, Vector2 value)
        {
            Id = guid.GetStableHashU32();
            Value = value;
        }

        public Vector2 GetValue(int portIndex) => Value;
    }
    [Serializable]
    public class Vector2ValueNode : ValueNode<Vector2> { }

    public class Vector2IntNode : INode, IValueNode<Vector2Int>
    {
        public uint Id { get; }
        public Vector2Int Value { get; }

        public Vector2IntNode(string guid, Vector2Int value)
        {
            Id = guid.GetStableHashU32();
            Value = value;
        }

        public Vector2Int GetValue(int portIndex) => Value;
    }
    [Serializable]
    public class Vector2IntValueNode : ValueNode<Vector2Int> { }

    public class Vector3Node : INode, IValueNode<Vector3>
    {
        public uint Id { get; }
        public Vector3 Value { get; }

        public Vector3Node(string guid, Vector3 value)
        {
            Id = guid.GetStableHashU32();
            Value = value;
        }

        public Vector3 GetValue(int portIndex) => Value;
    }
    [Serializable]
    public class Vector3ValueNode : ValueNode<Vector3> { }

    public class Vector3IntNode : INode, IValueNode<Vector3Int>
    {
        public uint Id { get; }
        public Vector3Int Value { get; }

        public Vector3IntNode(string guid, Vector3Int value)
        {
            Id = guid.GetStableHashU32();
            Value = value;
        }

        public Vector3Int GetValue(int portIndex) => Value;
    }
    [Serializable]
    public class Vector3IntValueNode : ValueNode<Vector3Int> { }

    public class QuaternionNode : INode, IValueNode<Quaternion>
    {
        public uint Id { get; }
        public Quaternion Value { get; }

        public QuaternionNode(string guid, Quaternion value)
        {
            Id = guid.GetStableHashU32();
            Value = value;
        }

        public Quaternion GetValue(int portIndex) => Value;
    }
    [Serializable, SearchableNode("Value Types/Quaternion", "Quaternion")]
    public class QuaternionValueNode : ValueNode<Quaternion> { }

    public class ColorNode : INode, IValueNode<Color>
    {
        public uint Id { get; }
        public Color Value { get; }

        public ColorNode(string guid, Color value)
        {
            Id = guid.GetStableHashU32();
            Value = value;
        }

        public Color GetValue(int portIndex) => Value;
    }
    [Serializable, SearchableNode("Value Types/Color", "Color")]
    public class ColorValueNode : ValueNode<Color> { }
    #endregion
}
