using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using Vapor;
using Vapor.Blueprints;
using Vapor.NewtonsoftConverters;
using VaporEditor.Inspector;

namespace VaporEditor.Blueprints
{
    public enum ChangeType
    {
        Added,
        Removed,
        Modified,
    }
    
    public class BlueprintClassGraphModel : IBlueprintGraphModel
    {
        public BlueprintGraphSo Graph { get; }
        public BlueprintMethodGraph Current { get; set; }
        

        public bool ParentIsBlueprint { get; private set; }
        public Type ParentType { get; }
        
        public BlueprintGraphSo ParentObject { get; private set; }
        
        public bool IsObsolete { get; set; }
        public string Namespace { get; set; }
        public List<string> Usings { get; } = new();
        
        public List<Type> ImplementedInterfaceTypes { get; }
        public Dictionary<string, BlueprintVariable> Variables { get; }
        public List<BlueprintMethodGraph> Methods { get; }

        #region Events
        public event Action<BlueprintClassGraphModel> ParentChanged;
        public event Action<BlueprintClassGraphModel, Type, ChangeType> InterfaceTypeChanged;
        public event Action<BlueprintClassGraphModel, BlueprintVariable, ChangeType, bool> VariableChanged;
        public event Action<BlueprintClassGraphModel, BlueprintMethodGraph, ChangeType, bool> MethodChanged;
        public event Action<BlueprintClassGraphModel, BlueprintMethodGraph> MethodOpened;
        public event Action<BlueprintClassGraphModel, BlueprintMethodGraph> MethodClosed;
        #endregion

        public static BlueprintClassGraphModel New(BlueprintGraphSo graph)
        {
            if (graph.ParentObject)
            {
                var parentGraph = Load(graph.ParentObject);
                var dto = new BlueprintClassGraphDto()
                {
                    IsObsolete = false,
                    Namespace = parentGraph.Namespace,
                    Usings = new List<string>(),
                    ParentType = null,
                    ParentObject = graph.ParentObject,
                    ImplementedInterfaceTypes = null,
                    Variables = new List<BlueprintVariableDto>(),
                    Methods = new List<BlueprintMethodGraphDto>(),
                };
                return new BlueprintClassGraphModel(graph, dto);
            }
            else
            {
                var parentType = Type.GetType(graph.ParentType);
                var dto =  new BlueprintClassGraphDto
                {
                    IsObsolete = false,
                    Namespace = parentType?.Namespace,
                    Usings = new List<string>(),
                    ParentType = parentType,
                    ParentObject = null,
                    ImplementedInterfaceTypes = null,
                    Variables = new List<BlueprintVariableDto>(),
                    Methods = new List<BlueprintMethodGraphDto>(),
                };
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
            ParentIsBlueprint = dto.ParentObject;
            if (!ParentIsBlueprint)
            {
                ParentType = dto.ParentType;
            }
            IsObsolete = dto.IsObsolete;
            Namespace = dto.Namespace;
            Usings.AddRange(dto.Usings ?? new List<string>());
            
            if(dto.ImplementedInterfaceTypes != null)
            {
                ImplementedInterfaceTypes = new List<Type>(dto.ImplementedInterfaceTypes);
            }
            ImplementedInterfaceTypes ??= new List<Type>();
            
            if(dto.Variables != null)
            {
                Variables = new Dictionary<string, BlueprintVariable>(dto.Variables.Count);
                foreach (var v in dto.Variables)
                {
                    Variables.Add(v.Id, new BlueprintVariable(v).WithClassGraph(this));
                }
            }
            else
            {
                Variables = new Dictionary<string, BlueprintVariable>();
                if (!ParentIsBlueprint)
                {
                    var abstractProperties = ReflectionUtility.GetAllPropertiesThatMatch(ParentType, pi => pi.DeclaringType.IsInterface || pi.GetMethod?.IsAbstract == true || pi.SetMethod?.IsAbstract == true, false, true);
                    foreach (var pi in abstractProperties)
                    {
                        var bpv = new BlueprintVariable(pi.Name, pi.PropertyType, VariableScopeType.Class, true);
                        Variables.Add(bpv.Id, bpv);
                    }
                }
            }

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
                if (ParentIsBlueprint)
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
                            Nodes = new List<BlueprintDesignNodeDto>(),
                            Wires = new List<BlueprintWireDto>(),
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
                Usings = new List<string>(Usings),
                ParentType = ParentType,
                ParentObject = ParentObject,
                ImplementedInterfaceTypes = new List<Type>(ImplementedInterfaceTypes),
                Variables = new List<BlueprintVariableDto>(Variables.Count),
                Methods = new List<BlueprintMethodGraphDto>(Methods.Count),
            };
            foreach (var v in Variables.Values)
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
            ParentIsBlueprint = ParentObject;
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
            InterfaceTypeChanged?.Invoke(this, newInterfaceType, ChangeType.Modified);
        }
        #endregion

        #region - Variables -
        public BlueprintVariable AddVariable(Type type, bool ignoreUndo = false)
        {
            var selection = Variables.Values.ToList().FindAll(v => v.DisplayName.StartsWith("Var_")).Select(v => v.DisplayName.Split('_')[1]);
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
            Variables.Add(addedVariable.Id, addedVariable);
            VariableChanged?.Invoke(this, addedVariable, ChangeType.Added, ignoreUndo);
            return addedVariable;
        }
        
        public BlueprintVariable AddVariable(BlueprintVariable variable, bool ignoreUndo)
        {
            Variables.Add(variable.Id, variable);
            VariableChanged?.Invoke(this, variable, ChangeType.Added, ignoreUndo);
            return variable;
        }

        public void RemoveVariable(BlueprintVariable variable, bool ignoreUndo = false)
        {
            if (Variables.Remove(variable.Id))
            {
                VariableChanged?.Invoke(this, variable, ChangeType.Removed, ignoreUndo);
            }
        }
        
        public void OnVariableUpdated(BlueprintVariable variable, bool ignoreUndo = false)
        {
            VariableChanged?.Invoke(this, variable, ChangeType.Modified, ignoreUndo);
        }
        #endregion

        #region - Methods -
        public BlueprintMethodGraph AddMethod(MethodInfo methodInfo, bool ignoreUndo = false)
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
                    Nodes = new List<BlueprintDesignNodeDto>(),
                    Wires = new List<BlueprintWireDto>(),
                };
                var graph = new BlueprintMethodGraph(this, dto);
                graph.Validate();
                Methods.Add(graph);
                MethodChanged?.Invoke(this, graph, ChangeType.Added, ignoreUndo);
                return graph;
            }
            else
            {
                var graph = CreateMethodGraphFromMethodInfo(methodInfo);
                graph.Validate();
                Methods.Add(graph);
                MethodChanged?.Invoke(this, graph, ChangeType.Added, ignoreUndo);
                return graph;
            }
        }
        
        public BlueprintMethodGraph AddMethod(BlueprintMethodGraph methodGraph, bool ignoreUndo)
        {
            Methods.Add(methodGraph);
            MethodChanged?.Invoke(this, methodGraph, ChangeType.Added, ignoreUndo);
            return methodGraph;
        }

        public BlueprintMethodGraph AddUnityMethod(string methodName, (Type, string)[] messageParameters, bool ignoreUndo = false)
        {
            var dto = new BlueprintMethodGraphDto
            {
                IsUnityOverride = true,
                MethodDeclaringType = ParentType,
                MethodName = methodName,
                MethodParameters = messageParameters?.Select(mp => $"{mp.Item1.AssemblyQualifiedName}|{mp.Item2}").ToArray(),
                Arguments = messageParameters?.Select((mp, i) => new BlueprintArgumentDto
                {
                    Type = mp.Item1,
                    ParameterName = mp.Item2,
                    DisplayName = ObjectNames.NicifyVariableName(mp.Item2),
                    IsOut = false,
                    IsRef = false,
                    IsReturn = false,
                    ParameterIndex = i
                }).ToList() ?? new List<BlueprintArgumentDto>(),
                Variables = new List<BlueprintVariableDto>(),
                Nodes = new List<BlueprintDesignNodeDto>(),
                Wires = new List<BlueprintWireDto>(),
            };
            var graph = new BlueprintMethodGraph(this, dto);
            graph.Validate();
            Methods.Add(graph);
            MethodChanged?.Invoke(this, graph, ChangeType.Added, ignoreUndo);
            return graph;
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
                Nodes = new List<BlueprintDesignNodeDto>(),
                Wires = new List<BlueprintWireDto>(),
            };
            var graph = new BlueprintMethodGraph(this, dto);
            return graph;
        }

        public void RemoveMethod(BlueprintMethodGraph methodGraph, bool ignoreUndo = false)
        {
            if (Methods.Remove(methodGraph))
            {
                MethodChanged?.Invoke(this, methodGraph, ChangeType.Removed, ignoreUndo);
            }
        }

        public void OnMethodUpdated(BlueprintMethodGraph methodGraph, bool ignoreUndo = false)
        {
            MethodChanged?.Invoke(this, methodGraph, ChangeType.Modified, ignoreUndo);
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
                arguments.Add(new BlueprintArgumentDto
                {
                    Type = paramType,
                    ParameterName = paramInfo.Name,
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

            foreach (var v in Variables.Values)
            {
                allTypes.Add(v.Type);
            }

            foreach (var mg in Methods)
            {
                foreach (var a in mg.Arguments)
                {
                    allTypes.Add(a.Type);
                }
                foreach (var a in mg.Variables.Values)
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