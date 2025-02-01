using System;

namespace Vapor.Blueprints
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class BlueprintParamAttribute : Attribute
    {
        public string Name { get; }

        public BlueprintParamAttribute(string name = null)
        {
            Name = name;
        }
    }
}