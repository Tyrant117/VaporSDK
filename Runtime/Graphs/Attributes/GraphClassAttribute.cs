using System;
using UnityEngine;

namespace Vapor.Graphs
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
