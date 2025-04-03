using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using Vapor;
using Vapor.Blueprints;
using VaporEditor.Inspector;

namespace VaporEditor.Blueprints
{
    internal class ContextSearchProvider : SearchProviderBase
    {
        private readonly Type _contextType;
        private readonly bool _includeStatics;
        private BlueprintPortView _pin;
        public ContextSearchProvider(Type contextType, bool includeStatics, Action<BlueprintSearchModel, Vector2> onSpawnNode) : base(onSpawnNode)
        {
            _contextType = contextType;
            _includeStatics = includeStatics;
        }

        public ContextSearchProvider WithPin(BlueprintPortView pin)
        {
            _pin = pin;
            return this;
        }

        public override IEnumerable<BlueprintSearchModel> GetDescriptors()
        {
            var fields = _contextType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            string category = _contextType.IsGenericType ? $"{_contextType.Name.Split('`')[0]}<{string.Join(",", _contextType.GetGenericArguments().Select(a => a.Name))}>" : _contextType.Name;
            foreach (var fieldInfo in fields)
            {
                yield return new BlueprintSearchModel($"{category}/Fields", $"Get {ObjectNames.NicifyVariableName(fieldInfo.Name)}")
                    .WithSynonyms(fieldInfo.Name)
                    .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.MemberAccess), (SearchModelParams.FIELD_INFO_PARAM, fieldInfo), (SearchModelParams.VARIABLE_ACCESS_PARAM, VariableAccessType.Get))
                    .WithPin(_pin);
                    
                yield return new BlueprintSearchModel($"{category}/Fields", $"Set {ObjectNames.NicifyVariableName(fieldInfo.Name)}")
                    .WithSynonyms(fieldInfo.Name)
                    .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.MemberAccess), (SearchModelParams.FIELD_INFO_PARAM, fieldInfo), (SearchModelParams.VARIABLE_ACCESS_PARAM, VariableAccessType.Set))
                    .WithPin(_pin);
            }

            var methods = _contextType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var methodInfo in methods)
            {
                if (methodInfo.IsDefined(typeof(ObsoleteAttribute), false))
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
                    .WithSynonyms(name)
                    .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Method), (SearchModelParams.METHOD_INFO_PARAM, methodInfo))
                    .WithPin(_pin);
            }

            if (_includeStatics)
            {
                foreach (var blueprintSearchModel in GetIncludedStatics(category))
                {
                    yield return blueprintSearchModel;
                }
            }

            foreach (var blueprintSearchModel in BlueprintSearchLibrary.GetInternalDescriptors())
            {
                yield return blueprintSearchModel.WithPin(_pin);
            }

            yield return new BlueprintSearchModel("Utilities/Flow Control", "Reroute")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Redirect), (SearchModelParams.DATA_TYPE_PARAM, _contextType))
                .WithPin(_pin);
        }

        private IEnumerable<BlueprintSearchModel> GetIncludedStatics(string category)
        {
            var staticFields = _contextType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var fieldInfo in staticFields)
            {
                yield return new BlueprintSearchModel($"{category}/Static Fields", $"Get {ObjectNames.NicifyVariableName(fieldInfo.Name)}")
                    .WithSynonyms(fieldInfo.Name)
                    .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.MemberAccess), (SearchModelParams.FIELD_INFO_PARAM, fieldInfo), (SearchModelParams.VARIABLE_ACCESS_PARAM, VariableAccessType.Get))
                    .WithPin(_pin);
                    
                yield return new BlueprintSearchModel($"{category}/Static Fields", $"Set {ObjectNames.NicifyVariableName(fieldInfo.Name)}")
                    .WithSynonyms(fieldInfo.Name)
                    .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.MemberAccess), (SearchModelParams.FIELD_INFO_PARAM, fieldInfo), (SearchModelParams.VARIABLE_ACCESS_PARAM, VariableAccessType.Set))
                    .WithPin(_pin);
            }
            
            var staticMethods = _contextType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (var methodInfo in staticMethods)
            {
                if (methodInfo.IsDefined(typeof(ObsoleteAttribute), false))
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

                yield return new BlueprintSearchModel($"{category}/Static Methods", $"{name}({paramNames})")
                    .WithSynonyms(name)
                    .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Method), (SearchModelParams.METHOD_INFO_PARAM, methodInfo))
                    .WithPin(_pin);
            }
        }
    }

    internal class DefaultSearchProvider : SearchProviderBase
    {
        private BlueprintDesignGraph _graph;

        public DefaultSearchProvider(Action<BlueprintSearchModel, Vector2> onSpawnNode) : base(onSpawnNode)
        {
        }

        public DefaultSearchProvider WithGraph(BlueprintDesignGraph graph)
        {
            _graph = graph;
            return this;
        }

        public override IEnumerable<BlueprintSearchModel> GetDescriptors()
        {
            foreach (var model in ConstructGetterSetterNodes())
            {
                yield return model.WithGraph(_graph);
            }
            
            foreach (var model in BlueprintSearchLibrary.GetUnityLibraries())
            {
                yield return model.WithGraph(_graph);
            }
            
            foreach (var model in BlueprintSearchLibrary.GetBlueprintLibraries())
            {
                yield return model.WithGraph(_graph);
            }

            foreach (var model in BlueprintSearchLibrary.GetInternalDescriptors())
            {
                yield return model.WithGraph(_graph);
            }
            
            yield return new BlueprintSearchModel("Utilities/Flow Control", "Return")
                .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.Return))
                .WithGraph(_graph);
            
        }
        
        private IEnumerable<BlueprintSearchModel> ConstructGetterSetterNodes()
        {
            foreach (var classVar in _graph.Variables)
            {
                yield return new BlueprintSearchModel("Variables/Global", $"Get {classVar.Name}")
                    .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.MemberAccess), (SearchModelParams.VARIABLE_NAME_PARAM, classVar.Name), (SearchModelParams.VARIABLE_SCOPE_PARAM, VariableScopeType.Class), (SearchModelParams.VARIABLE_ACCESS_PARAM, VariableAccessType.Get))
                    .WithGraph(_graph);
                
                yield return new BlueprintSearchModel("Variables/Global", $"Set {classVar.Name}")
                    .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.MemberAccess), (SearchModelParams.VARIABLE_NAME_PARAM, classVar.Name), (SearchModelParams.VARIABLE_SCOPE_PARAM, VariableScopeType.Class), (SearchModelParams.VARIABLE_ACCESS_PARAM, VariableAccessType.Set))
                    .WithGraph(_graph);
            }

            foreach (var methodVar in _graph.Current.TemporaryVariables)
            {
                yield return new BlueprintSearchModel("Variables/Local", $"Get {methodVar.Name}")
                    .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.MemberAccess), (SearchModelParams.VARIABLE_NAME_PARAM, methodVar.Name), (SearchModelParams.VARIABLE_SCOPE_PARAM, VariableScopeType.Method), (SearchModelParams.VARIABLE_ACCESS_PARAM, VariableAccessType.Get))
                    .WithGraph(_graph);
                
                yield return new BlueprintSearchModel("Variables/Local", $"Set {methodVar.Name}")
                    .WithParameters((SearchModelParams.NODE_TYPE_PARAM, NodeType.MemberAccess), (SearchModelParams.VARIABLE_NAME_PARAM, methodVar.Name), (SearchModelParams.VARIABLE_SCOPE_PARAM, VariableScopeType.Method), (SearchModelParams.VARIABLE_ACCESS_PARAM, VariableAccessType.Set))
                    .WithGraph(_graph);
            }
        }
    }

    internal class TypeSearchProvider : SearchProviderBase
    {
        public const string PARAM_TYPE_DATA = "TypeData";
        private static List<BlueprintSearchModel> s_CachedDescriptors;
        public TypeSearchProvider(Action<BlueprintSearchModel, Vector2> onSpawnNode) : base(onSpawnNode)
        {
            if (s_CachedDescriptors != null)
            {
                return;
            }

            var typeIterator = RuntimeSubclassUtility.GetCachedTypes().ToArray();
            s_CachedDescriptors = new List<BlueprintSearchModel>(typeIterator.Length);
            foreach (var t in typeIterator)
            {
                var typeName = t.IsGenericType ? $"{t.Name.Split('`')[0]}<{string.Join(",", t.GetGenericArguments().Select(a => a.Name))}>" : t.Name;
                s_CachedDescriptors.Add(new BlueprintSearchModel(t.Namespace?.Replace('.', '/'), typeName).WithParameters((PARAM_TYPE_DATA, t)).WithSynonyms($"{t.Namespace}.{typeName}"));
            }
        }

        public override IEnumerable<BlueprintSearchModel> GetDescriptors()
        {
            return s_CachedDescriptors;
        }
    }
    
    internal class TypeSearchProvider<T> : SearchProviderBase
    {
        // ReSharper disable once StaticMemberInGenericType
        private static List<BlueprintSearchModel> s_CachedDescriptors;
        public TypeSearchProvider(Action<BlueprintSearchModel, Vector2> onSpawnNode) : base(onSpawnNode)
        {
            if (s_CachedDescriptors != null)
            {
                return;
            }

            var typeIterator = RuntimeSubclassUtility.GetFilteredTypes<T>().ToArray();
            s_CachedDescriptors = new List<BlueprintSearchModel>(typeIterator.Length);
            foreach (var t in typeIterator)
            {
                var typeName = t.IsGenericType ? $"{t.Name.Split('`')[0]}<{string.Join(",", t.GetGenericArguments().Select(a => a.Name))}>" : t.Name;
                s_CachedDescriptors.Add(new BlueprintSearchModel(t.Namespace?.Replace('.','/'), typeName).WithParameters((TypeSearchProvider.PARAM_TYPE_DATA, t)).WithSynonyms($"{t.Namespace}.{typeName}"));
            }
        }

        public override IEnumerable<BlueprintSearchModel> GetDescriptors()
        {
            return s_CachedDescriptors;
        }
    }
    
    internal class TypeSearchProvider<T1 ,T2> : SearchProviderBase
    {
        // ReSharper disable once StaticMemberInGenericType
        private static List<BlueprintSearchModel> s_CachedDescriptors;
        public TypeSearchProvider(Action<BlueprintSearchModel, Vector2> onSpawnNode) : base(onSpawnNode)
        {
            if (s_CachedDescriptors != null)
            {
                return;
            }

            var validTypes = new HashSet<Type>();
            var typeIteratorT1 = TypeCache.GetTypesDerivedFrom<T1>();
            var typeIteratorT2 = TypeCache.GetTypesDerivedFrom<T2>();
            s_CachedDescriptors = new List<BlueprintSearchModel>(typeIteratorT1.Count + typeIteratorT2.Count);
            foreach (var t in typeIteratorT1.Where(t => t is { IsNested: false, IsPublic: true }))
            {
                var typeName = t.IsGenericType ? $"{t.Name.Split('`')[0]}<{string.Join(",", t.GetGenericArguments().Select(a => a.Name))}>" : t.Name;
                s_CachedDescriptors.Add(new BlueprintSearchModel(t.Namespace?.Replace('.','/'), typeName).WithParameters((TypeSearchProvider.PARAM_TYPE_DATA, t)).WithSynonyms($"{t.Namespace}.{typeName}"));
                validTypes.Add(t);
            }
            foreach (var t in typeIteratorT2.Where(t => t is { IsNested: false, IsPublic: true } && !validTypes.Contains(t)))
            {
                var typeName = t.IsGenericType ? $"{t.Name.Split('`')[0]}<{string.Join(",", t.GetGenericArguments().Select(a => a.Name))}>" : t.Name;
                s_CachedDescriptors.Add(new BlueprintSearchModel(t.Namespace?.Replace('.','/'), typeName).WithParameters((TypeSearchProvider.PARAM_TYPE_DATA, t)).WithSynonyms($"{t.Namespace}.{typeName}"));
            }
        }

        public override IEnumerable<BlueprintSearchModel> GetDescriptors()
        {
            return s_CachedDescriptors;
        }
    }
    
    internal class TypeSearchProvider<T1, T2, T3> : SearchProviderBase
    {
        // ReSharper disable once StaticMemberInGenericType
        private static List<BlueprintSearchModel> s_CachedDescriptors;
        public TypeSearchProvider(Action<BlueprintSearchModel, Vector2> onSpawnNode) : base(onSpawnNode)
        {
            if (s_CachedDescriptors != null)
            {
                return;
            }

            var validTypes = new HashSet<Type>();
            var typeIteratorT1 = TypeCache.GetTypesDerivedFrom<T1>();
            var typeIteratorT2 = TypeCache.GetTypesDerivedFrom<T2>();
            var typeIteratorT3 = TypeCache.GetTypesDerivedFrom<T3>();
            s_CachedDescriptors = new List<BlueprintSearchModel>(typeIteratorT1.Count + typeIteratorT2.Count + typeIteratorT3.Count);
            foreach (var t in typeIteratorT1.Where(t => t is { IsNested: false, IsPublic: true }))
            {
                var typeName = t.IsGenericType ? $"{t.Name.Split('`')[0]}<{string.Join(",", t.GetGenericArguments().Select(a => a.Name))}>" : t.Name;
                s_CachedDescriptors.Add(new BlueprintSearchModel(t.Namespace?.Replace('.','/'), typeName).WithParameters((TypeSearchProvider.PARAM_TYPE_DATA, t)).WithSynonyms($"{t.Namespace}.{typeName}"));
                validTypes.Add(t);
            }
            foreach (var t in typeIteratorT2.Where(t => t is { IsNested: false, IsPublic: true } && !validTypes.Contains(t)))
            {
                var typeName = t.IsGenericType ? $"{t.Name.Split('`')[0]}<{string.Join(",", t.GetGenericArguments().Select(a => a.Name))}>" : t.Name;
                s_CachedDescriptors.Add(new BlueprintSearchModel(t.Namespace?.Replace('.','/'), typeName).WithParameters((TypeSearchProvider.PARAM_TYPE_DATA, t)).WithSynonyms($"{t.Namespace}.{typeName}"));
                validTypes.Add(t);
            }
            foreach (var t in typeIteratorT2.Where(t => t is { IsNested: false, IsPublic: true } && !validTypes.Contains(t)))
            {
                var typeName = t.IsGenericType ? $"{t.Name.Split('`')[0]}<{string.Join(",", t.GetGenericArguments().Select(a => a.Name))}>" : t.Name;
                s_CachedDescriptors.Add(new BlueprintSearchModel(t.Namespace?.Replace('.','/'), typeName).WithParameters((TypeSearchProvider.PARAM_TYPE_DATA, t)).WithSynonyms($"{t.Namespace}.{typeName}"));
            }
        }

        public override IEnumerable<BlueprintSearchModel> GetDescriptors()
        {
            return s_CachedDescriptors;
        }
    }

    public struct GenericDescriptor
    {
        public string Category;
        public string Name;
        public object UserData;
    }
    
    public class GenericSearchProvider : SearchProviderBase
    {
        public const string PARAM_USER_DATA = "Index";
        
        private readonly List<BlueprintSearchModel> _cachedDescriptors;

        public GenericSearchProvider(Action<BlueprintSearchModel, Vector2> onSpawnNode, List<GenericDescriptor> descriptors) : base(onSpawnNode)
        {
            _cachedDescriptors = new List<BlueprintSearchModel>(descriptors.Count);
            foreach (var desc in descriptors)
            {
                _cachedDescriptors.Add(new BlueprintSearchModel(desc.Category, desc.Name)
                    .WithParameters((PARAM_USER_DATA, desc.UserData)));
            }
        }

        public override IEnumerable<BlueprintSearchModel> GetDescriptors()
        {
            return _cachedDescriptors;
        }
    }
}