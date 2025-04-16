using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using VaporEditor;
using VaporEditor.Blueprints;

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
            
            // Custom Type Drawers
            typeof(Type),
        };

        public NodeModelBase Node { get; }
        public string PortName { get; private set; }
        public string DisplayName { get; private set; }
        public PinDirection Direction { get; }
        public bool IsExecutePin { get; private set; }
        public bool IsOptional { get; private set; }
        public bool AllowMultipleWires { get; private set; }
        public bool IsWildcard { get; private set; }
        public Type[] WildcardTypes { get; private set; }
        public bool IsGenericPin { get; private set; }
        public Type GenericPinType { get; set; }
        
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

        private bool _isArray;
        public bool IsArray
        {
            get => _isArray;
            set => _isArray = value;
        }
        
        public List<BlueprintWire> Wires { get; private set; } = new();

        // Inline Drawers
        public bool HasInlineValue { get; private set; }
        public FieldWrapper InlineValue { get; private set; }
        private readonly bool _blockInlineContent;
        private bool _hasCustomDisplayName;
        
        public BlueprintPin(NodeModelBase node, string portName, PinDirection direction, Type type, bool blockInlineContent)
        {
            Node = node;
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

            if (_type.IsGenericTypeDefinition || _type.IsGenericParameter || type == typeof(GenericPin))
            {
                IsGenericPin = true;
                _type = typeof(GenericPin);
            }

            _blockInlineContent = blockInlineContent;

            UpdateInlineValue(type);
        }

        private void UpdateInlineValue(Type type)
        {
            HasInlineValue = CheckForInlineValue(type);
            if (!HasInlineValue)
            {
                return;
            }
            
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

            if (Direction == PinDirection.Out)
            {
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
                var wrapperType = typeof(FieldWrapper<>).MakeGenericType(type);
                InlineValue = (FieldWrapper)Activator.CreateInstance(wrapperType);
            }
        }

        public BlueprintPin WithDisplayName(string displayName)
        {
            _hasCustomDisplayName = true;
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
            if (value == null)
            {
                if(InlineValue.GetResolvedType().IsClass)
                {
                    InlineValue.Set(null);
                }
                else
                {
                    Debug.LogError($"Value type [{InlineValue.GetResolvedType()}] is not nullable and object is null.");
                }

                return;
            }
            
            if (value.GetType().IsSubclassOf(typeof(FieldWrapper)))
            {
                InlineValue = (FieldWrapper)value;
            }
            else if(InlineValue.GetResolvedType() == value.GetType())
            {
                InlineValue.Set(value);
            }
            else
            {
                Debug.LogError($"Value type [{value.GetType()}] is not the content type [{InlineValue.GetType()}] or the pin type [{InlineValue.GetResolvedType()}]");
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

            return IsGenericPin 
                ? Type == typeof(GenericPin) 
                    ? BlueprintEditorUtility.FormatTypeName(GenericPinType) 
                    : BlueprintEditorUtility.FormatTypeName(Type) 
                : BlueprintEditorUtility.FormatTypeName(Type);
        }

        public void RenamePort(string newName)
        {
            PortName = newName;
            if (!_hasCustomDisplayName)
            {
                DisplayName = newName;
            }
        }

        public bool TryGetWire(out BlueprintWire wire)
        {
            if (Wires.IsValidIndex(0))
            {
                wire = Wires[0];
                return true;
            }
            wire = null;
            return false;
        }

        public void Connect(BlueprintWire blueprintWire)
        {
            Wires.Add(blueprintWire);
        }
    }
}