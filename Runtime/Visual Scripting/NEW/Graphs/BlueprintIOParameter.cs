using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    [Serializable, DrawWithVapor(UIGroupType.Vertical)]
    public class BlueprintIOParameter
    {
        [HorizontalGroup("H"), OnValueChanged("OnChanged", false, true)]
        public string FieldName = string.Empty;
        [HorizontalGroup("H"), TypeSelector("@GetValidTypes"), OnValueChanged("OnChanged", false)]
        public string FieldType = typeof(double).AssemblyQualifiedName;

        [SerializeField, HideInInspector] private string _previousName;
        public string PreviousName
        {
            get => _previousName;
            set => _previousName = value;
        }
        
        private void OnChanged(string old, string @new)
        {
            _previousName = old;
        }
        
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
                typeof(char),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(float),
                typeof(double),
                typeof(decimal),

                // Other Common Value Types
                typeof(string), // Immutable reference type but often treated like a value type

                // Unity Serializable Value Types
                typeof(Vector2),
                typeof(Vector2Int),
                typeof(Vector3),
                typeof(Vector3Int),
                typeof(Vector4),
                typeof(Quaternion),
                typeof(Color),
                typeof(Color32),
                typeof(Rect),
                typeof(RectInt),
                typeof(Bounds),
                typeof(BoundsInt),
                typeof(Matrix4x4),
                typeof(AnimationCurve),
                typeof(LayerMask),
                typeof(Gradient),
            };
            
#if UNITY_EDITOR
            // Add all types deriving from ScriptableObjects and Components
            types.AddRangeUnique(UnityEditor.TypeCache.GetTypesWithAttribute<BlueprintableAttribute>());
            types.AddRangeUnique(UnityEditor.TypeCache.GetTypesDerivedFrom<Component>());
            types.AddRangeUnique(UnityEditor.TypeCache.GetTypesDerivedFrom<ScriptableObject>());
#endif
            return types;
        }

        public (string, Type) ToTuple()
        {
            return FieldName == null || FieldType == null ? (string.Empty, typeof(double)) : (FieldName, Type.GetType(FieldType));
        }
        
        
    }
}