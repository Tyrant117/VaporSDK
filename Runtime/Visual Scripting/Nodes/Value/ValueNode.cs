using Newtonsoft.Json;
using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.VisualScripting
{
    //[Serializable]
    //public abstract class ValueNode : NodeModel
    //{       
    //    public abstract Type GetValueType();
    //    protected abstract object GetBoxedValue();

    //    /// <summary>
    //    /// Tries to cast the boxed value of <see cref="GetBoxedValue"/> to the desired type.
    //    /// </summary>
    //    /// <typeparam name="T">The type to cast to</typeparam>
    //    /// <param name="value">The output value, default(T) if not successful</param>
    //    /// <returns>True if the cast was successful</returns>
    //    public bool TryGetValue<T>(out T value)
    //    {
    //        var boxed = GetBoxedValue();
    //        if (boxed is T val)
    //        {
    //            value = val;
    //            return true;
    //        }
    //        else
    //        {
    //            value = default;
    //            return false;
    //        }
    //    }
    //}

    [Serializable]
    public abstract class ValueNode<T> : NodeModel
    {       
        [NodeContent]
        public T Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual T GetValue() { return Value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SetValue(T value) { Value = value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Type GetValueType() => typeof(T);
    }

    [Serializable]
    public abstract class UnityObjectValueNode<T> : ValueNode<T>, ISerializationCallbackReceiver where T : UnityEngine.Object
    {
        public string ObjectGuid;
        public long LocalId;

        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            if(!ObjectGuid.EmptyOrNull())
            {
                SetValue(AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(ObjectGuid)));
            }
#endif
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (Value)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Value, out ObjectGuid, out LocalId);
            }
            else
            {
                ObjectGuid = string.Empty;
            }
#endif
        }

        public override void SetValue(T value)
        {
#if UNITY_EDITOR
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out ObjectGuid, out LocalId);
#endif
            base.SetValue(value);
        }
    }
}
