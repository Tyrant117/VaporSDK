using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.VisualScripting;
using Vapor.Keys;
using NodeModel = Vapor.VisualScripting.NodeModel;
using Object = UnityEngine.Object;

namespace VaporEditor.VisualScripting
{
    public struct SearchContextElement
    {
        public Type NodeType { get; private set; }
        public string[] MenuName { get; private set; }
        public object UserData { get; private set; }


        public SearchContextElement(Type nodeType, string[] title, object userData = null)
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
                var groupName = new StringBuilder();
                for (int i = 0; i < e.MenuName.Length - 1; i++)
                {
                    groupName.Append(e.MenuName[i]);
                    if (!groups.Contains(groupName.ToString()))
                    {
                        tree.Add(new SearchTreeGroupEntry(new GUIContent(e.MenuName[i]), i + 1));
                        groups.Add(groupName.ToString());
                    }
                    groupName.Append("/");
                }

                var entry = new SearchTreeEntry(new GUIContent(e.MenuName[^1]))
                {
                    level = e.MenuName.Length,
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
            if (attribute.MenuName.Length == 0) { return; }
            if (nodeType.IsSubclassOf(typeof(NodeModel)))
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

                                string[] menu = new string[attribute.MenuName.Length + 1];
                                Array.Copy(attribute.MenuName, menu, attribute.MenuName.Length);
                                menu[^1] = item.name;
                                Elements.Add(new SearchContextElement(nodeType, menu, containerTarget));
                            }
                        }
                    }
                    else
                    {
                        var collection = AssetDatabaseUtility.FindAssetsByType(attribute.TypeCollection);
                        foreach (var item in collection)
                        {
                            ValueTuple<string, Type> containerTarget = new(AssetDatabase.GetAssetPath(item), item.GetType());

                            string[] menu = new string[attribute.MenuName.Length + 1];
                            Array.Copy(attribute.MenuName, menu, attribute.MenuName.Length);
                            menu[^1] = item.name;
                            Elements.Add(new SearchContextElement(nodeType, menu, containerTarget));
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
            Elements.Sort((entry1, entry2) =>
            {
                for (var i = 0; i < entry1.MenuName.Length; i++)
                {
                    if (i >= entry2.MenuName.Length)
                        return 1;
                    var value = entry1.MenuName[i].CompareTo(entry2.MenuName[i]);
                    if (value != 0)
                    {
                        // Make sure that leaves go before nodes
                        if (entry1.MenuName.Length != entry2.MenuName.Length && (i == entry1.MenuName.Length - 1 || i == entry2.MenuName.Length - 1))
                        {
                            //once nodes are sorted, sort slot entries by slot order instead of alphebetically
                            var alphaOrder = entry1.MenuName.Length < entry2.MenuName.Length ? -1 : 1;
                            return alphaOrder.CompareTo(0);
                        }

                        return value;
                    }
                }
                return 0;
            });
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            var mousePos = View.ChangeCoordinatesTo(View, context.screenMousePosition - View.Window.position.position);
            var graphMousePos = View.GraphView.contentViewContainer.WorldToLocal(mousePos);

            var element = (SearchContextElement)SearchTreeEntry.userData;

            if (element.NodeType.IsSubclassOf(typeof(NodeModel)))
            {
                var n = Activator.CreateInstance(element.NodeType);
                if (n is NodeModel node)
                {
                    node.Position = new Rect(graphMousePos, Vector2.zero);
                    node.Guid = NodeModel.CreateGuid();
                    node.NodeType = node.GetType().AssemblyQualifiedName;
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
