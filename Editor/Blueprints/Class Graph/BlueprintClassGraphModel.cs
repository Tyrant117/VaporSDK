using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using Vapor.NewtonsoftConverters;
using VaporEditor.Inspector;

namespace Vapor.Blueprints
{
    [Serializable]
    public struct BlueprintClassGraphDto
    {
        public bool IsObsolete;
        public string Namespace;
        public Type ParentType;
        public BlueprintGraphSo ParentObject;
        public List<Type> ImplementedInterfaceTypes;
        public List<BlueprintVariableDto> Variables;
        public List<BlueprintMethodGraphDto> Methods;

        public static BlueprintClassGraphDto New(Type parentType)
        {
            return new BlueprintClassGraphDto()
            {
                IsObsolete = false,
                Namespace = parentType.Namespace,
                ParentType = parentType,
                ParentObject = null,
                ImplementedInterfaceTypes = null,
                Variables = new List<BlueprintVariableDto>(),
                Methods = new List<BlueprintMethodGraphDto>(),
            };
        }
        
        public static BlueprintClassGraphDto New(BlueprintGraphSo parentObject)
        {
            var parentGraph = BlueprintClassGraphModel.Load(parentObject);
            return new BlueprintClassGraphDto()
            {
                IsObsolete = false,
                Namespace = parentGraph.Namespace,
                ParentType = null,
                ParentObject = parentObject,
                ImplementedInterfaceTypes = null,
                Variables = new List<BlueprintVariableDto>(),
                Methods = new List<BlueprintMethodGraphDto>(),
            };
        }

        public static BlueprintClassGraphDto Load(string graphJson)
        {
            return JsonConvert.DeserializeObject<BlueprintClassGraphDto>(graphJson, NewtonsoftUtility.SerializerSettings);
        }
    }

    public enum ChangeType
    {
        Added,
        Removed,
        Updated,
    }
    
    public class BlueprintClassGraphModel
    {
        public BlueprintGraphSo Graph { get; }
        public BlueprintMethodGraph Current { get; set; }
        

        public bool IsParentBlueprint { get; private set; }
        public Type ParentType { get; }
        
        public BlueprintGraphSo ParentObject { get; private set; }
        
        public bool IsObsolete { get; set; }
        public string Namespace { get; set; }
        
        public List<Type> ImplementedInterfaceTypes { get; }
        public List<BlueprintVariable> Variables { get; }
        public List<BlueprintMethodGraph> Methods { get; }

        #region Events
        public event Action<BlueprintClassGraphModel> ParentChanged;
        public event Action<BlueprintClassGraphModel, Type, ChangeType> InterfaceTypeChanged;
        public event Action<BlueprintClassGraphModel, BlueprintVariable, ChangeType> VariableChanged;
        public event Action<BlueprintClassGraphModel, BlueprintMethodGraph, ChangeType> MethodChanged;
        public event Action<BlueprintClassGraphModel, BlueprintMethodGraph> MethodOpened;
        public event Action<BlueprintClassGraphModel, BlueprintMethodGraph> MethodClosed;
        #endregion

        public static BlueprintClassGraphModel New(BlueprintGraphSo graph)
        {
            if (graph.ParentObject)
            {
                var dto = BlueprintClassGraphDto.New(graph.ParentObject);
                return new BlueprintClassGraphModel(graph, dto);
            }
            else
            {
                var parentType = Type.GetType(graph.ParentType);
                var dto = BlueprintClassGraphDto.New(parentType);
                return new BlueprintClassGraphModel(graph, dto);
            }
        }
        
        public static BlueprintClassGraphModel Load(BlueprintGraphSo graph)
        {
            var dto = BlueprintClassGraphDto.Load(graph.GraphJson);
            return new BlueprintClassGraphModel(graph, dto);
        }
        

        private BlueprintClassGraphModel(BlueprintGraphSo graph, BlueprintClassGraphDto dto)
        {
            Graph = graph;
            ParentObject = dto.ParentObject;
            IsParentBlueprint = dto.ParentObject;
            if (!IsParentBlueprint)
            {
                ParentType = dto.ParentType;
            }
            IsObsolete = dto.IsObsolete;
            Namespace = dto.Namespace;
            
            if(dto.ImplementedInterfaceTypes != null)
            {
                ImplementedInterfaceTypes = new List<Type>(dto.ImplementedInterfaceTypes);
            }
            ImplementedInterfaceTypes ??= new List<Type>();
            
            if(dto.Variables != null)
            {
                Variables = new List<BlueprintVariable>(dto.Variables.Count);
                foreach (var v in dto.Variables)
                {
                    Variables.Add(new BlueprintVariable(v).WithClassGraph(this));
                }
            }
            Variables ??= new List<BlueprintVariable>();

            if (dto.Methods != null)
            {
                Methods = new List<BlueprintMethodGraph>(dto.Methods.Count);
                foreach (var m in dto.Methods)
                {
                    Methods.Add(new BlueprintMethodGraph(this, m));
                }
            }
            else
            {
                Methods = new List<BlueprintMethodGraph>();
                if (IsParentBlueprint)
                {
                    var parentGraph = Load(ParentObject);
                    foreach (var parentMethod in parentGraph.Methods)
                    {
                        if (!parentMethod.IsAbstract)
                        {
                            continue;
                        }

                        var methodDto = new BlueprintMethodGraphDto
                        {
                            IsTypeOverride = false,
                            IsBlueprintOverride = true,
                            MethodDeclaringType = parentMethod.MethodDeclaringType,
                            MethodName = parentMethod.MethodName,
                            MethodParameters = null,
                            Arguments = new List<BlueprintArgumentDto>(parentMethod.Arguments.Select(arg => arg.Serialize())),
                            Variables = new List<BlueprintVariableDto>(),
                            Nodes = new List<BlueprintDesignNodeDto>()
                        };
                        var methodGraph = new BlueprintMethodGraph(this, methodDto);
                        methodGraph.Validate();
                        Methods.Add(methodGraph);
                    }
                }
                else
                {
                    var abstractMethods = ReflectionUtility.GetAllMethodsThatMatch(ParentType, mi => mi.IsAbstract, false, true);
                    foreach (var mi in abstractMethods)
                    {
                        var methodGraph = CreateMethodGraphFromMethodInfo(mi);
                        methodGraph.Validate();
                        Methods.Add(methodGraph);
                    }
                }
            }
        }
        
        public string Serialize()
        {
            BlueprintClassGraphDto graphDto = new BlueprintClassGraphDto
            {
                IsObsolete = IsObsolete,
                Namespace = Namespace,
                ParentType = ParentType,
                ParentObject = ParentObject,
                ImplementedInterfaceTypes = new List<Type>(ImplementedInterfaceTypes),
                Variables = new List<BlueprintVariableDto>(Variables.Count),
                Methods = new List<BlueprintMethodGraphDto>(Methods.Count),
            };
            foreach (var v in Variables)
            {
                graphDto.Variables.Add(v.Serialize());
            }
            foreach (var m in Methods)
            {
                graphDto.Methods.Add(m.Serialize());
            }
            
            return JsonConvert.SerializeObject(graphDto, NewtonsoftUtility.SerializerSettings);
        }

        public bool Validate()
        {
            bool valid = true;
            foreach (var m in Methods)
            {
                if (!valid)
                {
                    m.Validate();
                }
                else
                {
                    valid = m.Validate();
                }
            }
            return valid;
        }

        #region - Class Settings -

        public void SetParent(BlueprintGraphSo newParent)
        {
            ParentObject = newParent;
            IsParentBlueprint = ParentObject;
            ParentChanged?.Invoke(this);
        }

        #endregion

        #region - Interfaces -
        public void AddInterface(Type interfaceType)
        {
            if (ImplementedInterfaceTypes.Contains(interfaceType))
            {
                return;
            }
            
            ImplementedInterfaceTypes.Add(interfaceType);
            AddInterfaceMethods(interfaceType);
            InterfaceTypeChanged?.Invoke(this, interfaceType, ChangeType.Added);
        }

        private void AddInterfaceMethods(Type interfaceType)
        {
            if (interfaceType == null)
            {
                return;
            }
            
            var interfaceMethods = ReflectionUtility.GetAllMethodsThatMatch(interfaceType, mi => mi.DeclaringType!.IsInterface && mi.IsAbstract, false);
            foreach (var mi in interfaceMethods)
            {
                if (mi == null)
                {
                    // Skip
                    continue;
                }

                if (Methods.Any(m => m.MethodInfo != null && m.MethodInfo == mi))
                {
                    // Skip, Already Implemented
                    continue;
                }
                
                var methodGraph = CreateMethodGraphFromMethodInfo(mi);
                methodGraph.Validate();
                Methods.Add(methodGraph);
            }
        }

        public bool RemoveInterface(Type interfaceType)
        {
            if (ImplementedInterfaceTypes.Remove(interfaceType))
            {
                RemoveInterfaceMethods(interfaceType);
                InterfaceTypeChanged?.Invoke(this, interfaceType, ChangeType.Removed);
                return true;
            }
            return false;
        }

        public bool RemoveInterfaceAt(int index)
        {
            if (ImplementedInterfaceTypes.IsValidIndex(index))
            {
                return RemoveInterface(ImplementedInterfaceTypes[index]);
            }
            return false;
        }

        private void RemoveInterfaceMethods(Type interfaceType)
        {
            if (interfaceType == null)
            {
                return;
            }
            var interfaceMethods = ReflectionUtility.GetAllMethodsThatMatch(interfaceType, mi => mi.DeclaringType!.IsInterface && mi.IsAbstract, false);
            Methods.RemoveAll(m => m.MethodInfo != null && interfaceMethods.Contains(m.MethodInfo));
        }

        public void OnInterfaceUpdated(Type oldInterface, Type newInterfaceType)
        {
            var idx = ImplementedInterfaceTypes.IndexOf(oldInterface);
            if (idx == -1)
            {
                return;
            }
            
            RemoveInterfaceMethods(oldInterface);
            ImplementedInterfaceTypes[idx] = newInterfaceType;
            AddInterfaceMethods(newInterfaceType);
            InterfaceTypeChanged?.Invoke(this, newInterfaceType, ChangeType.Updated);
        }
        #endregion

        #region - Variables -
        public BlueprintVariable AddVariable(Type type)
        {
            var selection = Variables.FindAll(v => v.Name.StartsWith("Var_")).Select(v => v.Name.Split('_')[1]);
            int idx = 0;
            foreach (var s in selection)
            {
                if (!int.TryParse(s, out var sIdx))
                {
                    continue;
                }

                if (sIdx >= idx)
                {
                    idx = sIdx + 1;
                }
            }

            var addedVariable = new BlueprintVariable($"Var_{idx}", type, VariableScopeType.Class).WithClassGraph(this);
            Variables.Add(addedVariable);
            VariableChanged?.Invoke(this, addedVariable, ChangeType.Added);
            return addedVariable;
        }

        public void RemoveVariable(BlueprintVariable variable)
        {
            if (Variables.Remove(variable))
            {
                VariableChanged?.Invoke(this, variable, ChangeType.Removed);
            }
        }
        
        public void OnVariableUpdated(BlueprintVariable variable)
        {
            VariableChanged?.Invoke(this, variable, ChangeType.Updated);
        }
        #endregion

        #region - Methods -
        public BlueprintMethodGraph AddMethod(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                var selection = Methods.FindAll(v => v.MethodName.StartsWith("Method_")).Select(v => v.MethodName.Split('_')[1]);
                int idx = 0;
                foreach (var s in selection)
                {
                    if (!int.TryParse(s, out var sIdx))
                    {
                        continue;
                    }

                    if (sIdx >= idx)
                    {
                        idx = sIdx + 1;
                    }
                }
                
                var dto = new BlueprintMethodGraphDto
                {
                    IsTypeOverride = false,
                    MethodDeclaringType = ParentType,
                    MethodName = $"Method_{idx}",
                    MethodParameters = null,
                    Arguments = new List<BlueprintArgumentDto>(),
                    Variables = new List<BlueprintVariableDto>(),
                    Nodes = new List<BlueprintDesignNodeDto>()
                };
                var graph = new BlueprintMethodGraph(this, dto);
                graph.Validate();
                Methods.Add(graph);
                MethodChanged?.Invoke(this, graph, ChangeType.Added);
                return graph;
            }
            else
            {
                var graph = CreateMethodGraphFromMethodInfo(methodInfo);
                graph.Validate();
                Methods.Add(graph);
                MethodChanged?.Invoke(this, graph, ChangeType.Added);
                return graph;
            }
        }

        private BlueprintMethodGraph CreateMethodGraphFromMethodInfo(MethodInfo methodInfo)
        {
            CreateArgumentsFromMethodInfo(methodInfo, out var arguments);
            var dto = new BlueprintMethodGraphDto
            {
                IsTypeOverride = true,
                IsBlueprintOverride = false,
                MethodDeclaringType = methodInfo.DeclaringType,
                MethodName = methodInfo.Name,
                MethodParameters = methodInfo.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName).ToArray(),
                Arguments = arguments,
                Variables = new List<BlueprintVariableDto>(),
                Nodes = new List<BlueprintDesignNodeDto>()
            };
            var graph = new BlueprintMethodGraph(this, dto);
            return graph;
        }

        public void RemoveMethod(BlueprintMethodGraph methodGraph)
        {
            if (Methods.Remove(methodGraph))
            {
                MethodChanged?.Invoke(this, methodGraph, ChangeType.Removed);
            }
        }

        public void OnMethodUpdated(BlueprintMethodGraph methodGraph)
        {
            MethodChanged?.Invoke(this, methodGraph, ChangeType.Updated);
        }

        public void OpenMethodForEdit(BlueprintMethodGraph methodGraph)
        {
            var idx = Methods.IndexOf(methodGraph);
            if (idx == -1)
            {
                return;
            }

            if (Current != null)
            {
                MethodClosed?.Invoke(this, Current);
            }
            Current = Methods[idx];
            MethodOpened?.Invoke(this, Current);
        }

        public void CloseMethodForEdit(BlueprintMethodGraph methodGraph)
        {
            if (Current != methodGraph)
            {
                return;
            }

            MethodClosed?.Invoke(this, Current);
            Current = null;
        }
        #endregion

        private static void CreateArgumentsFromMethodInfo(MethodInfo methodInfo, out List<BlueprintArgumentDto> arguments)
        {
            arguments = new List<BlueprintArgumentDto>();
            var paramInfos = methodInfo.GetParameters();

            int idx = 0;
            foreach (var paramInfo in paramInfos)
            {
                var paramType = paramInfo.ParameterType;
                if (paramType.IsByRef)
                {
                    paramType = paramType.GetElementType();
                }
                arguments.Add(new BlueprintArgumentDto()
                {
                    Type = paramType,
                    DisplayName = ObjectNames.NicifyVariableName(paramInfo.Name),
                    ParameterIndex = idx,
                    IsReturn = paramInfo.IsRetval,
                    IsOut = paramInfo.ParameterType.IsByRef && paramInfo.IsOut,
                    IsRef = paramInfo.ParameterType.IsByRef && !paramInfo.IsOut,
                });
                idx++;
            }

            /*if (methodInfo.ReturnType != typeof(void))
            {
                var retParam = methodInfo.ReturnParameter;
                if (retParam is { IsRetval: true })
                {
                    // Out Ports
                    outputArguments.Add(new BlueprintArgumentDto
                    {
                        ParameterIndex = 0,
                        Type = retParam.ParameterType,
                        IsReturn = true,
                        IsOut = retParam.ParameterType.IsByRef && retParam.IsOut,
                        IsRef = retParam.ParameterType.IsByRef && !retParam.IsOut,
                    });
                }
            }

            foreach (var pi in paramInfos)
            {
                if (pi.IsOut)
                {
                    // Out Ports
                    var type = pi.ParameterType;
                    if (type.IsByRef)
                    {
                        type = type.GetElementType();
                    }

                    outputArguments.Add(new BlueprintVariableDto
                    {
                        Name = pi.Name,
                        Type = type,
                        VariableType = VariableType.OutArgument,
                    });
                }
                else
                {
                    // In Ports
                    var type = pi.ParameterType;
                    if (type.IsByRef)
                    {
                        type = type.GetElementType();
                    }
                    
                    inputArguments.Add(new BlueprintVariableDto
                    {
                        Name = pi.Name,
                        Type = type,
                        VariableType = VariableType.Argument,
                    });
                }
            }*/
        }
        
        public List<Type> GetAllTypes()
        {
            var allTypes = new List<Type>(300) { ParentType };

            foreach (var v in Variables)
            {
                allTypes.Add(v.Type);
            }

            foreach (var mg in Methods)
            {
                foreach (var a in mg.Arguments)
                {
                    allTypes.Add(a.Type);
                }
                foreach (var a in mg.Variables)
                {
                    allTypes.Add(a.Type);
                }

                foreach (var n in mg.Nodes.Values)
                {
                    foreach (var p in n.InputPins.Values)
                    {
                        allTypes.Add(p.Type);
                    }
                    foreach (var p in n.OutputPins.Values)
                    {
                        allTypes.Add(p.Type);
                    }
                }
            }
            
            return allTypes;
        }
    }
}