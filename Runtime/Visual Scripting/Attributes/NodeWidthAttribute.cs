using System;

namespace Vapor.VisualScripting
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
