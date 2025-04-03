using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    [Serializable]
    public struct BlueprintMethodGraphDto
    {
        public bool IsOverride;
        public Type MethodDeclaringType;
        public string MethodName;
        public string[] MethodParameters;
        public List<BlueprintVariableDto> InputArguments;
        public List<BlueprintVariableDto> OutputArguments;
        public List<BlueprintVariableDto> TemporaryVariables;
        public List<BlueprintDesignNodeDto> Nodes;
    }
    
    public class BlueprintMethodGraph
    {
        public BlueprintDesignGraph ClassGraph { get; }
        public bool IsOverride { get; }
        public Type MethodDeclaringType { get; set; }
        public string MethodName { get; set; }
        public string[] MethodParameters { get; }
        public MethodInfo MethodInfo { get; }

        public List<BlueprintVariable> InputArguments { get; }
        public List<BlueprintVariable> OutputArguments { get; }
        public List<BlueprintVariable> TemporaryVariables { get; }
        public List<BlueprintNodeController> Nodes { get; }

        public BlueprintMethodGraph(BlueprintDesignGraph graph, BlueprintMethodGraphDto dto)
        {
            ClassGraph = graph;
            MethodName = dto.MethodName;
            if (dto.IsOverride)
            {
                IsOverride = true;
                MethodDeclaringType = dto.MethodDeclaringType;
                MethodParameters = dto.MethodParameters;
                MethodInfo = RuntimeReflectionUtility.GetMethodInfo(MethodDeclaringType, MethodName, MethodParameters);
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
                Nodes = new List<BlueprintNodeController>(dto.Nodes.Count);
                foreach (var nodeDto in dto.Nodes)
                {
                    var controller = BlueprintNodeControllerFactory.Build(nodeDto, this);
                    if (controller == null)
                    {
                        continue;
                    }
                    Nodes.Add(controller);
                }

                foreach (var node in Nodes)
                {
                    node.PostBuild();
                }
            }
            Nodes ??= new List<BlueprintNodeController>();
        }
        
        public BlueprintMethodGraphDto Serialize()
        {
            BlueprintMethodGraphDto graphDto = new BlueprintMethodGraphDto
            {
                IsOverride = IsOverride,
                MethodDeclaringType = MethodDeclaringType,
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

        // public BlueprintCompiledMethodGraphDto Compile()
        // {
        //     BlueprintCompiledMethodGraphDto graphDto = new BlueprintCompiledMethodGraphDto
        //     {
        //         Nodes = new List<BlueprintCompiledNodeDto>(Nodes.Count),
        //         InputArguments = new List<BlueprintVariableDto>(InputArguments.Count),
        //         OutputArguments = new List<BlueprintVariableDto>(OutputArguments.Count),
        //         TemporaryVariables = new List<BlueprintVariableDto>(TemporaryVariables.Count),
        //     };
        //     foreach (var arg in InputArguments)
        //     {
        //         graphDto.InputArguments.Add(arg.Serialize());
        //     }
        //     foreach (var arg in OutputArguments)
        //     {
        //         graphDto.OutputArguments.Add(arg.Serialize());
        //     }
        //     foreach (var v in TemporaryVariables)
        //     {
        //         graphDto.TemporaryVariables.Add(v.Serialize());
        //     }
        //     foreach (var node in Nodes)
        //     {
        //         graphDto.Nodes.Add(node.Compile());
        //     }
        //
        //     return graphDto; //JsonConvert.SerializeObject(graphDto, NewtonsoftUtility.SerializerSettings);
        // }

        public bool Validate()
        {
            bool valid = true;
            bool eNew = false;
            bool rNew = false;
            var entry = Nodes.FirstOrDefault(x => x.Model.NodeType == NodeType.Entry);
            if (entry == null)
            {
                var controller = BlueprintNodeControllerFactory.Build(NodeType.Entry, Vector2.zero, this);
                Nodes.Insert(0, controller);
                valid = false;
                eNew = true;
            }
            
            var ret = Nodes.FirstOrDefault(x => x.Model.NodeType == NodeType.Return);
            if (ret == null)
            {
                var controller = BlueprintNodeControllerFactory.Build(NodeType.Return, Vector2.zero + Vector2.right * 200, this);
                Nodes.Insert(1, controller);
                valid = false;
                rNew = true;
            }

            if (eNew && rNew)
            {
                var leftPort = new BlueprintPinReference(PinNames.EXECUTE_OUT, Nodes[0].Model.Guid, true);
                var rightPort = new BlueprintPinReference(PinNames.EXECUTE_IN, Nodes[1].Model.Guid, true);
                var wireRef = new BlueprintWireReference(leftPort, rightPort);
                Nodes[0].Model.OutputWires.Add(wireRef);
                Nodes[1].Model.InputWires.Add(wireRef);
            }
            return valid;
        }
        
        public BlueprintVariable AddInputArgument(Type type)
        {
            var selection = InputArguments.FindAll(v => v.Name.StartsWith("Input_")).Select(v => v.Name.Split('_')[1]);
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
            
            var tmp = new BlueprintVariable($"Input_{idx}", type, VariableType.Argument).WithMethodGraph(this);
            InputArguments.Add(tmp);
            return tmp;
        }
        
        public BlueprintVariable AddOutputArgument(Type type)
        {
            var selection = OutputArguments.FindAll(v => v.Name.StartsWith("Output_")).Select(v => v.Name.Split('_')[1]);
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
            
            var tmp = new BlueprintVariable($"Output_{idx}", type, VariableType.Return).WithMethodGraph(this);
            OutputArguments.Add(tmp);
            return tmp;
        }

        public BlueprintVariable AddTemporaryVariable(Type type)
        {
            var selection = TemporaryVariables.FindAll(v => v.Name.StartsWith("LocalVar_")).Select(v => v.Name.Split('_')[1]);
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
            
            var tmp = new BlueprintVariable($"LocalVar_{idx}", type, VariableType.Local).WithMethodGraph(this);
            TemporaryVariables.Add(tmp);
            return tmp;
        }

        public bool TryGetVariable(VariableScopeType variableScope, string variableName, out BlueprintVariable variable)
        {
            switch (variableScope)
            {
                case VariableScopeType.Block:
                case VariableScopeType.Method:
                    variable = TemporaryVariables.FirstOrDefault(x => x.Name == variableName);
                    return variable != null;
                case VariableScopeType.Class:
                    variable = ClassGraph.Variables.FirstOrDefault(x => x.Name == variableName);
                    return variable != null;
                default:
                    variable = null;
                    return false;
            }
        }
    }
}