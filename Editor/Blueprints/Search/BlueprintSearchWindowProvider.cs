using System;
using System.Collections.Generic;
using System.Reflection;
using PlasticPipe.PlasticProtocol.Messages;
using UnityEditor;
using UnityEngine;
using Vapor.Blueprints;
using Vapor.Inspector;
using VaporEditor.Inspector;

namespace VaporEditor.Blueprints
{
    public class BlueprintSearchWindowProvider : SearchWindowProvider<BlueprintSearchEntry>
    {
        public BlueprintGraphSo Graph { get; set; }
        public BlueprintEditorPort ConnectedPort { get; set; }
        
        protected override void GenerateEntries()
        {
            var libs = TypeCache.GetTypesWithAttribute<BlueprintLibraryAttribute>();
            foreach (var lib in libs)
            {
                var methods = lib.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                foreach (var methodInfo in methods)
                {
                    var pureAtr = methodInfo.GetCustomAttribute<BlueprintPureAttribute>();
                    if (pureAtr != null)
                    {
                        BlueprintSearchEntry entry = new BlueprintSearchEntry.Builder()
                            .WithNode(methodInfo)
                            .WithCategoryAndName(pureAtr.MenuName, pureAtr.NodeName.EmptyOrNull() ? methodInfo.Name : pureAtr.NodeName)
                            .WithSynonyms(pureAtr.Synonyms)
                            .Build();
                        Entries.Add(entry);
                    }
                    
                    var callableAtr = methodInfo.GetCustomAttribute<BlueprintCallableAttribute>();
                    if (callableAtr != null)
                    {
                        BlueprintSearchEntry entry = new BlueprintSearchEntry.Builder()
                            .WithNode(methodInfo)
                            .WithCategoryAndName(callableAtr.MenuName, callableAtr.NodeName.EmptyOrNull() ? methodInfo.Name : callableAtr.NodeName)
                            .WithSynonyms(callableAtr.Synonyms)
                            .Build();
                        Entries.Add(entry);
                    }
                }
            }

            var bpTypes = TypeCache.GetTypesWithAttribute<BlueprintableAttribute>();
            foreach (var bpType in bpTypes)
            {
                var methods = bpType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var methodInfo in methods)
                {
                    var pureAtr = methodInfo.GetCustomAttribute<BlueprintPureAttribute>();
                    if (pureAtr != null)
                    {
                        BlueprintSearchEntry entry = new BlueprintSearchEntry.Builder()
                            .WithNode(methodInfo)
                            .WithCategoryAndName(pureAtr.MenuName, pureAtr.NodeName.EmptyOrNull() ? methodInfo.Name : pureAtr.NodeName)
                            .WithSynonyms(pureAtr.Synonyms)
                            .Build();
                        Entries.Add(entry);
                    }
                    var callableAtr = methodInfo.GetCustomAttribute<BlueprintCallableAttribute>();
                    if (callableAtr != null)
                    {
                        BlueprintSearchEntry entry = new BlueprintSearchEntry.Builder()
                            .WithNode(methodInfo)
                            .WithCategoryAndName(callableAtr.MenuName, callableAtr.NodeName.EmptyOrNull() ? methodInfo.Name : callableAtr.NodeName)
                            .WithSynonyms(callableAtr.Synonyms)
                            .Build();
                        Entries.Add(entry);
                    }
                }
            }

            BlueprintSearchEntry returnEntry = new BlueprintSearchEntry.Builder()
                .WithCategoryAndName(new string[] { "Default" }, "Return Node")
                .WithSynonyms("Return")
                .WithNodeType(BlueprintNodeType.Return)
                .Build();
            Entries.Add(returnEntry);
            
            BlueprintSearchEntry ifElseEntry = new BlueprintSearchEntry.Builder()
                .WithCategoryAndName(new string[] { "Conditional" }, "If Else")
                .WithSynonyms("If, Else")
                .WithNodeType(BlueprintNodeType.IfElse)
                .Build();
            Entries.Add(ifElseEntry);
            
            BlueprintSearchEntry foreachEntry = new BlueprintSearchEntry.Builder()
                .WithCategoryAndName(new string[] { "Array" }, "ForEach")
                .WithSynonyms("For", "Loop")
                .WithNodeType(BlueprintNodeType.ForEach)
                .Build();
            Entries.Add(foreachEntry);

            foreach (var temp in Graph.TempData)
            {
                BlueprintSearchEntry getterEntry = new BlueprintSearchEntry.Builder()
                    .WithCategoryAndName(new[] { "Default" }, $"Get {temp.FieldName}")
                    .WithSynonyms("Get")
                    .WithNodeType(BlueprintNodeType.Getter)
                    .WithGetterSetterFieldName(temp.FieldName)
                    .Build();
                Entries.Add(getterEntry);
                
                BlueprintSearchEntry setterEntry = new BlueprintSearchEntry.Builder()
                    .WithCategoryAndName(new[] { "Default" }, $"Set {temp.FieldName}")
                    .WithSynonyms("Set")
                    .WithNodeType(BlueprintNodeType.Setter)
                    .WithGetterSetterFieldName(temp.FieldName)
                    .Build();
                Entries.Add(setterEntry);
            }
        }

        protected override void SortEntries()
        {
            // Sort the entries lexicographically by group then title with the requirement that items always comes before sub-groups in the same group.
            // Example result:
            // - Art/BlendMode
            // - Art/Adjustments/ColorBalance
            // - Art/Adjustments/Contrast
            Entries.Sort((entry1, entry2) =>
            {
                for (var i = 0; i < entry1.MenuName.Length; i++)
                {
                    if (i >= entry2.MenuName.Length)
                    {
                        return 1;
                    }

                    var value = string.Compare(entry1.MenuName[i], entry2.MenuName[i], StringComparison.Ordinal);
                    if (value != 0)
                    {
                        // Make sure that leaves go before nodes
                        if (entry1.MenuName.Length != entry2.MenuName.Length && (i == entry1.MenuName.Length - 1 || i == entry2.MenuName.Length - 1))
                        {
                            //once nodes are sorted, sort slot entries by slot order instead of alphabetically
                            var alphaOrder = entry1.MenuName.Length < entry2.MenuName.Length ? -1 : 1;
                            return alphaOrder;
                            //var slotOrder = entry1.CompatibleSlotId.CompareTo(entry2.CompatibleSlotId);
                            //return alphaOrder.CompareTo(slotOrder);
                        }

                        return value;
                    }
                }
                return 0;
            });
        }

        public Searcher LoadSearchWindow(bool allowMultiSelect, out List<SearcherItem> searcherItems)
        {
            if (RegenerateEntries)
            {
                GenerateEntries();
                SortEntries();
                RegenerateEntries = false;
            }

            //create empty root for searcher tree
            var root = new List<SearcherItem>();
            foreach (var nodeEntry in Entries)
            {
                root.Add(new BlueprintSearcherItem(nodeEntry));
            }

            searcherItems = root;
            var nodeDatabase = SearcherDatabase.Create(SearcherTreeUtility.CreateFromFlatList(root), string.Empty, false);
            return new Searcher(nodeDatabase, new BlueprintSearchWindowAdapter(null, allowMultiSelect));
        }

        public bool OnSearcherSelectEntry(SearcherItem entry, Vector2 displayPosition)
        {
            if (entry == null)
            {
                Selector.DeselectAll();
            }
            else
            {
                var ud = ((BlueprintSearchEntry, Vector2))entry.UserData;
                var ud2 = (ud.Item1, displayPosition);
                entry.UserData = ud2;
                Selector.Select(entry);
            }
            return true;
        }
    }
}
