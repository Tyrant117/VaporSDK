using System;
using UnityEngine;

namespace Vapor.VisualScripting
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class GraphClassAttribute : Attribute
    {
        public GraphAttributeFlags Flags { get; }

        public GraphClassAttribute(GraphAttributeFlags flags = 0)
        {
            
        }
    }
}
