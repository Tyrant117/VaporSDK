using System;

namespace Vapor.VisualScripting
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NodeNameAttribute : Attribute
    {
        public string Name { get; }

        public NodeNameAttribute(string name)
        {
            Name = name;
        }
    }
}
