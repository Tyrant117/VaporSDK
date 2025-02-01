using System;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BlueprintCallableAttribute : Attribute
    {
        public string[] MenuName { get; }
        public string NodeName { get; }
        public string[] Synonyms { get; }
        
        public BlueprintCallableAttribute(string nodeName = "", string category = "", string[] synonyms = null)
        {
            MenuName = category.Split('/');
            NodeName = nodeName;
            Synonyms = synonyms;
        }
    }
}