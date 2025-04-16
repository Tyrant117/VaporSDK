using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Vapor;
using Vapor.Blueprints;
using Vapor.Inspector;
using VaporEditor.Inspector;
using Object = UnityEngine.Object;

namespace VaporEditor.Blueprints
{
    public struct MemberSearchData
    {
        public FieldInfo FieldInfo;
        public PropertyInfo PropertyInfo;
        public string Id;
        public VariableAccessType AccessType;
        public VariableScopeType ScopeType;
    }

    public struct ConstructorSearchData
    {
        public Type TypeToConstruct;
        public string ConstructorSignature;
        public bool IsArray;
    }

    public class BlueprintSearchModel
    {
        // Required
        public string Category { get; }
        public string Name { get; }
        public bool SupportFavorite { get; private set; }
        public NodeType NodeType { get; private set; }
        
        // Optional
        public List<string> Synonyms { get; private set; }
        public BlueprintClassGraphModel Class { get; private set; }
        public BlueprintPortView PortView { get; private set; }

        // User Data
        public bool IsStaticAccessor { get; }
        public object UserData { get; private set; }

        public BlueprintSearchModel(string category, string name, NodeType nodeType, bool supportFavorite = true)
        {
            Category = category;
            Name = name;
            NodeType = nodeType;
            SupportFavorite = supportFavorite;
        }

        public BlueprintSearchModel(string category, string name, bool supportFavorite = true)
        {
            Category = category;
            Name = name;
            SupportFavorite = supportFavorite;
            IsStaticAccessor = true;
        }

        public BlueprintSearchModel WithSynonyms(params string[] synonyms)
        {
            if (synonyms == null)
            {
                return this;
            }
            
            Synonyms ??= new List<string>();
            Synonyms.AddRange(synonyms);
            return this;
        }

        public BlueprintSearchModel WithUserData(object userData)
        {
            UserData = userData;
            return this;
        }

        public BlueprintSearchModel WithGraph(BlueprintClassGraphModel classModel)
        {
            Class = classModel;
            return this;
        }
        
        public BlueprintSearchModel WithPinView(BlueprintPortView pin)
        {
            PortView = pin;
            return this;
        }
    }
    
    public class BlueprintLibrarySentinel : ScriptableObject
    {
        private void OnDisable()
        {
            BlueprintSearchLibrary.ClearLibrary();
        }
    }

    public static class BlueprintSearchLibrary
    {
        private static BlueprintLibrarySentinel s_Sentinel;
        private static volatile bool s_Loaded;
        private static readonly object s_Lock = new();
        
        private static volatile List<BlueprintSearchModel> s_LibraryDescriptors;
        private static volatile List<BlueprintSearchModel> s_InternalDescriptors;
        private static volatile List<BlueprintSearchModel> s_UnityDescriptors;
        
        public static IEnumerable<BlueprintSearchModel> GetBlueprintLibraries()
        {
            LoadIfNeeded();
            return s_LibraryDescriptors;
        }
        
        public static IEnumerable<BlueprintSearchModel> GetInternalDescriptors()
        {
            LoadIfNeeded();
            return s_InternalDescriptors;
        }

        public static IEnumerable<BlueprintSearchModel> GetUnityLibraries()
        {
            LoadIfNeeded();
            return s_UnityDescriptors;
        }
        
        private static void LoadIfNeeded()
        {
            if (s_Loaded)
            {
                return;
            }

            lock (s_Lock)
            {
                if (!s_Loaded)
                {
                    Load();
                }
            }
        }

        private static void Load()
        {
            Profiler.BeginSample("BlueprintLibrary.Load");
            try
            {
                lock (s_Lock)
                {
                    if (s_Sentinel)
                    {
                        Object.DestroyImmediate(s_Sentinel);
                    }

                    s_Sentinel = ScriptableObject.CreateInstance<BlueprintLibrarySentinel>();

                    s_UnityDescriptors = LoadUnityLibraries();
                    s_LibraryDescriptors = LoadBlueprintLibrary();
                    s_InternalDescriptors = LoadInternalDescriptors();
                    s_Loaded = true;
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        public static void ClearLibrary()
        {
            lock (s_Lock)
            {
                if (s_Loaded)
                {
                    s_Loaded = false;
                }
            }
        }
        
        private static List<BlueprintSearchModel> LoadBlueprintLibrary()
        {
            var modelDescs = new List<BlueprintSearchModel>();
            var libs = TypeCache.GetTypesWithAttribute<BlueprintLibraryAttribute>();
            foreach (var lib in libs)
            {
                var methods = lib.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                foreach (var methodInfo in methods)
                {
                    if (methodInfo.IsDefined(typeof(ObsoleteAttribute), false))
                    {
                        continue;
                    }
                    
                    var callableAtr = methodInfo.GetCustomAttribute<BlueprintCallableAttribute>();
                    modelDescs.Add(new BlueprintSearchModel(string.Join('/', callableAtr.MenuName), callableAtr.NodeName.EmptyOrNull() ? methodInfo.Name : callableAtr.NodeName, NodeType.Method)
                        .WithSynonyms(callableAtr.Synonyms)
                        .WithUserData(methodInfo));
                }
            }
            return modelDescs;
        }

        private static List<BlueprintSearchModel> LoadInternalDescriptors()
        {
            var modelDescs = new List<BlueprintSearchModel>();

            modelDescs.Add(new BlueprintSearchModel("Utilities", "Cast", NodeType.Cast)
                .WithSynonyms("Is", "As"));
            modelDescs.Add(new BlueprintSearchModel("Utilities", "Construct", NodeType.Constructor)
                .WithSynonyms("New", "Create"));
            
            // Flow Control
            modelDescs.Add(new BlueprintSearchModel("Utilities/Flow Control", "Branch", NodeType.Branch)
                .WithSynonyms("If", "Else"));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Flow Control", "Switch", NodeType.Switch));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Flow Control", "While", NodeType.While)
                .WithSynonyms("Do"));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Flow Control", "Sequence", NodeType.Sequence));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Flow Control", "Continue", NodeType.Continue));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Flow Control", "Break", NodeType.Break));
            
            modelDescs.Add(new BlueprintSearchModel("Utilities/Array", "For", NodeType.For)
                .WithSynonyms("Loop"));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Array", "ForEach", NodeType.ForEach)
                .WithSynonyms("Loop"));
            
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Float", NodeType.Inline)
            //     .WithUserData( typeof(float)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Double", NodeType.Inline)
            //     .WithUserData(typeof(double)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Int", NodeType.Inline)
            //     .WithUserData(typeof(int)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Long", NodeType.Inline)
            //     .WithUserData(typeof(long)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make String", NodeType.Inline)
            //     .WithUserData(typeof(string)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Color", NodeType.Inline)
            //     .WithUserData(typeof(Color)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Gradient", NodeType.Inline)
            //     .WithUserData(typeof(Gradient)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make LayerMask", NodeType.Inline)
            //     .WithUserData(typeof(LayerMask)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make RenderingLayerMask", NodeType.Inline)
            //     .WithUserData(typeof(RenderingLayerMask)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Vector2", NodeType.Inline)
            //     .WithUserData(typeof(Vector2)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Vector3", NodeType.Inline)
            //     .WithUserData(typeof(Vector3)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Vector4", NodeType.Inline)
            //     .WithUserData(typeof(Vector4)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Vector2Int", NodeType.Inline)
            //     .WithUserData(typeof(Vector2Int)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Vector3Int", NodeType.Inline)
            //     .WithUserData(typeof(Vector3Int)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Rect", NodeType.Inline)
            //     .WithUserData(typeof(Rect)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make RectInt", NodeType.Inline)
            //     .WithUserData(typeof(RectInt)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Bounds", NodeType.Inline)
            //     .WithUserData(typeof(Bounds)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make BoundsInt", NodeType.Inline)
            //     .WithUserData(typeof(BoundsInt)));
            // modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make AnimationCurve", NodeType.Inline)
            //     .WithUserData(typeof(AnimationCurve)));
            return modelDescs;
        }

        private static List<BlueprintSearchModel> LoadUnityLibraries()
        {
            var modelDescs = new List<BlueprintSearchModel>();
            var unityCoreAssemTypes = typeof(Object).Assembly.GetTypes();
            foreach (var unityType in unityCoreAssemTypes)
            {
                if (unityType.IsPublic && !unityType.IsNestedPublic && !unityType.IsAbstract)
                {
                    modelDescs.AddRange(GetStaticsForType(unityType, $"Unity/{unityType.Name}"));
                }
            }
            
            return modelDescs;
        }
        
        private static IEnumerable<BlueprintSearchModel> GetStaticsForType(Type type, string category)
        {
            var staticFields = ReflectionUtility.GetAllFieldsThatMatch(type, ReflectionUtility.IsPublicStatic, false, true);
            foreach (var fieldInfo in staticFields)
            {
                if (fieldInfo.DeclaringType is { IsEnum: true })
                {
                    continue;
                }
                
                if (fieldInfo.IsDefined(typeof(ObsoleteAttribute), false))
                {
                    continue;
                }

                yield return new BlueprintSearchModel($"{category}/Fields", $"Get {ObjectNames.NicifyVariableName(fieldInfo.Name)}", NodeType.MemberAccess)
                    .WithSynonyms(fieldInfo.Name, type.Name, $"{type.Name}.{fieldInfo.Name}")
                    .WithUserData(new MemberSearchData { AccessType = VariableAccessType.Get, FieldInfo = fieldInfo });

                yield return new BlueprintSearchModel($"{category}/Fields", $"Set {ObjectNames.NicifyVariableName(fieldInfo.Name)}", NodeType.MemberAccess)
                    .WithSynonyms(fieldInfo.Name, type.Name, $"{type.Name}.{fieldInfo.Name}")
                    .WithUserData(new MemberSearchData { AccessType = VariableAccessType.Set, FieldInfo = fieldInfo });
            }

            var staticMethods = ReflectionUtility.GetAllMethodsThatMatch(type, ReflectionUtility.IsPublicStatic, false, true);
            foreach (var methodInfo in staticMethods)
            {
                if (methodInfo.IsDefined(typeof(ObsoleteAttribute), false))
                {
                    continue;
                }

                if (methodInfo.IsSpecialName && (methodInfo.Name.StartsWith("op_Implicit") || methodInfo.Name.StartsWith("op_Explicit")))
                {
                    continue;
                }
                
                var name = methodInfo.IsGenericMethod ? $"{methodInfo.Name.Split('`')[0]}<{string.Join(",", methodInfo.GetGenericArguments().Select(a => a.Name))}>" : methodInfo.Name;
                name = methodInfo.IsSpecialName ? name.ToTitleCase() : name;
                var parameters = methodInfo.GetParameters();
                string paramNames = parameters.Length > 0
                    ? parameters.Select(pi => pi.ParameterType.IsGenericType
                            ? $"{pi.ParameterType.Name.Split('`')[0]}<{string.Join(",", pi.ParameterType.GetGenericArguments().Select(a => a.Name))}>"
                            : pi.ParameterType.Name)
                        .Aggregate((a, b) => a + ", " + b)
                    : string.Empty;

                yield return new BlueprintSearchModel($"{category}/Methods", $"{name}({paramNames})", NodeType.Method)
                    .WithSynonyms(name, type.Name, $"{type.Name}.{methodInfo.Name}")
                    .WithUserData(methodInfo);
            }
        }
    }

    internal static class BlueprintStringHelper
    {
        private static readonly Regex s_NodeNameParser = new("(?<label>[|]?[^\\|]*)", RegexOptions.Compiled);
        
        public static string ToHumanReadable(this string text)
        {
            return text.Replace("|_", " ").Replace('|', ' ').TrimStart();
        }

        public static IEnumerable<Label> SplitTextIntoLabels(this string text, string className)
        {
            var matches = s_NodeNameParser.Matches(text);
            if (matches.Count == 0)
            {
                yield return new Label(text);
                yield break;
            }
            foreach (var m in matches)
            {
                var match = (Match)m;
                if (match.Length == 0)
                    continue;
                if (match.Value.StartsWith("|_"))
                {
                    yield return new Label(match.Value.Substring(2, match.Length - 2));
                }
                else if (match.Value.StartsWith('|'))
                {
                    var label = new Label(match.Value.Substring(1, match.Length - 1));
                    label.AddToClassList(className);
                    yield return label;
                }
                else
                {
                    yield return new Label(match.Value);
                }
            }
        }
    }
}
