using System;

namespace Vapor.GraphTools
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PortOutAttribute : Attribute
    {
        public string Name { get; }
        public int PortIndex { get; }
        public bool Required { get; }
        public bool MultiPort { get; }
        public Type[] PortTypes { get; }

        public PortOutAttribute(string paramName, int portIndex, bool required, params Type[] portTypes)
        {
            Name = paramName;
            PortIndex = portIndex;
            Required = required;
            MultiPort = false;
            PortTypes = portTypes;
        }

        public PortOutAttribute(string paramName, int portIndex, bool required, bool multiPort, params Type[] portTypes)
        {
            Name = paramName;
            PortIndex = portIndex;
            Required = required;
            MultiPort = multiPort;
            PortTypes = portTypes;
        }
    }
}
