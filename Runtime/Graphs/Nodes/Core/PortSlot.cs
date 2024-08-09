using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.Graphs
{
    public class PortSlot
    {
        public string UniqueName { get; }
        public string DisplayName { get; }
        public PortDirection Direction { get; }
        public Type Type { get; }
        public bool AllowMultiple { get; private set; }
        public bool Optional { get; private set; }

        public bool HasContent { get; private set; }
        public Type ContentType { get; private set; }
        public object Content { get; private set; }

        public NodeReference Reference { get; set; }

        public PortSlot(string uniqueName, string displayName, PortDirection direction, Type type)
        {
            UniqueName = uniqueName;
            DisplayName = displayName;
            Direction = direction;
            Type = type;
        }

        public PortSlot CanAllowMultiple()
        {
            AllowMultiple = true;
            return this;
        }

        public PortSlot IsOptional()
        {
            Optional = true;
            return this;
        }

        public PortSlot WithContent(Type contentType, object defaultValue)
        {
            Assert.IsNotNull(contentType, "Content Type Cannot Be Null");
            Optional = true;
            HasContent = true;
            ContentType = contentType;
            Content = defaultValue;
            return this;
        }
    }
}
