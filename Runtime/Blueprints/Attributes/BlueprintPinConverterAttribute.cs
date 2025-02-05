using System;

namespace Vapor.Blueprints
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BlueprintPinConverterAttribute : Attribute
    {
        public Type SourceType { get; }
        public Type TargetType { get; }

        public BlueprintPinConverterAttribute(Type sourceType, Type targetType)
        {
            SourceType = sourceType;
            TargetType = targetType;
        }
    }
}