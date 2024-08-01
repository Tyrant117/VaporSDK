using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Graphs;
using Vapor.Keys;
using Node = Vapor.Graphs.Node;
using Object = UnityEngine.Object;

namespace VaporEditor.Graphs
{
    public struct SearchContextElement
    {
        public Type NodeType { get; private set; }
        public string MenuName { get; private set; }
        public object UserData { get; private set; }


        public SearchContextElement(Type nodeType, string title, object userData = null)
        {
            NodeType = nodeType;
            MenuName = title;
            UserData = userData;
        }
    }

    public class GraphSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        public static List<SearchContextElement> Elements = new();

        public GraphEditorView View;
        public List<string> IncludeFlags = new();

        public void SetupIncludes(List<string> includes)
        {
            IncludeFlags.AddRange(includes);
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            Elements.Clear();

            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Nodes"), 0)
            };

            var types = TypeCache.GetTypesWithAttribute<SearchableNodeAttribute>();
            foreach (var type in types)
            {
                var atr = type.GetCustomAttribute<SearchableNodeAttribute>();
                FilterNewNode(type, atr);
            }

            SortElements();

            var groups = new List<string>();
            foreach (var e in Elements)
            {
                var split = e.MenuName.Split('/');
                var groupName = new StringBuilder();
                for (int i = 0; i < split.Length - 1; i++)
                {
                    groupName.Append(split[i]);
                    if (!groups.Contains(groupName.ToString()))
                    {
                        tree.Add(new SearchTreeGroupEntry(new GUIContent(split[i]), i + 1));
                        groups.Add(groupName.ToString());
                    }
                    groupName.Append("/");
                }

                var entry = new SearchTreeEntry(new GUIContent(split[^1]))
                {
                    level = split.Length,
                    userData = e,
                };
                tree.Add(entry);
            }

            return tree;
        }

        /// <summary>
        /// Filters a newly created node. This method should add be used to add new <see cref="SearchContextElement"/> to <see cref="Elements"/>
        /// </summary>
        /// <param name="node"></param>
        private void FilterNewNode(Type nodeType, SearchableNodeAttribute attribute)
        {
            if (string.IsNullOrEmpty(attribute.MenuName)) { return; }
            if (nodeType.IsSubclassOf(typeof(Node)))
            {
                if (attribute.TypeCollection != null)
                {
                    if (attribute.IncludeFlags.Length != 0)
                    {
                        if (IncludeFlags.Intersect(attribute.IncludeFlags).Count() > 0)
                        {
                            var collection = AssetDatabaseUtility.FindAssetsByType(attribute.TypeCollection);
                            foreach (var item in collection)
                            {
                                ValueTuple<string, Type> containerTarget = new(AssetDatabase.GetAssetPath(item), item.GetType());
                                Elements.Add(new SearchContextElement(nodeType, $"{attribute.MenuName}/{item.name}", containerTarget));
                            }
                        }
                    }
                    else
                    {
                        var collection = AssetDatabaseUtility.FindAssetsByType(attribute.TypeCollection);
                        foreach (var item in collection)
                        {
                            ValueTuple<string, Type> containerTarget = new(AssetDatabase.GetAssetPath(item), item.GetType());
                            Elements.Add(new SearchContextElement(nodeType, $"{attribute.MenuName}/{item.name}", containerTarget));
                        }
                    }
                }
                else
                {
                    if (attribute.IncludeFlags.Length != 0)
                    {
                        if (IncludeFlags.Intersect(attribute.IncludeFlags).Count() > 0)
                        {
                            Elements.Add(new SearchContextElement(nodeType, attribute.MenuName));
                        }
                    }
                    else
                    {
                        Elements.Add(new SearchContextElement(nodeType, attribute.MenuName));
                    }
                }
            }
        }

        private void SortElements()
        {
            // Sort
            Elements.Sort((e1, e2) =>
            {
                var s1 = e1.MenuName.Split('/');
                var s2 = e2.MenuName.Split('/');
                for (int i = 0; i < s1.Length; i++)
                {
                    if (i >= s2.Length)
                    {
                        return 1;
                    }
                    else
                    {
                        var value = s1[i].CompareTo(s2[i]);
                        if (value != 0)
                        {
                            if (s1.Length != s2.Length && (i == s1.Length - 1 || i == s2.Length - 1))
                            {
                                return s1.Length < s2.Length ? 1 : -1;
                            }
                            else
                            {
                                return value;
                            }
                        }
                    }
                }

                return 0;
            });
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            var mousePos = View.ChangeCoordinatesTo(View, context.screenMousePosition - View.Window.position.position);
            var graphMousePos = View.contentViewContainer.WorldToLocal(mousePos);

            var element = (SearchContextElement)SearchTreeEntry.userData;

            if (element.NodeType.IsSubclassOf(typeof(Node)))
            {
                var n = Activator.CreateInstance(element.NodeType);
                if (n is Node node)
                {
                    node.Position = new Rect(graphMousePos, Vector2.zero);
                    node.Guid = Node.CreateGuid();
                    if (element.UserData != null)
                    {
                        if (element.UserData is ValueTuple<string, Type> assetContainer)
                        {
                            var asset = AssetDatabase.LoadAssetAtPath(assetContainer.Item1, assetContainer.Item2);
                            var objectNode = (UnityObjectValueNode<Object>)node;
                            objectNode.Value = asset;
                            objectNode.Name = asset.name;
                        }
                    }
                    else
                    {
                        var searchableAtr = element.NodeType.GetCustomAttribute<SearchableNodeAttribute>();
                        node.Name = searchableAtr != null ? searchableAtr.NodeName : element.NodeType.Name;
                    }
                    View.AddNode(node);
                }
            }
            return true;
        }
    }
}
