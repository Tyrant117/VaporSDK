using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Vapor;
using Vapor.Blueprints;

namespace VaporEditor.Blueprints
{
    internal class ContextSearchProvider : SearchProviderBase
    {
        private readonly Type _contextType;
        private readonly bool _includeStatics;
        private readonly Action<BlueprintSearchModel, Vector2> _onSelect;
        private BlueprintPortView _pin;
        public ContextSearchProvider(Type contextType, bool includeStatics, Action<BlueprintSearchModel, Vector2> onSelect)
        {
            _contextType = contextType;
            _includeStatics = includeStatics;
            _onSelect = onSelect;
        }

        public ContextSearchProvider WithPin(BlueprintPortView pin)
        {
            _pin = pin;
            return this;
        }

        public override bool Select(BlueprintSearchModel model)
        {
            _onSelect?.Invoke(model, Position);
            return true;
        }

        public override IEnumerable<BlueprintSearchModel> GetDescriptors()
        {
            // string category = _contextType.IsGenericType ? $"{_contextType.Name.Split('`')[0]}<{string.Join(",", _contextType.GetGenericArguments().Select(a => a.Name))}>" : _contextType.Name;
            
            foreach (var blueprintSearchModel in GetIncludedInstances())
            {
                yield return blueprintSearchModel;
            }
            
            if (_includeStatics)
            {
                foreach (var blueprintSearchModel in GetIncludedStatics())
                {
                    yield return blueprintSearchModel;
                }
            }

            foreach (var blueprintSearchModel in BlueprintSearchLibrary.GetInternalDescriptors())
            {
                yield return blueprintSearchModel.WithPinView(_pin);
            }

            yield return new BlueprintSearchModel("Utilities/Flow Control", "Reroute", NodeType.Redirect)
                .WithUserData(_contextType)
                .WithPinView(_pin);
        }

        private IEnumerable<BlueprintSearchModel> GetIncludedInstances()
        {
            var fields = _contextType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var fieldInfo in fields)
            {
                yield return new BlueprintSearchModel($"Fields", $"Get {ObjectNames.NicifyVariableName(fieldInfo.Name)}", NodeType.MemberAccess)
                    .WithSynonyms(fieldInfo.Name)
                    .WithUserData(new MemberSearchData{ AccessType = VariableAccessType.Get, FieldInfo = fieldInfo})
                    .WithPinView(_pin);
                    
                yield return new BlueprintSearchModel($"Fields", $"Set {ObjectNames.NicifyVariableName(fieldInfo.Name)}", NodeType.MemberAccess)
                    .WithSynonyms(fieldInfo.Name)
                    .WithUserData(new MemberSearchData{ AccessType = VariableAccessType.Set, FieldInfo = fieldInfo})
                    .WithPinView(_pin);
            }
            
            var properties = _contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.GetIndexParameters().Length != 0)
                {
                    continue;
                }
                
                yield return new BlueprintSearchModel($"Properties", $"Get {ObjectNames.NicifyVariableName(propertyInfo.Name)}", NodeType.MemberAccess)
                    .WithSynonyms(propertyInfo.Name)
                    .WithUserData(new MemberSearchData{ AccessType = VariableAccessType.Get, PropertyInfo = propertyInfo})
                    .WithPinView(_pin);
                    
                if(propertyInfo.SetMethod != null && propertyInfo.SetMethod.IsPublic)
                {
                    yield return new BlueprintSearchModel($"Properties", $"Set {ObjectNames.NicifyVariableName(propertyInfo.Name)}", NodeType.MemberAccess)
                        .WithSynonyms(propertyInfo.Name)
                        .WithUserData(new MemberSearchData { AccessType = VariableAccessType.Set, PropertyInfo = propertyInfo })
                        .WithPinView(_pin);
                }
            }

            var methods = _contextType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var methodInfo in methods)
            {
                if (methodInfo.IsDefined(typeof(ObsoleteAttribute), false))
                {
                    continue;
                }

                if (methodInfo.IsSpecialName && methodInfo.Name.StartsWith("get_") && methodInfo.GetParameters().Length == 0)
                {
                    continue;
                }

                if (methodInfo.IsSpecialName && methodInfo.Name.StartsWith("set_") && methodInfo.GetParameters().Length == 0)
                {
                    continue;
                }

                var name = BlueprintEditorUtility.FormatMethodName(methodInfo);
                yield return new BlueprintSearchModel($"Methods", $"{name}", NodeType.Method)
                    .WithSynonyms(name)
                    .WithUserData(methodInfo)
                    .WithPinView(_pin);
            }
        }
        
        private IEnumerable<BlueprintSearchModel> GetIncludedStatics()
        {
            var staticFields = _contextType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var fieldInfo in staticFields)
            {
                yield return new BlueprintSearchModel($"Static Fields", $"Get {ObjectNames.NicifyVariableName(fieldInfo.Name)}", NodeType.MemberAccess)
                    .WithSynonyms(fieldInfo.Name)
                    .WithUserData(new MemberSearchData{ AccessType = VariableAccessType.Get, FieldInfo = fieldInfo})
                    .WithPinView(_pin);
                    
                yield return new BlueprintSearchModel($"Static Fields", $"Set {ObjectNames.NicifyVariableName(fieldInfo.Name)}", NodeType.MemberAccess)
                    .WithSynonyms(fieldInfo.Name)
                    .WithUserData(new MemberSearchData{ AccessType = VariableAccessType.Set, FieldInfo = fieldInfo})
                    .WithPinView(_pin);
            }
            
            var staticMethods = _contextType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (var methodInfo in staticMethods)
            {
                if (methodInfo.IsDefined(typeof(ObsoleteAttribute), false))
                {
                    continue;
                }
                
                var name = BlueprintEditorUtility.FormatMethodName(methodInfo);
                yield return new BlueprintSearchModel($"Static Methods", $"{name}", NodeType.Method)
                    .WithSynonyms(name)
                    .WithUserData(methodInfo)
                    .WithPinView(_pin);
            }
        }
    }

    internal class DefaultSearchProvider : SearchProviderBase
    {
        private BlueprintClassGraphModel _graphModel;
        private readonly IEnumerable<BlueprintSearchModel> _filteredDescriptors;
        private readonly Action<BlueprintSearchModel, Vector2> _onSelect;

        public DefaultSearchProvider(Action<BlueprintSearchModel, Vector2> onSelect, List<BlueprintSearchModel> searchModels, Func<BlueprintSearchModel, bool> filter = null)
        {
            _onSelect = onSelect;
            _filteredDescriptors = filter != null ? searchModels.Where(filter) : searchModels;
        }

        public override bool Select(BlueprintSearchModel model)
        {
            if (model.IsStaticAccessor)
            {
                SearchWindow.UpdateProvider(new TypeOnlySearchProvider((Type)model.UserData, true, false, _onSelect).WithGraph(_graphModel), false, false);
                return false;
            }
            else
            {
                _onSelect?.Invoke(model, Position);
                return true;
            }
        }

        public DefaultSearchProvider WithGraph(BlueprintClassGraphModel graphModel)
        {
            _graphModel = graphModel;
            return this;
        }

        public override IEnumerable<BlueprintSearchModel> GetDescriptors()
        {
            foreach (var model in ConstructGetterSetterNodes())
            {
                yield return model.WithGraph(_graphModel);
            }
            
            foreach (var model in BlueprintSearchLibrary.GetBlueprintLibraries())
            {
                yield return model.WithGraph(_graphModel);
            }

            foreach (var model in BlueprintSearchLibrary.GetInternalDescriptors())
            {
                yield return model.WithGraph(_graphModel);
            }
            
            yield return new BlueprintSearchModel("Utilities/Flow Control", "Return", NodeType.Return)
                .WithGraph(_graphModel);

            foreach (var model in _filteredDescriptors)
            {
                yield return model.WithGraph(_graphModel);
            }
            
        }
        
        private IEnumerable<BlueprintSearchModel> ConstructGetterSetterNodes()
        {
            foreach (var classVar in _graphModel.Variables.Values)
            {
                
                yield return new BlueprintSearchModel("Variables/Global", $"Get {classVar.DisplayName}", NodeType.MemberAccess)
                    .WithUserData(new MemberSearchData{ AccessType = VariableAccessType.Get, ScopeType = VariableScopeType.Class, Id = classVar.Id})
                    .WithGraph(_graphModel);
                
                yield return new BlueprintSearchModel("Variables/Global", $"Set {classVar.DisplayName}", NodeType.MemberAccess)
                    .WithUserData(new MemberSearchData{ AccessType = VariableAccessType.Set, ScopeType = VariableScopeType.Class, Id = classVar.Id})
                    .WithGraph(_graphModel);
            }

            foreach (var methodVar in _graphModel.Current.Variables.Values)
            {
                yield return new BlueprintSearchModel("Variables/Local", $"Get {methodVar.DisplayName}", NodeType.MemberAccess)
                    .WithUserData(new MemberSearchData{ AccessType = VariableAccessType.Get, ScopeType = VariableScopeType.Method, Id = methodVar.Id})
                    .WithGraph(_graphModel);
                
                yield return new BlueprintSearchModel("Variables/Local", $"Set {methodVar.DisplayName}", NodeType.MemberAccess)
                    .WithUserData(new MemberSearchData{ AccessType = VariableAccessType.Set, ScopeType = VariableScopeType.Method, Id = methodVar.Id})
                    .WithGraph(_graphModel);
            }
        }
    }

    internal class TypeOnlySearchProvider : SearchProviderBase
    {
        private readonly Type _contextType;
        private readonly bool _includeStatics;
        private readonly bool _includeInstance;
        private BlueprintClassGraphModel _graphModel;
        private readonly Action<BlueprintSearchModel, Vector2> _onSelect;
        
        public TypeOnlySearchProvider(Type contextType, bool includeStatics, bool includeInstance, Action<BlueprintSearchModel, Vector2> onSelect)
        {
            _contextType = contextType;
            _includeStatics = includeStatics;
            _includeInstance = includeInstance;
            _onSelect = onSelect;
        }
        
        public TypeOnlySearchProvider WithGraph(BlueprintClassGraphModel graphModel)
        {
            _graphModel = graphModel;
            return this;
        }
        
        public override bool Select(BlueprintSearchModel model)
        {
            _onSelect?.Invoke(model, Position);
            return true;
        }

        public override IEnumerable<BlueprintSearchModel> GetDescriptors()
        {
            string conSignature = null;
            var constructors = _contextType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (_contextType == typeof(string) || _contextType.IsPrimitive || constructors.Length == 0)
            {
                conSignature = "Default(T)";
            }
            else
            {
                conSignature = BlueprintEditorUtility.FormatConstructorSignature(constructors[0]);
            }

            ConstructorSearchData sd = new ConstructorSearchData
            {
                TypeToConstruct = _contextType,
                ConstructorSignature = conSignature,
                IsArray = false,
            };
            
            yield return new BlueprintSearchModel( string.Empty, $"Construct {BlueprintEditorUtility.FormatTypeName(_contextType)}", NodeType.Constructor)
                .WithSynonyms("New", "Make")
                .WithUserData(sd)
                .WithGraph(_graphModel);
            
            if (_includeInstance)
            {
                foreach (var blueprintSearchModel in GetIncludedInstances())
                {
                    yield return blueprintSearchModel;
                }
            }

            if (_includeStatics)
            {
                foreach (var blueprintSearchModel in GetIncludedStatics())
                {
                    yield return blueprintSearchModel;
                }
            }
        }

        private IEnumerable<BlueprintSearchModel> GetIncludedInstances()
        {
            var fields = _contextType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var fieldInfo in fields)
            {
                yield return new BlueprintSearchModel($"Fields", $"Get {ObjectNames.NicifyVariableName(fieldInfo.Name)}", NodeType.MemberAccess)
                    .WithSynonyms(fieldInfo.Name)
                    .WithUserData(new MemberSearchData { AccessType = VariableAccessType.Get, FieldInfo = fieldInfo })
                    .WithGraph(_graphModel);

                yield return new BlueprintSearchModel($"Fields", $"Set {ObjectNames.NicifyVariableName(fieldInfo.Name)}", NodeType.MemberAccess)
                    .WithSynonyms(fieldInfo.Name)
                    .WithUserData(new MemberSearchData { AccessType = VariableAccessType.Set, FieldInfo = fieldInfo })
                    .WithGraph(_graphModel);
            }
            
            var properties = _contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.GetIndexParameters().Length != 0)
                {
                    continue;
                }
                yield return new BlueprintSearchModel($"Properties", $"Get {ObjectNames.NicifyVariableName(propertyInfo.Name)}", NodeType.MemberAccess)
                    .WithSynonyms(propertyInfo.Name)
                    .WithUserData(new MemberSearchData{ AccessType = VariableAccessType.Get, PropertyInfo = propertyInfo})
                    .WithGraph(_graphModel);
                    
                if(propertyInfo.SetMethod != null && propertyInfo.SetMethod.IsPublic)
                {
                    yield return new BlueprintSearchModel($"Properties", $"Set {ObjectNames.NicifyVariableName(propertyInfo.Name)}", NodeType.MemberAccess)
                        .WithSynonyms(propertyInfo.Name)
                        .WithUserData(new MemberSearchData { AccessType = VariableAccessType.Set, PropertyInfo = propertyInfo })
                        .WithGraph(_graphModel);
                }
            }

            var methods = _contextType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var methodInfo in methods)
            {
                if (methodInfo.IsDefined(typeof(ObsoleteAttribute), false))
                {
                    continue;
                }
                
                if (methodInfo.IsSpecialName && methodInfo.Name.StartsWith("get_") && methodInfo.GetParameters().Length == 0)
                {
                    continue;
                }

                if (methodInfo.IsSpecialName && methodInfo.Name.StartsWith("set_") && methodInfo.GetParameters().Length == 0)
                {
                    continue;
                }

                var name = BlueprintEditorUtility.FormatMethodName(methodInfo);
                yield return new BlueprintSearchModel($"Methods", $"{name}", NodeType.Method)
                    .WithSynonyms(name)
                    .WithUserData(methodInfo)
                    .WithGraph(_graphModel);
            }
        }

        private IEnumerable<BlueprintSearchModel> GetIncludedStatics()
        {
            var staticFields = _contextType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var fieldInfo in staticFields)
            {
                yield return new BlueprintSearchModel($"Static Fields", $"Get {ObjectNames.NicifyVariableName(fieldInfo.Name)}", NodeType.MemberAccess)
                    .WithSynonyms(fieldInfo.Name)
                    .WithUserData(new MemberSearchData { AccessType = VariableAccessType.Get, FieldInfo = fieldInfo })
                    .WithGraph(_graphModel);

                yield return new BlueprintSearchModel($"Static Fields", $"Set {ObjectNames.NicifyVariableName(fieldInfo.Name)}", NodeType.MemberAccess)
                    .WithSynonyms(fieldInfo.Name)
                    .WithUserData(new MemberSearchData { AccessType = VariableAccessType.Set, FieldInfo = fieldInfo })
                    .WithGraph(_graphModel);
            }
            
            var staticMethods = _contextType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (var methodInfo in staticMethods)
            {
                if (methodInfo.IsDefined(typeof(ObsoleteAttribute), false))
                {
                    continue;
                }
                

                var name = BlueprintEditorUtility.FormatMethodName(methodInfo);
                yield return new BlueprintSearchModel($"Static Methods", $"{name}", NodeType.Method)
                    .WithSynonyms(name)
                    .WithUserData(methodInfo)
                    .WithGraph(_graphModel);
            }
        }
    }

    internal class MemberOnlySearchProvider : SearchProviderBase
    {
        private readonly BlueprintVariable _variable;
        private readonly Action<BlueprintSearchModel, Vector2> _onSelect;
        private readonly BlueprintClassGraphModel _graphModel;

        public MemberOnlySearchProvider(BlueprintVariable variable, Action<BlueprintSearchModel, Vector2> onSelect, BlueprintClassGraphModel graphModel)
        {
            _variable = variable;
            _onSelect = onSelect;
            _graphModel = graphModel;
        }
        
        public override bool Select(BlueprintSearchModel model)
        {
            _onSelect?.Invoke(model, Position);
            return true;
        }

        public override IEnumerable<BlueprintSearchModel> GetDescriptors()
        {
            yield return new BlueprintSearchModel(string.Empty, $"Set {_variable.DisplayName}", NodeType.MemberAccess)
                .WithUserData(new MemberSearchData { AccessType = VariableAccessType.Set, ScopeType = _variable.Scope, Id = _variable.Id })
                .WithGraph(_graphModel);
            
            yield return new BlueprintSearchModel(string.Empty, $"Get {_variable.DisplayName}", NodeType.MemberAccess)
                .WithUserData(new MemberSearchData { AccessType = VariableAccessType.Get, ScopeType = _variable.Scope, Id = _variable.Id })
                .WithGraph(_graphModel);
        }
    }
}