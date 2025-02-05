using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Vapor.Inspector;
using Vapor.Keys;

namespace Vapor.Blueprints
{
    [Serializable, DrawWithVapor(UIGroupType.Vertical)]
    public class SubclassOf : FieldWrapper
    {
        private static List<Type> s_CachedTypes;
        private static HashSet<string> s_EditorAssemblies;
        
        [SerializeField, TypeSelector("@TypeResolver")] 
        private string _assemblyQualifiedName;
        public string AssemblyQualifiedName
        {
            get => _assemblyQualifiedName;
            set => _assemblyQualifiedName = value;
        }

        public SubclassOf()
        {
            
        }
        public SubclassOf(Type assignedType)
        {
            AssemblyQualifiedName = assignedType.AssemblyQualifiedName;
        }
        public SubclassOf(string assemblyQualifiedName)
        {
            AssemblyQualifiedName = assemblyQualifiedName;
        }
        
        public override object Get() => AssemblyQualifiedName;
        public override void Set(object value)
        {
            AssemblyQualifiedName = value switch
            {
                null => null,
                Type t => t.AssemblyQualifiedName,
                _ => value as string
            };
        }

        public override Type GetPinType() => Type.GetType(AssemblyQualifiedName);
        
        protected virtual IEnumerable<Type> TypeResolver()
        {
            return GetFilteredTypes();
        }
        
        /// Default method: Returns all valid types
        public static IEnumerable<Type> GetFilteredTypes()
        {
            if (s_CachedTypes != null)
            {
                return s_CachedTypes;
            }

            s_CachedTypes = LoadValidTypes();
            return s_CachedTypes;
        }

        /// Overload: Returns types that are assignable from base type T
        public static IEnumerable<Type> GetFilteredTypes<T>()
        {
            return GetFilteredTypes().Where(t => typeof(T).IsAssignableFrom(t));
        }
        
        /// Loads valid types while filtering out editor-only assemblies
        private static List<Type> LoadValidTypes()
        {
            LoadEditorAssemblies();
            var validTypes = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // Filters for valid types
            foreach (var assembly in assemblies)
            {
                // Exclude Editor
                if (IsEditorAssembly(assembly))
                    continue;

                try
                {
                    validTypes.AddRange(assembly.GetTypes().Where(IsValidType));
                }
                catch (ReflectionTypeLoadException e)
                {
                    validTypes.AddRange(e.Types.Where(t => t != null && IsValidType(t)));
                }
            }
            return validTypes;
        }

        private static bool IsEditorAssembly(Assembly assembly)
        {
            return s_EditorAssemblies.Contains(assembly.GetName().Name);
        }

        /// Load Editor Assemblies from .asmdef Files
        private static void LoadEditorAssemblies()
        {
            if (s_EditorAssemblies != null) return; // Already loaded

            s_EditorAssemblies = new HashSet<string>();

            // Load Assemblies and Check For Editor Flag
            foreach (var asmdefFile in Directory.GetFiles(Application.dataPath, "*.asmdef", SearchOption.AllDirectories))
            {
                try
                {
                    var json = File.ReadAllText(asmdefFile);
                    var jObject = JObject.Parse(json); // ✅ Parse JSON properly

                    if (jObject.TryGetValue("includePlatforms", out JToken includePlatforms) && 
                        includePlatforms.Type == JTokenType.Array &&
                        includePlatforms.Any(p => p.ToString() == "Editor")) 
                    {
                        string assemblyName = Path.GetFileNameWithoutExtension(asmdefFile);
                        s_EditorAssemblies.Add(assemblyName);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to read asmdef file: {asmdefFile}, Error: {ex.Message}");
                }
            }
        }
        
        private static bool IsValidType(Type type)
        {
            if (type == null || type.IsNested || !type.IsPublic)
                return false;

            // Support abstract classes and interfaces and generics
            return true;
        }
    }
    
    [Serializable, DrawWithVapor(UIGroupType.Vertical)]
    public class SubclassOf<T> : SubclassOf
    {
        // ReSharper disable once StaticMemberInGenericType
        private static List<Type> s_FilteredTypes;

        public SubclassOf()
        {
        }

        public SubclassOf(Type assignedType) : base(assignedType)
        {
        }

        public SubclassOf(string assemblyQualifiedName) : base(assemblyQualifiedName)
        {
        }

        protected override IEnumerable<Type> TypeResolver()
        {
            if (s_FilteredTypes != null)
            {
                return s_FilteredTypes;
            }

            s_FilteredTypes = GetFilteredTypes<T>().ToList();
            return s_FilteredTypes;
        }
    }


    public abstract class FieldWrapper
    {
        public abstract object Get();
        public abstract void Set(object value);
        public abstract Type GetPinType();
    }

    [Serializable]
    public abstract class FieldWrapper<T> : FieldWrapper /*where T : new()*/
    {
        public abstract T WrappedObject { get; }

        public override object Get() => WrappedObject;
        public override Type GetPinType() => typeof(T);
        public override string ToString() => WrappedObject.ToString();
    }

    [Serializable]
    public class BoolWrapper : FieldWrapper<bool>
    {
        [JsonProperty, SerializeField] private bool _value;

        public override bool WrappedObject => _value;
        public override void Set(object value) => _value = (bool)value;
    }

    [Serializable]
    public class ByteWrapper : FieldWrapper<byte>
    {
        [JsonProperty, SerializeField] private byte _value;

        public override byte WrappedObject => _value;
        public override void Set(object value) => _value = (byte)value;
    }

    [Serializable]
    public class SByteWrapper : FieldWrapper<sbyte>
    {
        [JsonProperty, SerializeField] private sbyte _value;

        public override sbyte WrappedObject => _value;
        public override void Set(object value) => _value = (sbyte)value;
    }

    [Serializable]
    public class ShortWrapper : FieldWrapper<short>
    {
        [JsonProperty, SerializeField] private short _value;

        public override short WrappedObject => _value;
        public override void Set(object value) => _value = (short)value;
    }

    [Serializable]
    public class UShortWrapper : FieldWrapper<ushort>
    {
        [JsonProperty, SerializeField] private ushort _value;

        public override ushort WrappedObject => _value;
        public override void Set(object value) => _value = (ushort)value;
    }

    [Serializable]
    public class IntWrapper : FieldWrapper<int>
    {
        [JsonProperty, SerializeField] private int _value;

        public override int WrappedObject => _value;
        public override void Set(object value) => _value = (int)value;
    }

    [Serializable]
    public class UIntWrapper : FieldWrapper<uint>
    {
        [JsonProperty, SerializeField] private uint _value;

        public override uint WrappedObject => _value;
        public override void Set(object value) => _value = (uint)value;
    }

    [Serializable]
    public class LongWrapper : FieldWrapper<long>
    {
        [JsonProperty, SerializeField] private long _value;

        public override long WrappedObject => _value;
        public override void Set(object value) => _value = (long)value;
    }

    [Serializable]
    public class ULongWrapper : FieldWrapper<ulong>
    {
        [JsonProperty, SerializeField] private ulong _value;

        public override ulong WrappedObject => _value;
        public override void Set(object value) => _value = (ulong)value;
    }

    [Serializable]
    public class FloatWrapper : FieldWrapper<float>
    {
        [JsonProperty, SerializeField] private float _value;

        public override float WrappedObject => _value;
        public override void Set(object value) => _value = (float)value;
    }

    [Serializable]
    public class DoubleWrapper : FieldWrapper<double>
    {
        [JsonProperty, SerializeField] private double _value;

        public override double WrappedObject => _value;
        public override void Set(object value) => _value = (double)value;
    }

    [Serializable]
    public class StringWrapper : FieldWrapper<string>
    {
        [JsonProperty, SerializeField] private string _value;

        public override string WrappedObject => _value;
        public override void Set(object value) => _value = value as string;
    }

// Unity Specific Types
    [Serializable]
    public class Vector2Wrapper : FieldWrapper<Vector2>
    {
        [JsonProperty, SerializeField] private Vector2 _value;

        public override Vector2 WrappedObject => _value;
        public override void Set(object value) => _value = (Vector2)value;
    }

    [Serializable]
    public class Vector2IntWrapper : FieldWrapper<Vector2Int>
    {
        [JsonProperty, SerializeField] private Vector2Int _value;

        public override Vector2Int WrappedObject => _value;
        public override void Set(object value) => _value = (Vector2Int)value;
    }

    [Serializable]
    public class Vector3Wrapper : FieldWrapper<Vector3>
    {
        [JsonProperty, SerializeField] private Vector3 _value;

        public override Vector3 WrappedObject => _value;
        public override void Set(object value) => _value = (Vector3)value;
    }

    [Serializable]
    public class Vector3IntWrapper : FieldWrapper<Vector3Int>
    {
        [JsonProperty, SerializeField] private Vector3Int _value;

        public override Vector3Int WrappedObject => _value;
        public override void Set(object value) => _value = (Vector3Int)value;
    }

    [Serializable]
    public class Vector4Wrapper : FieldWrapper<Vector4>
    {
        [JsonProperty, SerializeField] private Vector4 _value;

        public override Vector4 WrappedObject => _value;
        public override void Set(object value) => _value = (Vector4)value;
    }

    [Serializable]
    public class ColorWrapper : FieldWrapper<Color>
    {
        [JsonProperty, SerializeField] private Color _value;

        public override Color WrappedObject => _value;
        public override void Set(object value) => _value = (Color)value;
    }

    [Serializable]
    public class GradientWrapper : FieldWrapper<Gradient>
    {
        [JsonProperty, SerializeField] private Gradient _value;

        public override Gradient WrappedObject => _value;
        public override void Set(object value) => _value = (Gradient)value;
    }

    [Serializable]
    public class RectWrapper : FieldWrapper<Rect>
    {
        [JsonProperty, SerializeField] private Rect _value;

        public override Rect WrappedObject => _value;
        public override void Set(object value) => _value = (Rect)value;
    }

    [Serializable]
    public class RectIntWrapper : FieldWrapper<RectInt>
    {
        [JsonProperty, SerializeField] private RectInt _value;

        public override RectInt WrappedObject => _value;
        public override void Set(object value) => _value = (RectInt)value;
    }

    [Serializable]
    public class BoundsWrapper : FieldWrapper<Bounds>
    {
        [JsonProperty, SerializeField] private Bounds _value;

        public override Bounds WrappedObject => _value;
        public override void Set(object value) => _value = (Bounds)value;
    }

    [Serializable]
    public class BoundsIntWrapper : FieldWrapper<BoundsInt>
    {
        [JsonProperty, SerializeField] private BoundsInt _value;

        public override BoundsInt WrappedObject => _value;
        public override void Set(object value) => _value = (BoundsInt)value;
    }

    [Serializable]
    public class LayerMaskWrapper : FieldWrapper<LayerMask>
    {
        [JsonProperty, SerializeField] private LayerMask _value;

        public override LayerMask WrappedObject => _value;
        public override void Set(object value) => _value = (LayerMask)value;
    }

    [Serializable]
    public class RenderingLayerMaskWrapper : FieldWrapper<RenderingLayerMask>
    {
        [JsonProperty, SerializeField] private RenderingLayerMask _value;

        public override RenderingLayerMask WrappedObject => _value;
        public override void Set(object value) => _value = (RenderingLayerMask)value;
    }

    [Serializable]
    public class AnimationCurveWrapper : FieldWrapper<AnimationCurve>
    {
        [JsonProperty, SerializeField] private AnimationCurve _value;

        public override AnimationCurve WrappedObject => _value;
        public override void Set(object value) => _value = (AnimationCurve)value;
    }

    [Serializable]
    public class Hash128Wrapper : FieldWrapper<Hash128>
    {
        [JsonProperty, SerializeField] private Hash128 _value;

        public override Hash128 WrappedObject => _value;
        public override void Set(object value) => _value = (Hash128)value;
    }

    [Serializable]
    public class EnumWrapper<TEnum> : FieldWrapper where TEnum : Enum
    {
        [JsonProperty, SerializeField] private TEnum _value;

        public TEnum Value
        {
            get => _value;
            set => _value = value;
        }

        public override object Get()
        {
            return _value;
        }

        public override void Set(object value)
        {
            Value = (TEnum)value;
        }

        public override Type GetPinType()
        {
            return typeof(TEnum);
        }
    }

    [Serializable, Blueprintable]
    public class EventKeyWrapper : FieldWrapper<KeyDropdownValue>
    {
        [JsonProperty, SerializeField, ValueDropdown("EventKeys", ValueDropdownAttribute.FilterType.Category), IgnoreCustomDrawer]
        private KeyDropdownValue _key;

        public override KeyDropdownValue WrappedObject => _key;

        public override void Set(object value)
        {
            _key = (KeyDropdownValue)value;
        }
    }

    [Serializable, Blueprintable]
    public class ProviderKeyWrapper : FieldWrapper<KeyDropdownValue>
    {
        [JsonProperty, SerializeField, ValueDropdown("ProviderKeys", ValueDropdownAttribute.FilterType.Category), IgnoreCustomDrawer]
        private KeyDropdownValue _key;

        public override KeyDropdownValue WrappedObject => _key;

        public override void Set(object value)
        {
            _key = (KeyDropdownValue)value;
        }
    }
    
    public static class FieldWrapperFactory
    {
        private static readonly Dictionary<Type, Type> s_WrapperTypeMap = new Dictionary<Type, Type>
        {
            { typeof(bool), typeof(BoolWrapper) },
            { typeof(byte), typeof(ByteWrapper) },
            { typeof(sbyte), typeof(SByteWrapper) },
            { typeof(short), typeof(ShortWrapper) },
            { typeof(ushort), typeof(UShortWrapper) },
            { typeof(int), typeof(IntWrapper) },
            { typeof(uint), typeof(UIntWrapper) },
            { typeof(long), typeof(LongWrapper) },
            { typeof(ulong), typeof(ULongWrapper) },
            { typeof(float), typeof(FloatWrapper) },
            { typeof(double), typeof(DoubleWrapper) },
            { typeof(string), typeof(StringWrapper) },
            { typeof(Vector2), typeof(Vector2Wrapper) },
            { typeof(Vector2Int), typeof(Vector2IntWrapper) },
            { typeof(Vector3), typeof(Vector3Wrapper) },
            { typeof(Vector3Int), typeof(Vector3IntWrapper) },
            { typeof(Vector4), typeof(Vector4Wrapper) },
            { typeof(Color), typeof(ColorWrapper) },
            { typeof(Gradient), typeof(GradientWrapper) },
            { typeof(Rect), typeof(RectWrapper) },
            { typeof(RectInt), typeof(RectIntWrapper) },
            { typeof(Bounds), typeof(BoundsWrapper) },
            { typeof(BoundsInt), typeof(BoundsIntWrapper) },
            { typeof(LayerMask), typeof(LayerMaskWrapper) },
            { typeof(RenderingLayerMask), typeof(RenderingLayerMaskWrapper) },
            { typeof(AnimationCurve), typeof(AnimationCurveWrapper) },
            { typeof(Hash128), typeof(Hash128Wrapper) }
        };

        public static FieldWrapper Create(Type type)
        {
            if (s_WrapperTypeMap.TryGetValue(type, out Type wrapperType))
            {
                return (FieldWrapper)Activator.CreateInstance(wrapperType);
            }

            throw new NotSupportedException($"No wrapper found for type {type}");
        }
    }
}
