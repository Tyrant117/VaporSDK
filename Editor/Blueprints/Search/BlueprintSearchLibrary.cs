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
    public class BlueprintSearchModel
    {
        // Required
        public string Category { get; }
        public string Name { get; }
        public bool SupportFavorite { get; private set; }
        
        // Optional
        public List<string> Synonyms { get; private set; }

        // User Data
        public List<ValueTuple<string, object>> Parameters { get; private set; }

        public BlueprintSearchModel(string category, string name, bool supportFavorite = true)
        {
               Category = category;
               Name = name;
               SupportFavorite = supportFavorite;
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

        public BlueprintSearchModel WithParameters(params ValueTuple<string, object>[] parameters)
        {
            if (parameters == null)
            {
                return this;
            }
            
            Parameters ??= new List<ValueTuple<string, object>>();
            Parameters.AddRange(parameters);
            return this;
        }

        public BlueprintSearchModel WithGraph(BlueprintClassGraphModel graphModel)
        {
            Parameters ??= new List<ValueTuple<string, object>>();
            var idx = Parameters.FindIndex(v => v.Item1 == SearchModelParams.GRAPH_PARAM);
            if (idx != -1)
            {
                Parameters[idx] = (SearchModelParams.GRAPH_PARAM, graphModel.Current);
            }
            else
            {
                Parameters.Add((SearchModelParams.GRAPH_PARAM, graphModel.Current));
            }
            
            return this;
        }

        public BlueprintSearchModel WithPin(BlueprintPortView portView)
        {
            Parameters ??= new List<ValueTuple<string, object>>();
            var idx = Parameters.FindIndex(v => v.Item1 == SearchModelParams.PORT_PARAM);
            if (idx != -1)
            {
                Parameters[idx] = (SearchModelParams.PORT_PARAM, portView);
            }
            else
            {
                Parameters.Add((SearchModelParams.PORT_PARAM, portView));
            }
            
            return this;
        }

        public bool TryGetParameter<T>(string name, out T parameter)
        {
            var idx = Parameters.FindIndex(v => v.Item1 == name);
            if (idx != -1)
            {
                parameter = (T)Parameters[idx].Item2;
                return true;
            }

            parameter = default;
            return false;
        }

        public bool TryGetParameterWithType<T>(out T parameter)
        {
            var idx = Parameters.FindIndex(v => v.Item2.GetType() == typeof(T));
            if (idx != -1)
            {
                parameter = (T)Parameters[idx].Item2;
                return true;
            }

            parameter = default;
            return false;
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
                    modelDescs.Add(new BlueprintSearchModel(string.Join('/', callableAtr.MenuName), callableAtr.NodeName.EmptyOrNull() ? methodInfo.Name : callableAtr.NodeName)
                        .WithSynonyms(callableAtr.Synonyms)
                        .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Method), (SearchModelParams.METHOD_INFO_PARAM, methodInfo)));
                }
            }
            return modelDescs;
        }

        private static List<BlueprintSearchModel> LoadInternalDescriptors()
        {
            var modelDescs = new List<BlueprintSearchModel>();

            modelDescs.Add(new BlueprintSearchModel("Utilities", "Cast")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Cast))
                .WithSynonyms("Is", "As"));
            
            // Flow Control
            modelDescs.Add(new BlueprintSearchModel("Utilities/Flow Control", "Branch")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Branch))
                .WithSynonyms("If", "Else"));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Flow Control", "Switch")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Switch)));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Flow Control", "While")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.While))
                .WithSynonyms("Do"));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Flow Control", "Sequence")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Sequence)));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Flow Control", "Continue")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Continue)));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Flow Control", "Break")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Break)));
            
            modelDescs.Add(new BlueprintSearchModel("Utilities/Array", "For")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.For))
                .WithSynonyms("Loop"));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Array", "ForEach")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.ForEach))
                .WithSynonyms("Loop"));
            
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Float")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline), (SearchModelParams.DATA_TYPE_PARAM, typeof(float))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Double")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(double))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Int")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(int))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Long")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(long))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make String")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(string))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Color")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(Color))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Gradient")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(Gradient))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make LayerMask")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(LayerMask))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make RenderingLayerMask")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(RenderingLayerMask))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Vector2")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(Vector2))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Vector3")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(Vector3))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Vector4")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(Vector4))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Vector2Int")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(Vector2Int))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Vector3Int")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(Vector3Int))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Rect")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(Rect))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make RectInt")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(RectInt))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make Bounds")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(Bounds))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make BoundsInt")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(BoundsInt))));
            modelDescs.Add(new BlueprintSearchModel("Utilities/Inline Types", "Make AnimationCurve")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Inline),(SearchModelParams.DATA_TYPE_PARAM, typeof(AnimationCurve))));
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
                
                yield return new BlueprintSearchModel($"{category}/Fields", $"Get {ObjectNames.NicifyVariableName(fieldInfo.Name)}")
                    .WithSynonyms(fieldInfo.Name, type.Name, $"{type.Name}.{fieldInfo.Name}")
                    .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.MemberAccess), (SearchModelParams.VARIABLE_ACCESS_PARAM, VariableAccessType.Get), (SearchModelParams.FIELD_INFO_PARAM, fieldInfo));

                yield return new BlueprintSearchModel($"{category}/Fields", $"Set {ObjectNames.NicifyVariableName(fieldInfo.Name)}")
                    .WithSynonyms(fieldInfo.Name, type.Name, $"{type.Name}.{fieldInfo.Name}")
                    .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.MemberAccess), (SearchModelParams.VARIABLE_ACCESS_PARAM, VariableAccessType.Set), (SearchModelParams.FIELD_INFO_PARAM, fieldInfo));
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

                yield return new BlueprintSearchModel($"{category}/Methods", $"{name}({paramNames})")
                    .WithSynonyms(name, type.Name, $"{type.Name}.{methodInfo.Name}")
                    .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Method), (SearchModelParams.METHOD_INFO_PARAM, methodInfo));
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
