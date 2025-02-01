using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Vapor.Blueprints;

namespace VaporEditor.Blueprints
{
    public readonly struct BlueprintSearchEntry
    {
        // Required
        public readonly string CombinedMenuName;
        public readonly string[] MenuName;
        public readonly MethodInfo MethodInfo;
        public readonly BlueprintNodeType NodeType;

        // Optional
        public readonly string[] Synonyms;

        // Type Specific
        public readonly string GetterSetterFieldName;

        // Constructor
        private BlueprintSearchEntry(string combinedMenuName, string[] menuName, MethodInfo methodInfo, string[] synonyms, BlueprintNodeType nodeType, string getterSetterFieldName)
        {
            CombinedMenuName = combinedMenuName;
            MenuName = menuName;
            MethodInfo = methodInfo;
            Synonyms = synonyms;
            NodeType = nodeType;
            GetterSetterFieldName = getterSetterFieldName;
        }

        // Nested Builder Class
        public class Builder
        {
            private List<string> _menuName;
            private MethodInfo _methodInfo;
            private string[] _synonyms;
            private BlueprintNodeType _nodeType;
            private string _getterSetterFieldName;

            // Setters
            public Builder WithNode(MethodInfo methodInfo)
            {
                _methodInfo = methodInfo;
                return this;
            }

            public Builder WithCategoryAndName(string[] menuName, string nodeName)
            {
                _menuName ??= new();
                _menuName.AddRange(menuName);
                _menuName.Add(nodeName);
                return this;
            }

            public Builder WithFullName(string fullName)
            {
                _menuName ??= new();
                _menuName.Add(fullName);
                return this;
            }

            public Builder WithSynonyms(params string[] synonyms)
            {
                _synonyms = synonyms;
                return this;
            }

            public Builder WithNodeType(BlueprintNodeType nodeType)
            {
                _nodeType = nodeType;
                return this;
            }

            public Builder WithGetterSetterFieldName(string getterFieldName)
            {
                _getterSetterFieldName = getterFieldName;
                return this;
            }

            // Build method to create the immutable NodeEntry instance
            public BlueprintSearchEntry Build()
            {
                StringBuilder sb = new();
                for (var i = 0; i < _menuName.Count; i++)
                {
                    var menuName = _menuName[i];
                    sb.Append(menuName);
                    if (i != _menuName.Count - 1)
                    {
                        sb.Append('/');
                    }
                }

                return new BlueprintSearchEntry(sb.ToString(), _menuName.ToArray(), _methodInfo, _synonyms, _nodeType, _getterSetterFieldName);
            }
        }
    }
}