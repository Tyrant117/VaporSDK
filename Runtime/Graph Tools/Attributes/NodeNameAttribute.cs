using System;

namespace Vapor.GraphTools
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
