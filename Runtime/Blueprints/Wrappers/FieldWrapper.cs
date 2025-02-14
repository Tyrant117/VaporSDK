using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Vapor.Inspector;
using Vapor.Keys;

namespace Vapor.Blueprints
{
    public static class RuntimeSubclassUtility
    {
        private static List<Type> s_CachedTypes;
        private static HashSet<string> s_EditorAssemblies;
        
        /// Default method: Returns all valid types
        public static IEnumerable<Type> GetCachedTypes()
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
            return GetCachedTypes().Where(t => typeof(T).IsAssignableFrom(t) && !t.IsAbstract);
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
                {
                    continue;
                }

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
            if (s_EditorAssemblies != null)
            {
                return; // Already loaded
            }

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
            {
                return false;
            }
            
            // Support abstract classes and interfaces and generics
            return true;
        }
    }
    
    [Serializable, DrawWithVapor(UIGroupType.Box)]
    public class SubclassOf : FieldWrapper
    {
        [SerializeField, TypeSelector("@TypeResolver"), OnValueChanged("OnTypeChanged", true)] 
        private string _assemblyQualifiedName;
        public string AssemblyQualifiedName
        {
            get => _assemblyQualifiedName;
            set => _assemblyQualifiedName = value;
        }

        [SerializeField, ShowIf("@IsGenericTypeDefinition")]
        private SubclassGeneric[] _genericArguments;
        
        // ReSharper disable once UnusedMember.Global
        public bool IsGenericTypeDefinition => !AssemblyQualifiedName.EmptyOrNull() && Type.GetType(AssemblyQualifiedName) is { IsGenericTypeDefinition: true };
        private void OnTypeChanged(string current)
        {
            if (current.EmptyOrNull())
            {
                return;
            }
            
            var type = Type.GetType(current);
            _genericArguments = type is { IsGenericType: true } 
                ? new SubclassGeneric[type.GetGenericArguments().Length] 
                : Array.Empty<SubclassGeneric>();
        }
        
        // Cached resolved type for performance
        [NonSerialized]
        private Type _cachedType;

        public SubclassOf()
        {
            
        }
        public SubclassOf(Type assignedType)
        {
            SetType(assignedType);
        }
        public SubclassOf(string assemblyQualifiedName)
        {
            if (assemblyQualifiedName.EmptyOrNull())
            {
                AssemblyQualifiedName = null;
                _genericArguments = null;
                return;
            }

            Type type = Type.GetType(assemblyQualifiedName);
        
            if (type == null)
            {
                Debug.LogError($"Invalid type: {assemblyQualifiedName}");
                AssemblyQualifiedName = null;
                _genericArguments = null;
                return;
            }

            SetType(type); // Use the existing method to handle generics
        }
        
        public override object Get() => AssemblyQualifiedName;

        public override void Set(object value)
        {
            if (value == null)
            {
                AssemblyQualifiedName = null;
                _genericArguments = null;
                return;
            }

            if (value is Type type)
            {
                SetType(type);
            }
            else if (value is string assemblyQualifiedName)
            {
                SetType(Type.GetType(assemblyQualifiedName));
            }
            else
            {
                SetType(value.GetType());
            }
        }

        public override Type GetPinType()
        {
            if (_cachedType != null)
            {
                return _cachedType; // Return cached type if already resolved
            }

            _cachedType = ResolveType();
            return _cachedType;
        }
        
        // Resolves the actual type, handling generics correctly
        private Type ResolveType()
        {
            if (AssemblyQualifiedName.EmptyOrNull())
            {
                return null;
            }

            Type baseType = Type.GetType(AssemblyQualifiedName);
            if (baseType == null)
            {
                return null; // Invalid type
            }

            if (!baseType.IsGenericTypeDefinition || _genericArguments == null || _genericArguments.Length == 0)
            {
                return baseType; // Non-generic type, return it directly
            }

            // Build out full generic type using MakeGenericType
            var genericArgs = _genericArguments
                .Select(arg => arg.ResolveType())
                .Where(t => t != null)
                .ToArray();

            Assert.IsTrue(genericArgs.Length == _genericArguments.Length,
                $"Generic Argument Count Mismatch. Expected: {_genericArguments.Length}, Actual: {genericArgs.Length} for Type: {baseType.Name}");
            return baseType.MakeGenericType(genericArgs);
        }

        // Properly assigns type and extracts generic parameters
        private void SetType(Type type)
        {
            if (type == null)
            {
                AssemblyQualifiedName = null;
                _genericArguments = null;
                return;
            }

            AssemblyQualifiedName = type.IsGenericType ? type.GetGenericTypeDefinition().AssemblyQualifiedName : type.AssemblyQualifiedName;

            if (type.IsGenericType)
            {
                _genericArguments = type.GetGenericArguments()
                    .Select(t => new SubclassGeneric(t))
                    .ToArray();
            }
            else
            {
                _genericArguments = null;
            }
            
            _cachedType = type; // Cache the resolved type immediately
        }
        
        protected virtual IEnumerable<Type> TypeResolver()
        {
            return RuntimeSubclassUtility.GetCachedTypes();
        }
    }
    
    [Serializable, DrawWithVapor(UIGroupType.Box)]
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

            s_FilteredTypes = RuntimeSubclassUtility.GetFilteredTypes<T>().ToList();
            return s_FilteredTypes;
        }
    }

    [Serializable, DrawWithVapor(UIGroupType.Box)]
    public class SubclassGeneric
    {
        [SerializeField, TypeSelector("@TypeResolver"), OnValueChanged("OnTypeChanged", true)] 
        private string _assemblyQualifiedName;
        public string AssemblyQualifiedName
        {
            get => _assemblyQualifiedName;
            set => _assemblyQualifiedName = value;
        }
        
        [SerializeField, TypeSelector("@TypeResolverNoGeneric"), ShowIf("@IsGenericTypeDefinition")] 
        private List<string> _genericArguments;
        public List<string> GenericArguments => _genericArguments;
        
        [NonSerialized]
        private Type _cachedType;

        public bool IsGenericTypeDefinition => !AssemblyQualifiedName.EmptyOrNull() && Type.GetType(AssemblyQualifiedName) is { IsGenericTypeDefinition: true };

        private void OnTypeChanged(string current)
        {
            if (current.EmptyOrNull())
            {
                return;
            }

            var type = Type.GetType(current);
            _genericArguments = type is { IsGenericType: true }
                ? new List<string>(type.GetGenericArguments().Length)
                : new List<string>();
        }

        public SubclassGeneric()
        {
            
        }
        public SubclassGeneric(Type type)
        {
            SetType(type);
        }
        public SubclassGeneric(string assemblyQualifiedName)
        {
            if (assemblyQualifiedName.EmptyOrNull())
            {
                AssemblyQualifiedName = null;
                _genericArguments = null;
                return;
            }

            Type type = Type.GetType(assemblyQualifiedName);
        
            if (type == null)
            {
                Debug.LogError($"Invalid type: {assemblyQualifiedName}");
                AssemblyQualifiedName = null;
                _genericArguments = null;
                return;
            }

            SetType(type); // Use the existing method to handle generics
        }

        public Type ResolveType()
        {
            if (AssemblyQualifiedName.EmptyOrNull())
            {
                return null;
            }

            Type baseType = Type.GetType(AssemblyQualifiedName);
            if (baseType == null)
            {
                return null; // Invalid type
            }

            if (!baseType.IsGenericTypeDefinition || _genericArguments == null || _genericArguments.Count == 0)
            {
                return baseType; // Non-generic type, return it directly
            }

            // Build out full generic type using MakeGenericType
            var genericArgs = _genericArguments
                .Select(Type.GetType)
                .Where(t => t != null)
                .ToArray();

            Assert.IsTrue(genericArgs.Length == _genericArguments.Count,
                $"Generic Argument Count Mismatch. Expected: {_genericArguments.Count}, Actual: {genericArgs.Length} for Type: {baseType.Name}");
            return baseType.MakeGenericType(genericArgs);
        }
        
        public void SetType(Type type)
        {
            if (type == null)
            {
                AssemblyQualifiedName = null;
                _genericArguments = null;
                return;
            }

            AssemblyQualifiedName = type.IsGenericType ? type.GetGenericTypeDefinition().AssemblyQualifiedName : type.AssemblyQualifiedName;

            if (type.IsGenericType)
            {
                _genericArguments = type.GetGenericArguments()
                    .Select(t => t.AssemblyQualifiedName)
                    .ToList();
            }
            else
            {
                _genericArguments = null;
            }
            
            _cachedType = type; // Cache the resolved type immediately
        }
        
        private IEnumerable<Type> TypeResolver()
        {
            return RuntimeSubclassUtility.GetCachedTypes();
        }
        
        private IEnumerable<Type> TypeResolverNoGeneric()
        {
            return RuntimeSubclassUtility.GetCachedTypes().Where(t => !t.IsGenericType).ToArray();
        }
    }

    public abstract class FieldWrapper
    {
        public abstract object Get();
        public abstract void Set(object value);
        public abstract Type GetPinType();
    }

    [Serializable]
    public class FieldWrapper<T> : FieldWrapper
    {
        public virtual T WrappedObject { get; protected set; }

        public override void Set(object value) => WrappedObject = (T)value;
        public override object Get() => WrappedObject;
        public override Type GetPinType() => typeof(T);
        public override string ToString() => WrappedObject.ToString();
    }

    [Serializable]
    public class GenericWrapper<T> : FieldWrapper<T>
    {
        [JsonProperty, SerializeField] private T _value;

        public override T WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (T)value;
    }

    [Serializable]
    public class BoolWrapper : FieldWrapper<bool>
    {
        [JsonProperty, SerializeField] private bool _value;

        public override bool WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (bool)value;
    }

    [Serializable]
    public class ByteWrapper : FieldWrapper<byte>
    {
        [JsonProperty, SerializeField] private byte _value;

        public override byte WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (byte)value;
    }

    [Serializable]
    public class SByteWrapper : FieldWrapper<sbyte>
    {
        [JsonProperty, SerializeField] private sbyte _value;

        public override sbyte WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (sbyte)value;
    }

    [Serializable]
    public class ShortWrapper : FieldWrapper<short>
    {
        [JsonProperty, SerializeField] private short _value;

        public override short WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (short)value;
    }

    [Serializable]
    public class UShortWrapper : FieldWrapper<ushort>
    {
        [JsonProperty, SerializeField] private ushort _value;

        public override ushort WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (ushort)value;
    }

    [Serializable]
    public class IntWrapper : FieldWrapper<int>
    {
        [JsonProperty, SerializeField] private int _value;

        public override int WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (int)value;
    }

    [Serializable]
    public class UIntWrapper : FieldWrapper<uint>
    {
        [JsonProperty, SerializeField] private uint _value;

        public override uint WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (uint)value;
    }

    [Serializable]
    public class LongWrapper : FieldWrapper<long>
    {
        [JsonProperty, SerializeField] private long _value;

        public override long WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (long)value;
    }

    [Serializable]
    public class ULongWrapper : FieldWrapper<ulong>
    {
        [JsonProperty, SerializeField] private ulong _value;

        public override ulong WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (ulong)value;
    }

    [Serializable]
    public class FloatWrapper : FieldWrapper<float>
    {
        [JsonProperty, SerializeField] private float _value;

        public override float WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (float)value;
    }

    [Serializable]
    public class DoubleWrapper : FieldWrapper<double>
    {
        [JsonProperty, SerializeField] private double _value;

        public override double WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (double)value;
    }

    [Serializable]
    public class StringWrapper : FieldWrapper<string>
    {
        [JsonProperty, SerializeField] private string _value;

        public override string WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = value as string;
    }

// Unity Specific Types
    [Serializable]
    public class Vector2Wrapper : FieldWrapper<Vector2>
    {
        [JsonProperty, SerializeField] private Vector2 _value;

        public override Vector2 WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (Vector2)value;
    }

    [Serializable]
    public class Vector2IntWrapper : FieldWrapper<Vector2Int>
    {
        [JsonProperty, SerializeField] private Vector2Int _value;

        public override Vector2Int WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (Vector2Int)value;
    }

    [Serializable]
    public class Vector3Wrapper : FieldWrapper<Vector3>
    {
        [JsonProperty, SerializeField] private Vector3 _value;

        public override Vector3 WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (Vector3)value;
    }

    [Serializable]
    public class Vector3IntWrapper : FieldWrapper<Vector3Int>
    {
        [JsonProperty, SerializeField] private Vector3Int _value;

        public override Vector3Int WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (Vector3Int)value;
    }

    [Serializable]
    public class Vector4Wrapper : FieldWrapper<Vector4>
    {
        [JsonProperty, SerializeField] private Vector4 _value;

        public override Vector4 WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (Vector4)value;
    }

    [Serializable]
    public class ColorWrapper : FieldWrapper<Color>
    {
        [JsonProperty, SerializeField] private Color _value;

        public override Color WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (Color)value;
    }

    [Serializable]
    public class GradientWrapper : FieldWrapper<Gradient>
    {
        [JsonProperty, SerializeField] private Gradient _value;

        public override Gradient WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (Gradient)value;
    }

    [Serializable]
    public class RectWrapper : FieldWrapper<Rect>
    {
        [JsonProperty, SerializeField] private Rect _value;

        public override Rect WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (Rect)value;
    }

    [Serializable]
    public class RectIntWrapper : FieldWrapper<RectInt>
    {
        [JsonProperty, SerializeField] private RectInt _value;

        public override RectInt WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (RectInt)value;
    }

    [Serializable]
    public class BoundsWrapper : FieldWrapper<Bounds>
    {
        [JsonProperty, SerializeField] private Bounds _value;

        public override Bounds WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (Bounds)value;
    }

    [Serializable]
    public class BoundsIntWrapper : FieldWrapper<BoundsInt>
    {
        [JsonProperty, SerializeField] private BoundsInt _value;

        public override BoundsInt WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (BoundsInt)value;
    }

    [Serializable]
    public class LayerMaskWrapper : FieldWrapper<LayerMask>
    {
        [JsonProperty, SerializeField] private LayerMask _value;

        public override LayerMask WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (LayerMask)value;
    }

    [Serializable]
    public class RenderingLayerMaskWrapper : FieldWrapper<RenderingLayerMask>
    {
        [JsonProperty, SerializeField] private RenderingLayerMask _value;

        public override RenderingLayerMask WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (RenderingLayerMask)value;
    }

    [Serializable]
    public class AnimationCurveWrapper : FieldWrapper<AnimationCurve>
    {
        [JsonProperty, SerializeField] private AnimationCurve _value;

        public override AnimationCurve WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

        public override void Set(object value) => _value = (AnimationCurve)value;
    }

    [Serializable]
    public class Hash128Wrapper : FieldWrapper<Hash128>
    {
        [JsonProperty, SerializeField] private Hash128 _value;

        public override Hash128 WrappedObject
        {
            get => _value;
            protected set => _value = value;
        }

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

        public override KeyDropdownValue WrappedObject
        {
            get => _key;
            protected set => _key = value;
        }

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

        public override KeyDropdownValue WrappedObject
        {
            get => _key;
            protected set => _key = value;
        }

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
