using System;
using UnityEngine;

namespace Vapor.VisualScripting
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PortAttribute : Attribute
    {
        public string Name { get; }
        public PortDirection Direction { get; }
        public Type Type { get; }
        public bool AllowMultiple { get; }
        public bool Optional { get; }

        public PortAttribute(string paramName, PortDirection direction, Type type, bool allowMultiple = false, bool optional = false)
        {
            Name = paramName;
            Direction = direction;
            Type = type;
            AllowMultiple = allowMultiple;
            Optional = optional;
        }
    }
}
