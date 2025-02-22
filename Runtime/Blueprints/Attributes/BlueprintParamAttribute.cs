using System;

namespace Vapor.Blueprints
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class BlueprintParamAttribute : Attribute
    {
        public string Name { get; }
        public Type[] WildcardTypes { get; }

        public BlueprintParamAttribute(string name = null, Type[] wildcardTypes = null)
        {
            Name = name;
            WildcardTypes = wildcardTypes;
        }
    }
}