using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using Vapor.Inspector;
using Vapor.Keys;
using Vapor.NewtonsoftConverters;

namespace Vapor.Blueprints
{
    [Serializable]
    public struct BlueprintDesignGraphDto
    {
        public List<BlueprintVariableDto> Variables;
        public List<BlueprintMethodGraphDto> Methods;
    }
    
    [Serializable]
    public struct BlueprintMethodGraphDto
    {
        public bool IsOverride;
        public string MethodName;
        public string[] MethodParameters;
        public List<BlueprintVariableDto> InputArguments;
        public List<BlueprintVariableDto> OutputArguments;
        public List<BlueprintVariableDto> TemporaryVariables;
        public List<BlueprintDesignNodeDto> Nodes;
    }

    [Serializable]
    public struct BlueprintCompiledClassGraphDto
    {
        public List<BlueprintVariableDto> Variables;
        public List<BlueprintCompiledMethodGraphDto> Methods;
    }
    
    [Serializable]
    public struct BlueprintCompiledMethodGraphDto
    {
        public List<BlueprintVariableDto> InputArguments;
        public List<BlueprintVariableDto> OutputArguments;
        public List<BlueprintVariableDto> TemporaryVariables;
        public List<BlueprintCompiledNodeDto> Nodes;
    }
    
    public class BlueprintDesignGraph
    {
        public BlueprintGraphSo Graph { get; }
        public BlueprintMethodGraph Current { get; set; }

        public Type ClassType { get; }
        public List<BlueprintVariable> Variables { get; }
        public List<BlueprintMethodGraph> Methods { get; }

        public BlueprintDesignGraph(BlueprintGraphSo graph, BlueprintDesignGraphDto dto)
        {
            Graph = graph;
            ClassType = Type.GetType(Graph.AssemblyQualifiedTypeName);
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

        public string Compile()
        {
            BlueprintCompiledClassGraphDto graphDto = new BlueprintCompiledClassGraphDto
            {
                Variables = new List<BlueprintVariableDto>(Variables.Count),
                Methods = new List<BlueprintCompiledMethodGraphDto>(Methods.Count),
            };
            foreach (var v in Variables)
            {
                graphDto.Variables.Add(v.Serialize());
            }
            foreach (var m in Methods)
            {
                graphDto.Methods.Add(m.Compile());
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

        public void AddVariable(string labelText, Type type)
        {
            var tmp = new BlueprintVariable(labelText, type, BlueprintVariable.VariableType.Global).WithClassGraph(this);
            Variables.Add(tmp);
        }

        public void AddMethod(string methodName, MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                var dto = new BlueprintMethodGraphDto
                {
                    IsOverride = false,
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
                         VariableType = BlueprintVariable.VariableType.Return,
                         Value = retParam.DefaultValue,
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
                        VariableType = BlueprintVariable.VariableType.Argument,
                        Value = pi.DefaultValue,
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
                        VariableType = BlueprintVariable.VariableType.Argument,
                        Value = pi.DefaultValue,
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
    }

    public class BlueprintMethodGraph
    {
        public BlueprintDesignGraph ClassGraph { get; }
        public bool IsOverride { get; }
        public string MethodName { get; set; }
        public string[] MethodParameters { get; }
        public MethodInfo MethodInfo { get; }

        public List<BlueprintVariable> InputArguments { get; }
        public List<BlueprintVariable> OutputArguments { get; }
        public List<BlueprintVariable> TemporaryVariables { get; }
        public List<BlueprintDesignNode> Nodes { get; }

        public BlueprintMethodGraph(BlueprintDesignGraph graph, BlueprintMethodGraphDto dto)
        {
            ClassGraph = graph;
            MethodName = dto.MethodName;
            if (dto.IsOverride)
            {
                IsOverride = true;
                MethodParameters = dto.MethodParameters;
                MethodInfo = GetMethodInfo(ClassGraph.ClassType, MethodName, MethodParameters);
            }
            if(dto.InputArguments != null)
            {
                InputArguments = new List<BlueprintVariable>(dto.InputArguments.Count);
                foreach (var arg in dto.InputArguments)
                {
                    InputArguments.Add(new BlueprintVariable(arg).WithMethodGraph(this));
                }
            }
            InputArguments ??= new List<BlueprintVariable>();
            
            if(dto.OutputArguments != null)
            {
                OutputArguments = new List<BlueprintVariable>(dto.OutputArguments.Count);
                foreach (var arg in dto.OutputArguments)
                {
                    OutputArguments.Add(new BlueprintVariable(arg).WithMethodGraph(this));
                }
            }
            OutputArguments ??= new List<BlueprintVariable>();
            
            if(dto.TemporaryVariables != null)
            {
                TemporaryVariables = new List<BlueprintVariable>(dto.TemporaryVariables.Count);
                foreach (var v in dto.TemporaryVariables)
                {
                    TemporaryVariables.Add(new BlueprintVariable(v).WithMethodGraph(this));
                }
            }
            TemporaryVariables ??= new List<BlueprintVariable>();
            
            if(dto.Nodes != null)
            {
                Nodes = new List<BlueprintDesignNode>(dto.Nodes.Count);
                foreach (var nodeDto in dto.Nodes)
                {
                    Nodes.Add(new BlueprintDesignNode(nodeDto, this));
                }
            }
            Nodes ??= new List<BlueprintDesignNode>();
        }
        
        public BlueprintMethodGraphDto Serialize()
        {
            BlueprintMethodGraphDto graphDto = new BlueprintMethodGraphDto
            {
                IsOverride = IsOverride,
                MethodName = MethodName,
                MethodParameters = MethodParameters,
                Nodes = new List<BlueprintDesignNodeDto>(Nodes.Count),
                InputArguments = new List<BlueprintVariableDto>(InputArguments.Count),
                OutputArguments = new List<BlueprintVariableDto>(OutputArguments.Count),
                TemporaryVariables = new List<BlueprintVariableDto>(TemporaryVariables.Count),
            };
            foreach (var arg in InputArguments)
            {
                graphDto.InputArguments.Add(arg.Serialize());
            }
            foreach (var arg in OutputArguments)
            {
                graphDto.OutputArguments.Add(arg.Serialize());
            }
            foreach (var v in TemporaryVariables)
            {
                graphDto.TemporaryVariables.Add(v.Serialize());
            }
            foreach (var node in Nodes)
            {
                graphDto.Nodes.Add(node.Serialize());
            }

            return graphDto; //JsonConvert.SerializeObject(graphDto, NewtonsoftUtility.SerializerSettings);
        }

        public BlueprintCompiledMethodGraphDto Compile()
        {
            BlueprintCompiledMethodGraphDto graphDto = new BlueprintCompiledMethodGraphDto
            {
                Nodes = new List<BlueprintCompiledNodeDto>(Nodes.Count),
                InputArguments = new List<BlueprintVariableDto>(InputArguments.Count),
                OutputArguments = new List<BlueprintVariableDto>(OutputArguments.Count),
                TemporaryVariables = new List<BlueprintVariableDto>(TemporaryVariables.Count),
            };
            foreach (var arg in InputArguments)
            {
                graphDto.InputArguments.Add(arg.Serialize());
            }
            foreach (var arg in OutputArguments)
            {
                graphDto.OutputArguments.Add(arg.Serialize());
            }
            foreach (var v in TemporaryVariables)
            {
                graphDto.TemporaryVariables.Add(v.Serialize());
            }
            foreach (var node in Nodes)
            {
                graphDto.Nodes.Add(node.Compile());
            }

            return graphDto; //JsonConvert.SerializeObject(graphDto, NewtonsoftUtility.SerializerSettings);
        }

        public bool Validate()
        {
            bool valid = true;
            bool eNew = false;
            bool rNew = false;
            var entry = Nodes.FirstOrDefault(x => x.Type == typeof(EntryNodeType));
            if (entry == null)
            {
                var nodeType = new EntryNodeType();
                var node = nodeType.CreateDesignNode(Vector2.zero, new List<(string, object)> { (INodeType.GRAPH_PARAM, this) });
                Nodes.Insert(0, node);
                valid = false;
                eNew = true;
            }
            
            var ret = Nodes.FirstOrDefault(x => x.Type == typeof(ReturnNodeType));
            if (ret == null)
            {
                var nodeType = new ReturnNodeType();
                var node = nodeType.CreateDesignNode(Vector2.zero + Vector2.right * 200, new List<(string, object)> { (INodeType.GRAPH_PARAM, this) });
                Nodes.Insert(1, node);
                valid = false;
                rNew = true;
            }

            if (eNew && rNew)
            {
                var leftPort = new BlueprintPinReference(PinNames.EXECUTE_OUT, Nodes[0].Guid, true);
                var rightPort = new BlueprintPinReference(PinNames.EXECUTE_IN, Nodes[1].Guid, true);
                var wireRef = new BlueprintWireReference(leftPort, rightPort);
                Nodes[0].OutputWires.Add(wireRef);
                Nodes[1].InputWires.Add(wireRef);
            }
            return valid;
        }
        
        public void AddInputArgument(string labelText, Type type)
        {
            var tmp = new BlueprintVariable(labelText, type, BlueprintVariable.VariableType.Argument).WithMethodGraph(this);
            InputArguments.Add(tmp);
        }
        
        public void AddOutputArgument(string labelText, Type type)
        {
            var tmp = new BlueprintVariable(labelText, type, BlueprintVariable.VariableType.Argument).WithMethodGraph(this);
            OutputArguments.Add(tmp);
        }

        public void AddTemporaryVariable(string labelText, Type type)
        {
            var tmp = new BlueprintVariable(labelText, type, BlueprintVariable.VariableType.Local).WithMethodGraph(this);
            TemporaryVariables.Add(tmp);
        }
        
        private static MethodInfo GetMethodInfo(Type declaringType, string methodName, string[] parameterTypes)
        {
            if (declaringType == null)
            {
                return null;
            }

            if (parameterTypes.Length > 0)
            {
                var paramTypes = new Type[parameterTypes.Length];
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    paramTypes[i] = Type.GetType(parameterTypes[i]);
                }

                return declaringType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, paramTypes, null);
            }
            else
            {
                return declaringType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            }
        }
    }
    
    [DatabaseKeyValuePair, KeyOptions(includeNone: false, category: "Graphs")]
    public class BlueprintGraphSo : NamedKeySo, IDatabaseInitialize
    {
        public enum BlueprintGraphType
        {
            BehaviourGraph,
            ClassGraph,
        }

        public BlueprintGraphType GraphType;
        public string AssemblyQualifiedTypeName;
        public string DesignGraphJson;
        [HideInInspector] public string CompiledGraphJson;
        
        
        [Button]
        private void ResetGraph()
        {
            DesignGraphJson = string.Empty;
            CompiledGraphJson = string.Empty;
        }
        
        public IBlueprintGraph Graph { get; set; }
        public BlueprintDesignGraph DesignGraph { get; set; }

        public void InitializedInDatabase()
        {
            // Validate();
            Graph = new BlueprintFunctionGraph(this);
            RuntimeDataStore<IBlueprintGraph>.InitDatabase(RuntimeDatabase<BlueprintGraphSo>.Count);
        }

        public void PostInitializedInDatabase()
        {
            Debug.Log("Post Initialized Graph: " + Key);
            RuntimeDataStore<IBlueprintGraph>.Add(Key, Graph);
        }
        
        // public void Validate()
        // {
        //     var entry = BlueprintNodes.FirstOrDefault(x => x.NodeType == BlueprintNodeType.Entry);
        //     if (entry == null)
        //     {
        //         entry = BlueprintNodeDataModelUtility.CreateOrUpdateEntryNode(null, InputParameters);
        //         BlueprintNodes.Insert(0, entry);
        //     }
        //     else
        //     {
        //         BlueprintNodeDataModelUtility.CreateOrUpdateEntryNode(entry, InputParameters);
        //     }
        //     
        //     var ret = BlueprintNodes.FirstOrDefault(x => x.NodeType == BlueprintNodeType.Return);
        //     if (ret == null)
        //     {
        //         ret = BlueprintNodeDataModelUtility.CreateOrUpdateReturnNode(null, OutputParameters);
        //         BlueprintNodes.Add(ret);
        //     }
        //     else
        //     {
        //         BlueprintNodes.FindAll(x => x.NodeType == BlueprintNodeType.Return)
        //             .ForEach(x => BlueprintNodeDataModelUtility.CreateOrUpdateReturnNode(x, OutputParameters));
        //     }
        //     
        //     BlueprintNodes.FindAll(x => x.NodeType == BlueprintNodeType.Getter)
        //         .ForEach(x =>
        //         {
        //             var td = TempData.FirstOrDefault(td1 => td1.FieldName == x.MethodName);
        //             if (td != null)
        //             {
        //                 BlueprintNodeDataModelUtility.CreateOrUpdateGetterNode(x, td);
        //             }
        //         });
        //     
        //     BlueprintNodes.FindAll(x => x.NodeType == BlueprintNodeType.Setter)
        //         .ForEach(x =>
        //         {
        //             var td = TempData.FirstOrDefault(td => td.FieldName == x.MethodName);
        //             if (td != null)
        //             {
        //                 BlueprintNodeDataModelUtility.CreateOrUpdateSetterNode(x, td);
        //             }
        //         });
        //
        //     foreach (var n in BlueprintNodes)
        //     {
        //         n.Validate();
        //     }
        // }

        // public void Serialize()
        // {
        //     foreach (var n in BlueprintNodes)
        //     {
        //         for (var i = n.InEdges.Count - 1; i >= 0; i--)
        //         {
        //             var e = n.InEdges[i];
        //             var leftNode = BlueprintNodes.FirstOrDefault(ln => ln.Guid == e.LeftSidePin.NodeGuid 
        //                                                                && ln.OutEdges.Exists(oe => oe.LeftSidePin.PinName == e.LeftSidePin.PinName));
        //             if (leftNode == null)
        //             {
        //                 n.InEdges.RemoveAt(i);
        //             }
        //         }
        //
        //         for (var i = n.OutEdges.Count - 1; i >= 0; i--)
        //         {
        //             var e = n.OutEdges[i];
        //             var rightNode = BlueprintNodes.FirstOrDefault(rn => rn.Guid == e.RightSidePin.NodeGuid 
        //                                                                 && rn.InEdges.Exists(ie => ie.RightSidePin.PinName == e.RightSidePin.PinName));
        //             if (rightNode == null)
        //             {
        //                 n.OutEdges.RemoveAt(i);
        //             }
        //         }
        //     }
        //     
        //     foreach (var n in BlueprintNodes)
        //     {
        //         n.Serialize();
        //     }
        // }

        #region - Design Graph -
        public void OpenGraph()
        {
            if (DesignGraphJson.EmptyOrNull())
            {
                DesignGraph = new BlueprintDesignGraph(this, new BlueprintDesignGraphDto());
                DesignGraph.Validate();
            }
            else
            {
                var dto = JsonConvert.DeserializeObject<BlueprintDesignGraphDto>(DesignGraphJson, NewtonsoftUtility.SerializerSettings);
                DesignGraph = new BlueprintDesignGraph(this, dto);
            }
        }
        
        public void SaveGraph()
        {
            DesignGraph.Validate();
            DesignGraphJson = DesignGraph?.Serialize();
        }

        public void CompileGraph()
        {
            if (!DesignGraph.Validate())
            {
                Debug.LogError("Graph Validation Failed");
                return;
            }
            CompiledGraphJson = DesignGraph?.Compile();
        }

        public List<Type> GetAllTypes()
        {
            var dg = DesignGraph;
            if (dg == null)
            {
                Debug.LogError("Design Graph Invalid");
                return new List<Type>();
            }
            
            var allTypes = new List<Type>(300) { Type.GetType(AssemblyQualifiedTypeName) };

            foreach (var v in dg.Variables)
            {
                allTypes.Add(v.Type);
            }

            foreach (var mg in dg.Methods)
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
        #endregion
    }
}