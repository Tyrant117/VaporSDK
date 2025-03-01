using System;

namespace Vapor.VisualScripting
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SearchableNodeAttribute : Attribute
    {
        public string[] MenuName { get; }
        public string NodeName { get; }
        public Type TypeCollection { get; }
        public string[] Synonyms { get; }
        public string[] IncludeFlags { get; }

        public SearchableNodeAttribute(string menuName, params string[] includeFlags)
        {
            MenuName = menuName.Split('/');
            NodeName = MenuName[^1];
            TypeCollection = null;
            IncludeFlags = includeFlags;
        }

        public SearchableNodeAttribute(string menuName, string[] synonyms, string[] includeFlags)
        {
            MenuName = menuName.Split('/');
            NodeName = MenuName[^1];
            TypeCollection = null;
            Synonyms = synonyms;
            IncludeFlags = includeFlags;
        }

        public SearchableNodeAttribute(string menuName, Type typeCollection, params string[] includeFlags)
        {
            MenuName = menuName.Split('/');
            NodeName = string.Empty;
            TypeCollection = typeCollection;
            IncludeFlags = includeFlags;
        }
    }
}
