using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.VisualScripting;
using Vapor;
using Object = UnityEngine.Object;
using System.Linq;

namespace VaporEditor.VisualScripting
{
    #region Types
    public readonly struct NodeEntry
    {
        // Required
        public readonly string[] MenuName;
        public readonly Type NodeType;

        // Optional
        public readonly string[] Synonyms;

        // Type Collection
        public readonly string AssetPath;
        public readonly Type AssetType;

        // Context Sensitive
        public readonly int CompatibleSlotId;
        public readonly string SlotName;


        // Constructor
        private NodeEntry(string[] menuName, Type nodeType, string[] synonyms, string assetPath, Type assetType, int compatibleSlotId, string slotName)
        {
            MenuName = menuName;
            NodeType = nodeType;
            Synonyms = synonyms;
            AssetPath = assetPath;
            AssetType = assetType;
            CompatibleSlotId = compatibleSlotId;
            SlotName = slotName;
        }

        // Nested Builder Class
        public class Builder
        {
            private List<string> _menuName;
            private Type _nodeType;
            private string[] _synonyms;
            private string _assetPath;
            private Type _assetType;
            private int _compatibleSlotId = -1;
            private string _slotName;

            // Setters
            public Builder WithNode(Type nodeType, string[] menuName)
            {
                _menuName ??= new();
                _menuName.AddRange(menuName);
                _nodeType = nodeType;
                return this;
            }

            public Builder WithName(string nodeName)
            {
                _menuName ??= new();
                _menuName.Add(nodeName);
                return this;
            }

            public Builder WithSynonyms(params string[] synonyms)
            {
                _synonyms = synonyms;
                return this;
            }

            public Builder WithAsset(string assetPath, Type assetType)
            {
                _assetPath = assetPath;
                _assetType = assetType;
                return this;
            }

            public Builder WithContextSensitiveSlot(int compatibleSlotId, string slotName)
            {
                _compatibleSlotId = compatibleSlotId;
                _slotName = slotName;
                return this;
            }

            // Build method to create the immutable NodeEntry instance
            public NodeEntry Build()
            {
                return new NodeEntry(_menuName.ToArray(), _nodeType, _synonyms, _assetPath, _assetType, _compatibleSlotId, _slotName);
            }
        }
    }
    #endregion

    public class SearchWindowProvider : IDisposable
    {
        public static List<NodeEntry> NodeEntries = new();

        public EditorWindow EditorWindow;
        public BlueprintGraphEditorView GraphEditorView;
        public Texture2D Icon;


        public BlueprintPort connectedPort { get; set; }
        public VisualElement target { get; set; }
        public bool regenerateEntries { get; set; }

        public void Initialize(EditorWindow editorWindow, BlueprintGraphEditorView graphEditorView)
        {
            EditorWindow = editorWindow;
            GraphEditorView = graphEditorView;
            GenerateNodeEntries();

            // Transparent icon to trick search window into indenting items
            Icon = new Texture2D(1, 1);
            Icon.SetPixel(0, 0, new Color(0, 0, 0, 0));
            Icon.Apply();
        }

        public void Dispose()
        {
            if (Icon != null)
            {
                Object.DestroyImmediate(Icon);
                Icon = null;
            }

            EditorWindow = null;
            GraphEditorView = null;
            connectedPort = null;
        }

        public void GenerateNodeEntries()
        {
            NodeEntries.Clear();

            var types = TypeCache.GetTypesWithAttribute<SearchableNodeAttribute>();
            foreach (var type in types)
            {
                var atr = type.GetCustomAttribute<SearchableNodeAttribute>();
                FilterNewNode(type, atr);
            }

            SortEntries();
        }

        private void SortEntries()
        {
            // Sort the entries lexicographically by group then title with the requirement that items always comes before sub-groups in the same group.
            // Example result:
            // - Art/BlendMode
            // - Art/Adjustments/ColorBalance
            // - Art/Adjustments/Contrast
            NodeEntries.Sort((entry1, entry2) =>
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
                            var slotOrder = entry1.CompatibleSlotId.CompareTo(entry2.CompatibleSlotId);
                            return alphaOrder.CompareTo(slotOrder);
                        }

                        return value;
                    }
                }
                return 0;
            });
        }

        private void FilterNewNode(Type nodeType, SearchableNodeAttribute attribute)
        {
            if (attribute.MenuName.Length == 0) { return; }
            if (nodeType.IsSubclassOf(typeof(NodeModel)))
            {
                if (attribute.TypeCollection != null)
                {
                    var collection = AssetDatabaseUtility.FindAssetsByType(attribute.TypeCollection);
                    foreach (var item in collection)
                    {
                        if (connectedPort == null)
                        {
                            ValueTuple<string, Type> containerTarget = new(AssetDatabase.GetAssetPath(item), item.GetType());

                            NodeEntry nodeEntry = new NodeEntry.Builder()
                                .WithNode(nodeType, attribute.MenuName)
                                .WithName(item.name)
                                .WithAsset(AssetDatabase.GetAssetPath(item), item.GetType())    
                                .Build();
                            NodeEntries.Add(nodeEntry);
                        }
                    }
                }
                else
                {
                    if (connectedPort == null)
                    {
                        if (GraphEditorView.SearchIncludeFlags.Intersect(attribute.IncludeFlags).Count() > 0)
                        {
                            NodeEntry nodeEntry = new NodeEntry.Builder()
                                .WithNode(nodeType, attribute.MenuName)
                                .Build();
                            NodeEntries.Add(nodeEntry);
                        }
                    }
                }
            }
        }
    }

    public class SearcherProvider : SearchWindowProvider
    {
        public Searcher LoadSearchWindow()
        {
            if (regenerateEntries)
            {
                GenerateNodeEntries();
                regenerateEntries = false;
            }

            //create empty root for searcher tree
            var root = new List<SearcherItem>();
            var dummyEntry = new NodeEntry();

            foreach (var nodeEntry in NodeEntries)
            {
                SearcherItem item = null;
                SearcherItem parent = null;
                for (int i = 0; i < nodeEntry.MenuName.Length; i++)
                {
                    var pathEntry = nodeEntry.MenuName[i];
                    List<SearcherItem> children = parent != null ? parent.Children : root;
                    item = children.Find(x => x.Name == pathEntry);

                    if (item == null)
                    {
                        //if we have slot entries and are at a leaf, add the slot name to the entry title
                        if (nodeEntry.CompatibleSlotId != -1 && i == nodeEntry.MenuName.Length - 1)
                            item = new SearchNodeItem(pathEntry + ": " + nodeEntry.SlotName, nodeEntry, nodeEntry.Synonyms);
                        //if we don't have slot entries and are at a leaf, add userdata to the entry
                        else if (nodeEntry.CompatibleSlotId == -1 && i == nodeEntry.MenuName.Length - 1)
                            item = new SearchNodeItem(pathEntry, nodeEntry, nodeEntry.Synonyms);
                        //if we aren't a leaf, don't add user data
                        else
                            item = new SearchNodeItem(pathEntry, dummyEntry, null);

                        if (parent != null)
                        {
                            parent.AddChild(item);
                        }
                        else
                        {
                            children.Add(item);
                        }
                    }

                    parent = item;

                    if (parent.Depth == 0 && !root.Contains(parent))
                        root.Add(parent);
                }
            }

            var nodeDatabase = SearcherDatabase.Create(root, string.Empty, false);

            return new Searcher(nodeDatabase, new SearchWindowAdapter("Create Node"));
        }

        public bool OnSearcherSelectEntry(SearcherItem entry, Vector2 screenMousePosition)
        {
            if (entry == null || (entry as SearchNodeItem).NodeGUID.NodeType == null)
                return true;

            var nodeEntry = (entry as SearchNodeItem).NodeGUID;

            var windowRoot = EditorWindow.rootVisualElement;
            var windowMousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent, screenMousePosition); //- m_EditorWindow.position.position);
            var graphMousePosition = GraphEditorView.GraphView.contentViewContainer.WorldToLocal(windowMousePosition);

            var node = CopyNodeForGraph(nodeEntry.NodeType, graphMousePosition, nodeEntry.AssetPath, nodeEntry.AssetType); ;

            //m_Graph.owner.RegisterCompleteObjectUndo("Add " + node.name);

            GraphEditorView.AddNode(node);

            if (connectedPort != null)
            {
                //var connectedSlot = connectedPort.slot;
                //var connectedSlotReference = connectedSlot.owner.GetSlotReference(connectedSlot.id);
                //var compatibleSlotReference = node.GetSlotReference(nodeEntry.compatibleSlotId);

                //var fromReference = connectedSlot.isOutputSlot ? connectedSlotReference : compatibleSlotReference;
                //var toReference = connectedSlot.isOutputSlot ? compatibleSlotReference : connectedSlotReference;
                //m_Graph.Connect(fromReference, toReference);

                //nodeNeedsRepositioning = true;
                //targetSlotReference = compatibleSlotReference;
                //targetPosition = graphMousePosition;
            }

            return true;
        }

        public NodeModel CopyNodeForGraph(Type nodeType, Vector2 graphMousePos, string assetPath, Type assetType)
        {
            if (nodeType.IsSubclassOf(typeof(NodeModel)))
            {
                var n = Activator.CreateInstance(nodeType);
                if (n is NodeModel node)
                {
                    node.Position = new Rect(graphMousePos, Vector2.zero);
                    node.Guid = NodeModel.CreateGuid();
                    node.NodeType = node.GetType().AssemblyQualifiedName;
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                            var asset = AssetDatabase.LoadAssetAtPath(assetPath, assetType);
                            var objectNode = (UnityObjectValueNode<Object>)node;
                            objectNode.Value = asset;
                            objectNode.Name = asset.name;
                    }
                    else
                    {
                        var searchableAtr = nodeType.GetCustomAttribute<SearchableNodeAttribute>();
                        node.Name = searchableAtr != null ? searchableAtr.NodeName : nodeType.Name;
                    }
                    return node;
                }
            }
            return null;
        }
    }
}
