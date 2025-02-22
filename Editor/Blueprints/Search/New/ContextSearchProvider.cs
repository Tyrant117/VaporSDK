using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Vapor.Blueprints;

namespace VaporEditor.Blueprints
{
    internal class ContextSearchProvider : SearchProviderBase
    {
        private readonly Type _contextType;
        private readonly bool _includeStatics;
        private BlueprintEditorPort _pin;
        public ContextSearchProvider(Type contextType, bool includeStatics, Action<BlueprintSearchModel, Vector2> onSpawnNode) : base(onSpawnNode)
        {
            _contextType = contextType;
            _includeStatics = includeStatics;
        }

        public ContextSearchProvider WithPin(BlueprintEditorPort pin)
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
                yield return new BlueprintSearchModel($"{category}/Fields", $"Get {ObjectNames.NicifyVariableName(fieldInfo.Name)}", typeof(FieldGetterNodeType))
                    .WithSynonyms(fieldInfo.Name)
                    .WithParameters((INodeType.FIELD_INFO_PARAM, fieldInfo))
                    .WithPin(_pin);
                    
                yield return new BlueprintSearchModel($"{category}/Fields", $"Set {ObjectNames.NicifyVariableName(fieldInfo.Name)}", typeof(FieldSetterNodeType))
                    .WithSynonyms(fieldInfo.Name)
                    .WithParameters((INodeType.FIELD_INFO_PARAM, fieldInfo))
                    .WithPin(_pin);
            }

            var methods = _contextType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var methodInfo in methods)
            {
                var name = methodInfo.IsGenericMethod ? $"{methodInfo.Name.Split('`')[0]}<{string.Join(",", methodInfo.GetGenericArguments().Select(a => a.Name))}>" : methodInfo.Name;
                name = methodInfo.IsSpecialName ? BlueprintNodeDataModelUtility.ToTitleCase(name) : name;
                var parameters = methodInfo.GetParameters();
                string paramNames = parameters.Length > 0
                    ? parameters.Select(pi => pi.ParameterType.IsGenericType
                            ? $"{pi.ParameterType.Name.Split('`')[0]}<{string.Join(",", pi.ParameterType.GetGenericArguments().Select(a => a.Name))}>"
                            : pi.ParameterType.Name)
                        .Aggregate((a, b) => a + ", " + b)
                    : string.Empty;

                yield return new BlueprintSearchModel($"{category}/Methods", $"{name}({paramNames})", typeof(MethodNodeType))
                    .WithSynonyms(name)
                    .WithParameters((INodeType.METHOD_INFO_PARAM, methodInfo))
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

            yield return new BlueprintSearchModel("Utilities/Flow Control", "Reroute", typeof(RerouteNodeType))
                .WithParameters((INodeType.CONNECTION_TYPE_PARAM, _contextType))
                .WithPin(_pin);
        }

        private IEnumerable<BlueprintSearchModel> GetIncludedStatics(string category)
        {
            var staticFields = _contextType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var fieldInfo in staticFields)
            {
                yield return new BlueprintSearchModel($"{category}/Static Fields", $"Get {ObjectNames.NicifyVariableName(fieldInfo.Name)}", typeof(FieldGetterNodeType))
                    .WithSynonyms(fieldInfo.Name)
                    .WithParameters((INodeType.FIELD_INFO_PARAM, fieldInfo))
                    .WithPin(_pin);
                    
                yield return new BlueprintSearchModel($"{category}/Static Fields", $"Set {ObjectNames.NicifyVariableName(fieldInfo.Name)}", typeof(FieldSetterNodeType))
                    .WithSynonyms(fieldInfo.Name)
                    .WithParameters((INodeType.FIELD_INFO_PARAM, fieldInfo))
                    .WithPin(_pin);
            }
            
            var staticMethods = _contextType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (var methodInfo in staticMethods)
            {
                var name = methodInfo.IsGenericMethod ? $"{methodInfo.Name.Split('`')[0]}<{string.Join(",", methodInfo.GetGenericArguments().Select(a => a.Name))}>" : methodInfo.Name;
                name = methodInfo.IsSpecialName ? BlueprintNodeDataModelUtility.ToTitleCase(name) : name;
                var parameters = methodInfo.GetParameters();
                string paramNames = parameters.Length > 0
                    ? parameters.Select(pi => pi.ParameterType.IsGenericType
                            ? $"{pi.ParameterType.Name.Split('`')[0]}<{string.Join(",", pi.ParameterType.GetGenericArguments().Select(a => a.Name))}>"
                            : pi.ParameterType.Name)
                        .Aggregate((a, b) => a + ", " + b)
                    : string.Empty;

                yield return new BlueprintSearchModel($"{category}/Static Methods", $"{name}({paramNames})", typeof(MethodNodeType))
                    .WithSynonyms(name)
                    .WithParameters((INodeType.METHOD_INFO_PARAM, methodInfo))
                    .WithPin(_pin);
            }
        }
    }

    internal class DefaultSearchProvider : SearchProviderBase
    {
        private BlueprintGraphSo _graph;

        public DefaultSearchProvider(Action<BlueprintSearchModel, Vector2> onSpawnNode) : base(onSpawnNode)
        {
        }

        public DefaultSearchProvider WithGraph(BlueprintGraphSo graph)
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
            
            yield return new BlueprintSearchModel("Utilities/Flow Control", "Return", typeof(ReturnNodeType))
                .WithGraph(_graph);
            
        }
        
        private IEnumerable<BlueprintSearchModel> ConstructGetterSetterNodes()
        {
            foreach (var temp in _graph.TempData)
            {
                yield return new BlueprintSearchModel("Variables", $"Get {temp.FieldName}", typeof(TemporaryDataGetterNodeType))
                    .WithParameters((INodeType.NAME_DATA_PARAM, temp.FieldName))
                    .WithGraph(_graph);
                
                yield return new BlueprintSearchModel("Variables", $"Set {temp.FieldName}", typeof(TemporaryDataSetterNodeType))
                    .WithParameters((INodeType.NAME_DATA_PARAM, temp.FieldName))
                    .WithGraph(_graph);
            }
        }
    }
}