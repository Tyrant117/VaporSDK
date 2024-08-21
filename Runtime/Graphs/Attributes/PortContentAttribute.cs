using System;

namespace Vapor.Graphs
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PortContentAttribute : Attribute
    {
        public int PortInIndex { get; }

        public PortContentAttribute(int portInIndex)
        {
            PortInIndex = portInIndex;
        }
    }
}