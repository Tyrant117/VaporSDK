using System.Runtime.CompilerServices;
using UnityEngine;

namespace Vapor.GraphTools
{
    public abstract class ValueNodeSo<T> : PropertyNodeSo
    {
        [SerializeField]
        private T _value;
        public virtual T Value { get => _value; set => _value = value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override object GetBoxedValue() => Value;
    }
}
