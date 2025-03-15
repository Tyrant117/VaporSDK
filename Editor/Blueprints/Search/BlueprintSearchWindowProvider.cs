using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Vapor;
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
            if (ConnectedPort != null && !ConnectedPort.Pin.IsExecutePin)
            {
                var pinType = ConnectedPort.Pin.Type;
                ConstructNodesForType(pinType, BindingFlags.Public | BindingFlags.Instance, true);
                
                ConstructLibraryNodes();
                return;
            }

            // ConstructNamespaceTypes();
            
            ConstructInternalNodes();
            
            ConstructLibraryNodes();

            ConstructBlueprintableNodes();
            
            ConstructGetterSetterNodes();
        }

        private void ConstructGetterSetterNodes()
        {
            foreach (var temp in Graph.DesignGraph.Variables)
            {
                BlueprintSearchEntry getterEntry = new BlueprintSearchEntry.Builder()
                    .WithCategoryAndName(new[] { "Variables" }, $"Get {temp.Name}")
                    .WithSynonyms("Get")
                    .WithNodeType(BlueprintNodeType.Getter)
                    .WithNameData(temp.Name)
                    .Build();
                Entries.Add(getterEntry);
                
                BlueprintSearchEntry setterEntry = new BlueprintSearchEntry.Builder()
                    .WithCategoryAndName(new[] { "Variables" }, $"Set {temp.Name}")
                    .WithSynonyms("Set")
                    .WithNodeType(BlueprintNodeType.Setter)
                    .WithNameData(temp.Name)
                    .Build();
                Entries.Add(setterEntry);
            }
        }

        private void ConstructInternalNodes()
        {
            BlueprintSearchEntry returnEntry = new BlueprintSearchEntry.Builder()
                .WithCategoryAndName(new string[] { "Utilities", "Flow Control" }, "Return Node")
                .WithSynonyms("Return")
                .WithNodeType(BlueprintNodeType.Return)
                .Build();
            Entries.Add(returnEntry);
            
            BlueprintSearchEntry ifElseEntry = new BlueprintSearchEntry.Builder()
                .WithCategoryAndName(new[] { "Utilities", "Flow Control" }, "Branch")
                .WithSynonyms("If, Else")
                .WithNodeType(BlueprintNodeType.IfElse)
                .Build();
            Entries.Add(ifElseEntry);
            
            BlueprintSearchEntry foreachEntry = new BlueprintSearchEntry.Builder()
                .WithCategoryAndName(new[] { "Utilities", "Array" }, "ForEach")
                .WithSynonyms("For", "Loop")
                .WithNodeType(BlueprintNodeType.ForEach)
                .Build();
            Entries.Add(foreachEntry);
            
            BlueprintSearchEntry rerouteEntry = new BlueprintSearchEntry.Builder()
                .WithCategoryAndName(new[] { "Utilities", "Flow Control" }, "Reroute")
                .WithNodeType(BlueprintNodeType.Reroute)
                .Build();
            Entries.Add(rerouteEntry);
            
            RuntimeAssetDatabaseUtility.FindAssetsByType<BlueprintGraphSo>().ForEach(graph =>
            {
                var path = AssetDatabase.GetAssetPath(graph);
                var guid = AssetDatabase.AssetPathToGUID(path);
                
                BlueprintSearchEntry graphEntry = new BlueprintSearchEntry.Builder()
                    .WithCategoryAndName(new[] { "Graphs" }, $"{graph.DisplayName}")
                    .WithNodeType(BlueprintNodeType.Graph)
                    .WithNameData(guid)
                    .Build();
                Entries.Add(graphEntry);
            });
        }

        private void ConstructBlueprintableNodes()
        {
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
                            .WithMethodInfo(methodInfo)
                            .WithCategoryAndName(pureAtr.MenuName, pureAtr.NodeName.EmptyOrNull() ? methodInfo.Name : pureAtr.NodeName)
                            .WithSynonyms(pureAtr.Synonyms)
                            .Build();
                        Entries.Add(entry);
                    }
                    var callableAtr = methodInfo.GetCustomAttribute<BlueprintCallableAttribute>();
                    if (callableAtr != null)
                    {
                        BlueprintSearchEntry entry = new BlueprintSearchEntry.Builder()
                            .WithMethodInfo(methodInfo)
                            .WithCategoryAndName(callableAtr.MenuName, callableAtr.NodeName.EmptyOrNull() ? methodInfo.Name : callableAtr.NodeName)
                            .WithSynonyms(callableAtr.Synonyms)
                            .Build();
                        Entries.Add(entry);
                    }
                }
            }
        }

        private void ConstructLibraryNodes()
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
                            .WithMethodInfo(methodInfo)
                            .WithCategoryAndName(pureAtr.MenuName, pureAtr.NodeName.EmptyOrNull() ? methodInfo.Name : pureAtr.NodeName)
                            .WithSynonyms(pureAtr.Synonyms)
                            .Build();
                        Entries.Add(entry);
                    }
                    
                    var callableAtr = methodInfo.GetCustomAttribute<BlueprintCallableAttribute>();
                    if (callableAtr != null)
                    {
                        BlueprintSearchEntry entry = new BlueprintSearchEntry.Builder()
                            .WithMethodInfo(methodInfo)
                            .WithCategoryAndName(callableAtr.MenuName, callableAtr.NodeName.EmptyOrNull() ? methodInfo.Name : callableAtr.NodeName)
                            .WithSynonyms(callableAtr.Synonyms)
                            .Build();
                        Entries.Add(entry);
                    }
                }
            }
        }

        private void ConstructNamespaceTypes()
        {
            foreach (var t in GetUnityEngineTypes())
            {
                ConstructNodesForType(t, BindingFlags.Public | BindingFlags.Static, false);
            }
        }
        
        private void ConstructNodesForType(Type pinType, BindingFlags searchBindingFlags, bool includeTypeStatics, BindingFlags staticSearchBindingFlags = BindingFlags.Public | BindingFlags.Static)
        {
            var fields = pinType.GetFields(searchBindingFlags);
            string category = pinType.IsGenericType ? $"{pinType.Name.Split('`')[0]}<{string.Join(",", pinType.GetGenericArguments().Select(a => a.Name))}>" : pinType.Name;
            foreach (var fieldInfo in fields)
            {
                BlueprintSearchEntry getEntry = new BlueprintSearchEntry.Builder()
                    .WithFieldInfo(fieldInfo)
                    .WithCategoryAndName(new[] { category, "Fields" }, $"Get {ObjectNames.NicifyVariableName(fieldInfo.Name)}")
                    .WithSynonyms(fieldInfo.Name)
                    .WithNodeType(BlueprintNodeType.FieldGetter)
                    .Build();
                Entries.Add(getEntry);
                
                BlueprintSearchEntry setEntry = new BlueprintSearchEntry.Builder()
                    .WithFieldInfo(fieldInfo)
                    .WithCategoryAndName(new[] { category, "Fields" }, $"Set {ObjectNames.NicifyVariableName(fieldInfo.Name)}")
                    .WithSynonyms(fieldInfo.Name)
                    .WithNodeType(BlueprintNodeType.FieldSetter)
                    .Build();
                Entries.Add(setEntry);
            }
            
            if (includeTypeStatics)
            {
                var staticFields = pinType.GetFields(staticSearchBindingFlags);
                foreach (var fieldInfo in staticFields)
                {
                    BlueprintSearchEntry getEntry = new BlueprintSearchEntry.Builder()
                        .WithFieldInfo(fieldInfo)
                        .WithCategoryAndName(new[] { category, "Static Fields" }, $"Get {ObjectNames.NicifyVariableName(fieldInfo.Name)}")
                        .WithSynonyms(fieldInfo.Name)
                        .WithNodeType(BlueprintNodeType.FieldGetter)
                        .Build();
                    Entries.Add(getEntry);
                
                    BlueprintSearchEntry setEntry = new BlueprintSearchEntry.Builder()
                        .WithFieldInfo(fieldInfo)
                        .WithCategoryAndName(new[] { category, "Static Fields" }, $"Set {ObjectNames.NicifyVariableName(fieldInfo.Name)}")
                        .WithSynonyms(fieldInfo.Name)
                        .WithNodeType(BlueprintNodeType.FieldSetter)
                        .Build();
                    Entries.Add(setEntry);
                }
            }
            
            
            var methods = pinType.GetMethods(searchBindingFlags);
            foreach (var methodInfo in methods)
            {
                var name = methodInfo.IsGenericMethod ? $"{methodInfo.Name.Split('`')[0]}<{string.Join(",", methodInfo.GetGenericArguments().Select(a => a.Name))}>" : methodInfo.Name;
                name = methodInfo.IsSpecialName ? BlueprintNodeDataModelUtility.ToTitleCase(name) : name;
                var parameters = methodInfo.GetParameters();
                string paramNames = parameters.Length > 0 ? parameters.Select(pi => pi.ParameterType.IsGenericType 
                        ? $"{pi.ParameterType.Name.Split('`')[0]}<{string.Join(",", pi.ParameterType.GetGenericArguments().Select(a => a.Name))}>"
                        : pi.ParameterType.Name)
                    .Aggregate((a, b) => a + ", " + b) 
                    : string.Empty;

                
                BlueprintSearchEntry entry = new BlueprintSearchEntry.Builder()
                    .WithMethodInfo(methodInfo)
                    .WithCategoryAndName(new[] { category, "Methods" }, $"{name}({paramNames})")
                    .WithSynonyms(name)
                    .WithNodeType(BlueprintNodeType.Method)
                    .Build();
                Entries.Add(entry);
            }

            if (includeTypeStatics)
            {
                var staticMethods = pinType.GetMethods(staticSearchBindingFlags);
                foreach (var methodInfo in staticMethods)
                {
                    var name = methodInfo.IsGenericMethod ? $"{methodInfo.Name.Split('`')[0]}<{string.Join(",", methodInfo.GetGenericArguments().Select(a => a.Name))}>" : methodInfo.Name;
                    name = methodInfo.IsSpecialName ? BlueprintNodeDataModelUtility.ToTitleCase(name) : name;
                    var parameters = methodInfo.GetParameters();
                    string paramNames = parameters.Length > 0 ? parameters.Select(pi => pi.ParameterType.IsGenericType 
                                ? $"{pi.ParameterType.Name.Split('`')[0]}<{string.Join(",", pi.ParameterType.GetGenericArguments().Select(a => a.Name))}>"
                                : pi.ParameterType.Name)
                            .Aggregate((a, b) => a + ", " + b) 
                        : string.Empty;
                    
                    BlueprintSearchEntry entry = new BlueprintSearchEntry.Builder()
                        .WithMethodInfo(methodInfo)
                        .WithCategoryAndName(new[] { category, "Static Methods" }, $"{name}({paramNames})")
                        .WithSynonyms(name)
                        .WithNodeType(BlueprintNodeType.Method)
                        .Build();
                    Entries.Add(entry);
                }
            }
        }
        
        static Type[] GetUnityEngineTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        return e.Types.Where(t => t != null); // Handle partially loadable assemblies
                    }
                    catch
                    {
                        return Array.Empty<Type>(); // Handle inaccessible assemblies
                    }
                })
                .Where(type => type != null && type.IsPublic && type.Namespace == "UnityEngine")
                .ToArray();
        }

        protected override void SortEntries()
        {
            // Sort the entries lexicographically by group then title with the requirement that items always comes before sub-groups in the same group.
            // Example result:
            // - Art/BlendMode
            // - Art/Adjustments/ColorBalance
            // - Art/Adjustments/Contrast
            // Entries.Sort((entry1, entry2) =>
            // {
            //     for (var i = 0; i < entry1.MenuName.Length; i++)
            //     {
            //         if (i >= entry2.MenuName.Length)
            //         {
            //             return 1;
            //         }
            //
            //         var value = string.Compare(entry1.MenuName[i], entry2.MenuName[i], StringComparison.Ordinal);
            //         if (value != 0)
            //         {
            //             // Make sure that leaves go before nodes
            //             if (entry1.MenuName.Length != entry2.MenuName.Length && (i == entry1.MenuName.Length - 1 || i == entry2.MenuName.Length - 1))
            //             {
            //                 //once nodes are sorted, sort slot entries by slot order instead of alphabetically
            //                 var alphaOrder = entry1.MenuName.Length < entry2.MenuName.Length ? -1 : 1;
            //                 return alphaOrder;
            //                 //var slotOrder = entry1.CompatibleSlotId.CompareTo(entry2.CompatibleSlotId);
            //                 //return alphaOrder.CompareTo(slotOrder);
            //             }
            //
            //             return value;
            //         }
            //     }
            //     return 0;
            // });
        }

        public Searcher LoadSearchWindow(bool allowMultiSelect, out List<SearcherItem> searcherItems)
        {
            if (RegenerateEntries)
            {
                Entries.Clear();
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
                if (entry.UserData == null)
                {
                    return false;
                }
                var ud = ((BlueprintSearchEntry, Vector2))entry.UserData;
                var ud2 = (ud.Item1, displayPosition);
                entry.UserData = ud2;
                Selector.Select(entry);
            }
            return true;
        }
    }
}
