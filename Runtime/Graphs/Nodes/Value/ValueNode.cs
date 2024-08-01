using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Vapor.Graphs
{
    [Serializable]
    public abstract class ValueNode : Node
    {
        [PortOut("Out", 0, true, typeof(int))]
        public NodeReference Out;

        public abstract Type GetValueType();
        protected abstract object GetBoxedValue();

        /// <summary>
        /// Tries to cast the boxed value of <see cref="GetBoxedValue"/> to the desired type.
        /// </summary>
        /// <typeparam name="T">The type to cast to</typeparam>
        /// <param name="value">The output value, default(T) if not successful</param>
        /// <returns>True if the cast was successful</returns>
        public bool TryGetValue<T>(out T value)
        {
            var boxed = GetBoxedValue();
            if (boxed is T val)
            {
                value = val;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }

    [Serializable]
    public abstract class ValueNode<T> : ValueNode
    {
        [SerializeField]
        private T _value;
        public virtual T Value { get => _value; set => _value = value; }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override object GetBoxedValue() => Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Type GetValueType() => typeof(T);
    }

    [Serializable]
    public abstract class UnityObjectValueNode<T> : ValueNode<T> where T : UnityEngine.Object
    {
        public string ObjectGuid;
        public long LocalId;

        public override T Value
        {
            get => base.Value;
            set
            {
#if UNITY_EDITOR
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out ObjectGuid, out LocalId);
#endif
                base.Value = value;
            }
        }
    }
}
