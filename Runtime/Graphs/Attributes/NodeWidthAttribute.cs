using System;

namespace Vapor.Graphs
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NodeWidthAttribute : Attribute
    {
        public float MinWidth { get; }

        public NodeWidthAttribute(float minWidth)
        {
            MinWidth = minWidth;
        }
    }
}
