using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    [Serializable, DrawWithVapor(UIGroupType.Vertical)]
    public class BlueprintIOParameter
    {
        [OnValueChanged("OnChanged", false, true)]
        public string FieldName = string.Empty;
        // [OnValueChanged("OnSubclassChanged", true)]
        public SubclassOf FieldType;
        // [ShowIf("@IsGenericType")]
        // public SubclassOf[] GenericValueType;
        //TODO Add an OnChanged for field type and update the respective nodes field types
        
        // public bool IsGenericTypeDefinition => FieldType.GetPinType().IsGenericTypeDefinition;

        [SerializeField, HideInInspector] private string _previousName;
        public string PreviousName
        {
            get => _previousName;
            set => _previousName = value;
        }
        
        private void OnChanged(string old, string _)
        {
            _previousName = old;
        }

        // private void OnSubclassChanged(SubclassOf current)
        // {
        //     if (current == null)
        //     {
        //         return;
        //     }
        //     
        //     if (current.GetPinType().IsGenericType)
        //     {
        //         GenericValueType = new SubclassOf[current.GetPinType().GetGenericArguments().Length];
        //     }
        // }
        
        public static IEnumerable<Type> GetValidTypes()
        {
            // Add All Primitive Types and Unity Value Types
            // Add All Primitive Types and Unity Value Types
            List<Type> types = new()
            {
                // Primitive Types
                typeof(bool),
                typeof(byte),
                typeof(sbyte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(float),
                typeof(double),

                // Other Common Value Types
                typeof(string), // Immutable reference type but often treated like a value type

                // Unity Serializable Value Types
                typeof(Vector2),
                typeof(Vector2Int),
                typeof(Vector3),
                typeof(Vector3Int),
                typeof(Vector4),
                typeof(Color),
                typeof(Gradient),
                typeof(Rect),
                typeof(RectInt),
                typeof(Bounds),
                typeof(BoundsInt),
                typeof(LayerMask),
                typeof(RenderingLayerMask),
                typeof(AnimationCurve),
                typeof(Hash128),
            };
            
#if UNITY_EDITOR
            // Add all types deriving from ScriptableObjects and Components
            types.AddRangeUnique(UnityEditor.TypeCache.GetTypesDerivedFrom<FieldWrapper>());
            types.AddRangeUnique(UnityEditor.TypeCache.GetTypesWithAttribute<BlueprintableAttribute>());
            types.AddRangeUnique(UnityEditor.TypeCache.GetTypesDerivedFrom<Component>());
            types.AddRangeUnique(UnityEditor.TypeCache.GetTypesDerivedFrom<ScriptableObject>());
#endif
            return types;
        }

        public (string fieldName, Type fieldType) ToParameter()
        {
            if (FieldName == null || FieldType == null)
            {
                return (string.Empty, typeof(ExecutePin));
            }

            return (FieldName, FieldType.GetResolvedType());
            // return IsGenericTypeDefinition 
            //     ? (FieldName, FieldType.GetPinType().MakeGenericType(GenericValueType.Select(s => s.GetPinType()).ToArray())) 
            //     : (FieldName, FieldType.GetPinType());
        }
    }
}