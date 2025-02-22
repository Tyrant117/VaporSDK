using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.Blueprints
{
   
    public class BlueprintPin
    {
        private static readonly HashSet<Type> s_ValidTypes = new()
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
        
        public string PortName { get; }
        public string DisplayName { get; private set; }
        public PinDirection Direction { get; }
        public bool IsExecutePin { get; private set; }
        public bool IsOptional { get; private set; }
        public bool AllowMultipleWires { get; private set; }
        public bool IsWildcard { get; private set; }
        public Type[] WildcardTypes { get; private set; }
        
        // Type
        private Type _type;
        public Type Type
        {
            get => _type;
            set
            {
                if (_type == value)
                {
                    return;
                }

                _type = value;
                UpdateInlineValue(_type);
            }
        }

        // Inline Drawers
        public bool HasInlineValue { get; private set; }
        public FieldWrapper InlineValue { get; private set; }
        private readonly bool _blockInlineContent;
        
        public BlueprintPin(string portName, PinDirection direction, Type type, bool blockInlineContent)
        {
            PortName = portName;
            DisplayName = portName;
            Direction = direction;

            if (typeof(FieldWrapper).IsAssignableFrom(type))
            {
                _type = GetFirstGenericArgumentOfFieldWrapper(type) ?? type;
            }
            else
            {
                _type = type;
            }

            _blockInlineContent = blockInlineContent;

            UpdateInlineValue(type);
        }

        private void UpdateInlineValue(Type type)
        {
            if (!CheckForInlineValue(type))
            {
                return;
            }
            
            HasInlineValue = true;
            InitializeInlineValue(type);
        }

        private bool CheckForInlineValue(Type type)
        {
            if (_blockInlineContent)
            {
                return false;
            }
            
            if (type == typeof(ExecutePin))
            {
                IsExecutePin = true;
                return false;
            }

            if (!type.IsEnum && !s_ValidTypes.Contains(type) && !type.IsSubclassOf(typeof(FieldWrapper)))
            {
                return false;
            }

            return true;
        }
        
        private void InitializeInlineValue(Type type)
        {
            if (type.IsEnum)
            {
                Type wrapperType = typeof(EnumWrapper<>).MakeGenericType(type);
                InlineValue = (FieldWrapper)Activator.CreateInstance(wrapperType);
            }
            else if (typeof(SubclassOf<>).IsAssignableFrom(type))
            {
                Type wrapperType = typeof(SubclassOf<>).MakeGenericType(type.GetGenericArguments()[0]);
                InlineValue = (FieldWrapper)Activator.CreateInstance(wrapperType);
            }
            else if (type.IsSubclassOf(typeof(FieldWrapper)))
            {
                InlineValue = (FieldWrapper)Activator.CreateInstance(type);
            }
            else
            {
                InlineValue = FieldWrapperFactory.Create(type);
            }
        }

        public BlueprintPin WithDisplayName(string displayName)
        {
            DisplayName = displayName;
            return this;
        }
        
        public BlueprintPin WithIsOptional()
        {
            IsOptional = true;
            return this;
        }

        public BlueprintPin WithAllowMultipleWires()
        {
            AllowMultipleWires = true;
            return this;
        }

        public BlueprintPin WithWildcardTypes(Type[] wildcardTypes)
        {
            WildcardTypes = wildcardTypes;
            IsWildcard = true;
            return this;
        }
        
        public void SetDefaultValue(object value)
        {
            if (value.GetType().IsSubclassOf(typeof(FieldWrapper)))
            {
                InlineValue = (FieldWrapper)value;
            }
            else if(InlineValue.GetPinType() == value.GetType())
            {
                InlineValue.Set(value);
            }
            else
            {
                Debug.LogError($"Value type [{value.GetType()}] is not the content type [{InlineValue.GetType()}] or the pin type [{InlineValue.GetPinType()}]");
            }
        }

        public object GetContent()
        {
            Assert.IsTrue(HasInlineValue, $"No content available for {PortName}.");
            return InlineValue.Get();
        }
        
        private static Type GetFirstGenericArgumentOfFieldWrapper(Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericArguments().Length > 0)
                {
                    return type.GetGenericArguments()[0];
                }
                type = type.BaseType;
            }
            return null; // Or handle appropriately if no generic argument is found
        }
        
        private static bool HasValidBaseType(Type type)
        {
            while (type is { BaseType: not null })
            {
                type = type.BaseType;

                // Check if the base type (or its generic definition) is in s_ValidTypes
                if (s_ValidTypes.Contains(type) || 
                    (type.IsGenericType && s_ValidTypes.Contains(type.GetGenericTypeDefinition())))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasValidInterfaceType(Type type)
        {
            // Check implemented interfaces, including generic interfaces
            foreach (var itf in type.GetInterfaces())
            {
                if (s_ValidTypes.Contains(itf) ||
                    (itf.IsGenericType && s_ValidTypes.Contains(itf.GetGenericTypeDefinition())))
                {
                    return true;
                }
            }
            return false;
        }
        
        public string CreateTooltipForPin()
        {
            if (IsExecutePin)
            {
                return "Execute";
            }

            return Type.IsGenericType ? $"{Type.Name.Split('`')[0]}<{string.Join(",", Type.GetGenericArguments().Select(a => a.Name))}>" : Type.Name;
        }
    }
}