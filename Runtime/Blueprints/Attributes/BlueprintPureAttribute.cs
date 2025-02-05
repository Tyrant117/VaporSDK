using System;

namespace Vapor.Blueprints
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BlueprintPureAttribute : Attribute
    {
        public string[] MenuName { get; }
        public string NodeName { get; }
        public string[] Synonyms { get; }
        
        public BlueprintPureAttribute(string nodeName = "", string category = "", string[] synonyms = null)
        {
            MenuName = category.Split('/');
            NodeName = nodeName;
            Synonyms = synonyms;
        }
    }
}