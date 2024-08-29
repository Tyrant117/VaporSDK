using Newtonsoft.Json;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.VisualScripting
{
    public class PortSlot
    {
        public readonly string UniqueName;
        public readonly string DisplayName;
        public readonly PortDirection Direction;
        public readonly Type Type;

        public bool AllowMultiple;
        public bool IsOptional;

        public bool HasContent;
        public Type ContentType;
        public object Content;
        [JsonIgnore]
        private FieldInfo _contentFieldInfo;

        public NodeReference Reference;


        public PortSlot(string uniqueName, string displayName, PortDirection direction, Type type)
        {
            UniqueName = uniqueName;
            DisplayName = displayName;
            Direction = direction;
            Type = type;
        }

        public PortSlot SetAllowMultiple()
        {
            AllowMultiple = true;
            return this;
        }

        public PortSlot SetIsOptional()
        {
            IsOptional = true;
            return this;
        }

        public PortSlot WithContent<T>(T defaultValue)
        {
            IsOptional = true;
            HasContent = true;
            ContentType = typeof(T);
            Content = defaultValue;
            return this;
        }

        //public PortSlot WithContent(Type contentType, object defaultValue)
        //{
        //    Assert.IsNotNull(contentType, "Content Type Cannot Be Null");
        //    IsOptional = true;
        //    HasContent = true;
        //    ContentType = contentType;
        //    Content = defaultValue;
        //    return this;
        //}

        public FieldInfo GetContentFieldInfo()
        {
            if (_contentFieldInfo != null)
            {
                return _contentFieldInfo;
            }

            _contentFieldInfo = GetType().GetField("Content", BindingFlags.Public | BindingFlags.Instance);
            //Debug.Log(fi);
            //Debug.Log(fi.GetValue(this));
            return _contentFieldInfo;
        }
    }
}
