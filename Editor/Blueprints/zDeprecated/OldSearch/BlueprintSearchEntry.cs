using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace VaporEditor.Blueprints
{
    [Obsolete]
    public readonly struct BlueprintSearchEntry
    {
        // Required
        public readonly string CombinedMenuName;
        public readonly string[] MenuName;
        // public readonly BlueprintNodeType NodeType;

        // Optional
        public readonly string[] Synonyms;

        // Type Specific
        public readonly MethodInfo MethodInfo;
        public readonly FieldInfo FieldInfo;
        public readonly string[] NameData;
        public readonly Type[] TypeData;

        // Constructor
        private BlueprintSearchEntry(string combinedMenuName, string[] menuName, MethodInfo methodInfo, FieldInfo fieldInfo, string[] synonyms, /*BlueprintNodeType nodeType,*/ string[] nameData,
            Type[] typeData)
        {
            CombinedMenuName = combinedMenuName;
            MenuName = menuName;
            MethodInfo = methodInfo;
            FieldInfo = fieldInfo;
            Synonyms = synonyms;
            // NodeType = nodeType;
            NameData = nameData;
            TypeData = typeData;
        }

        // Nested Builder Class
        public class Builder
        {
            private List<string> _menuName;
            private MethodInfo _methodInfo;
            private FieldInfo _fieldInfo;
            private List<string> _synonyms;
            // private BlueprintNodeType _nodeType;
            private List<Type> _typeData;
            private List<string> _nameData;

            // Setters
            public Builder WithMethodInfo(MethodInfo methodInfo)
            {
                _methodInfo = methodInfo;
                return this;
            }
            
            public Builder WithFieldInfo(FieldInfo fieldInfo)
            {
                _fieldInfo = fieldInfo;
                return this;
            }

            public Builder WithCategoryAndName(string[] menuName, string nodeName)
            {
                _menuName ??= new List<string>();
                _menuName.AddRange(menuName);
                _menuName.Add(nodeName);
                return this;
            }

            public Builder WithFullName(string fullName)
            {
                _menuName ??= new List<string>();
                _menuName.Add(fullName);
                return this;
            }

            public Builder WithSynonyms(params string[] synonyms)
            {
                _synonyms ??= new List<string>();
                if(synonyms is { Length: > 0 })
                {
                    _synonyms.AddRange(synonyms);
                }
                return this;
            }

            // public Builder WithNodeType(BlueprintNodeType nodeType)
            // {
            //     _nodeType = nodeType;
            //     return this;
            // }

            public Builder WithNameData(params string[] names)
            {
                _nameData ??= new List<string>();
                _nameData.AddRange(names);
                return this;
            }

            public Builder WithTypes(params Type[] types)
            {
                _typeData ??= new List<Type>();
                _typeData.AddRange(types);
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
                

                return new BlueprintSearchEntry(sb.ToString(), _menuName?.ToArray(), _methodInfo, _fieldInfo, _synonyms?.ToArray(), /*_nodeType,*/ _nameData?.ToArray(), _typeData?.ToArray());
            }
        }
    }
}