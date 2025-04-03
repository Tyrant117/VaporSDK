using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Vapor.NewtonsoftConverters;

namespace Vapor.Blueprints
{
    [Serializable]
    public struct BlueprintDesignGraphDto
    {
        public bool IsDeprecated;
        public bool IsAbstract;
        public string Namespace;
        // public VariableAccessModifier AccessModifier;
        public List<string> ImplementedInterfaces;
        public List<BlueprintVariableDto> Variables;
        public List<BlueprintMethodGraphDto> Methods;
    }
    
    public class BlueprintDesignGraph
    {
        public BlueprintGraphSo Graph { get; }
        public BlueprintMethodGraph Current { get; set; }

        public Type ClassType { get; }
        public bool IsDeprecated { get; set; }
        public bool IsAbstract { get; set; }
        public string Namespace { get; set; }
        // public VariableAccessModifier AccessModifier { get; set; }
        public List<string> ImplementedInterfaces { get; }
        public List<BlueprintVariable> Variables { get; }
        public List<BlueprintMethodGraph> Methods { get; }

        private int _variableCounter;

        public BlueprintDesignGraph(BlueprintGraphSo graph, BlueprintDesignGraphDto dto)
        {
            Graph = graph;
            ClassType = Type.GetType(Graph.AssemblyQualifiedTypeName);
            IsDeprecated = dto.IsDeprecated;
            IsAbstract = dto.IsAbstract;
            Namespace = dto.Namespace;
            // AccessModifier = dto.AccessModifier;
            if(dto.ImplementedInterfaces != null)
            {
                ImplementedInterfaces = new List<string>(dto.ImplementedInterfaces);
            }
            ImplementedInterfaces ??= new List<string>();
            
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
            Methods ??= new List<BlueprintMethodGraph>();
        }
        
        public string Serialize()
        {
            BlueprintDesignGraphDto graphDto = new BlueprintDesignGraphDto
            {
                IsDeprecated = IsDeprecated,
                IsAbstract = IsAbstract,
                Namespace = Namespace,
                // AccessModifier = AccessModifier,
                ImplementedInterfaces = new List<string>(ImplementedInterfaces),
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

        // public string Compile()
        // {
        //     BlueprintCompiledClassGraphDto graphDto = new BlueprintCompiledClassGraphDto
        //     {
        //         Variables = new List<BlueprintVariableDto>(Variables.Count),
        //         Methods = new List<BlueprintCompiledMethodGraphDto>(Methods.Count),
        //     };
        //     foreach (var v in Variables)
        //     {
        //         graphDto.Variables.Add(v.Serialize());
        //     }
        //     foreach (var m in Methods)
        //     {
        //         graphDto.Methods.Add(m.Compile());
        //     }
        //     
        //     return JsonConvert.SerializeObject(graphDto, NewtonsoftUtility.SerializerSettings);
        // }

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

            var tmp = new BlueprintVariable($"Var_{idx}", type, VariableType.Global).WithClassGraph(this);
            Variables.Add(tmp);
            return tmp;
        }

        public void AddMethod(string methodName, MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                var dto = new BlueprintMethodGraphDto
                {
                    IsOverride = false,
                    MethodDeclaringType = ClassType,
                    MethodName = methodName,
                    MethodParameters = null,
                    InputArguments = new List<BlueprintVariableDto>(),
                    OutputArguments = new List<BlueprintVariableDto>(),
                    TemporaryVariables = new List<BlueprintVariableDto>(),
                    Nodes = new List<BlueprintDesignNodeDto>()
                };
                var graph = new BlueprintMethodGraph(this, dto);
                graph.Validate();
                Methods.Add(graph);
            }
            else
            {
                CreateArgumentsFromMethodInfo(methodInfo, out var input, out var output);
                var dto = new BlueprintMethodGraphDto
                {
                    IsOverride = true,
                    MethodDeclaringType = methodInfo.DeclaringType,
                    MethodName = methodInfo.Name,
                    MethodParameters = methodInfo.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName).ToArray(),
                    InputArguments = input,
                    OutputArguments = output,
                    TemporaryVariables = new List<BlueprintVariableDto>(),
                    Nodes = new List<BlueprintDesignNodeDto>()
                };
                var graph = new BlueprintMethodGraph(this, dto);
                graph.Validate();
                Methods.Add(graph);
            }
        }

        private static void CreateArgumentsFromMethodInfo(MethodInfo methodInfo, out List<BlueprintVariableDto> inputArguments, out List<BlueprintVariableDto> outputArguments)
        {
            inputArguments = new List<BlueprintVariableDto>();
            outputArguments = new List<BlueprintVariableDto>();
            var paramInfos = methodInfo.GetParameters();
            
            if (methodInfo.ReturnType != typeof(void))
            {
                var retParam = methodInfo.ReturnParameter;
                if (retParam is { IsRetval: true })
                {
                    // Out Ports
                    outputArguments.Add(new BlueprintVariableDto
                    {
                        Name = PinNames.RETURN,
                        Type = retParam.ParameterType,
                        VariableType = VariableType.Return,
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
            }
        }

        public void SelectMethod(string name)
        {
            var idx = Methods.FindIndex(m => m.MethodName == name);
            if (idx != -1)
            {
                Current = Methods[idx];
            }
        }
        
        public List<Type> GetAllTypes()
        {
            var allTypes = new List<Type>(300) { ClassType };

            foreach (var v in Variables)
            {
                allTypes.Add(v.Type);
            }

            foreach (var mg in Methods)
            {
                foreach (var a in mg.InputArguments)
                {
                    allTypes.Add(a.Type);
                }
                foreach (var a in mg.OutputArguments)
                {
                    allTypes.Add(a.Type);
                }
                foreach (var a in mg.TemporaryVariables)
                {
                    allTypes.Add(a.Type);
                }

                foreach (var n in mg.Nodes)
                {
                    foreach (var p in n.InPorts.Values)
                    {
                        allTypes.Add(p.Type);
                    }
                    foreach (var p in n.OutPorts.Values)
                    {
                        allTypes.Add(p.Type);
                    }
                }
            }
            
            return allTypes;
        }
    }
}