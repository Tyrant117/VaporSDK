using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Vapor.Inspector;
using VaporEditor.Blueprints;

namespace Vapor.Blueprints
{
    [Serializable]
    public struct BlueprintMethodGraphDto
    {
        public bool IsTypeOverride;
        public bool IsBlueprintOverride;
        public Type MethodDeclaringType;
        public string MethodName;
        public string[] MethodParameters;
        public List<BlueprintArgumentDto> Arguments;
        public List<BlueprintVariableDto> Variables;
        public List<BlueprintDesignNodeDto> Nodes;
    }
    
    public class BlueprintMethodGraph
    {
        public enum ChangeType
        {
            Added,
            Removed,
            Updated
        }
        
        public BlueprintClassGraphModel ClassGraphModel { get; }


        public bool IsAbstract { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsPure { get; set; }
        public VariableAccessModifier AccessModifier { get; set; }
        public bool IsOverride => _isTypeOverride || _isBlueprintOverride;
        
        public Type MethodDeclaringType { get; set; }
        public string MethodName { get; private set; }
        public string[] MethodParameters { get; }
        public MethodInfo MethodInfo { get; }

        public List<BlueprintArgument> Arguments { get; }
        public List<BlueprintVariable> Variables { get; }
        // public List<NodeModelBase> Nodes { get; }
        public Dictionary<string, BlueprintWire> Wires { get; }
        public Dictionary<string, NodeModelBase> Nodes { get; }

        private readonly bool _isTypeOverride;
        private readonly bool _isBlueprintOverride;

        public event Action<BlueprintMethodGraph> NameChanged;
        public event Action<BlueprintMethodGraph> ArgumentsReordered;
        public event Action<BlueprintMethodGraph, BlueprintArgument, ChangeType> ArgumentChanged;
        public event Action<BlueprintMethodGraph, BlueprintVariable, ChangeType> VariableChanged;
        public event Action<BlueprintMethodGraph, NodeModelBase, ChangeType> NodeChanged;
        public event Action<BlueprintMethodGraph, BlueprintWire, ChangeType> WireChanged;

        public BlueprintMethodGraph(BlueprintClassGraphModel graphModel, BlueprintMethodGraphDto dto)
        {
            ClassGraphModel = graphModel;
            MethodName = dto.MethodName;
            if (dto.IsTypeOverride)
            {
                _isTypeOverride = true;
                MethodDeclaringType = dto.MethodDeclaringType;
                MethodParameters = dto.MethodParameters;
                MethodInfo = RuntimeReflectionUtility.GetMethodInfo(MethodDeclaringType, MethodName, MethodParameters);
            }

            if (dto.IsBlueprintOverride)
            {
                _isBlueprintOverride = true;
                MethodDeclaringType = dto.MethodDeclaringType;
                MethodParameters = dto.MethodParameters;
            }
            
            if(dto.Arguments != null)
            {
                Arguments = new List<BlueprintArgument>(dto.Arguments.Count);
                foreach (var arg in dto.Arguments)
                {
                    Arguments.Add(new BlueprintArgument(this, arg));
                }
            }
            Arguments ??= new List<BlueprintArgument>();
            
            
            if(dto.Variables != null)
            {
                Variables = new List<BlueprintVariable>(dto.Variables.Count);
                foreach (var v in dto.Variables)
                {
                    Variables.Add(new BlueprintVariable(v).WithMethodGraph(this));
                }
            }
            Variables ??= new List<BlueprintVariable>();
            
            if(dto.Nodes != null)
            {
                Nodes = new Dictionary<string, NodeModelBase>(dto.Nodes.Count);
                foreach (var nodeDto in dto.Nodes)
                {
                    var controller = NodeFactory.Build(nodeDto, this);
                    if (controller == null)
                    {
                        continue;
                    }
                    Nodes.Add(controller.Guid, controller);
                }

                foreach (var node in Nodes.Values)
                {
                    node.PostBuildData();
                }
            }
            Nodes ??= new Dictionary<string, NodeModelBase>();
        }
        
        public BlueprintMethodGraphDto Serialize()
        {
            BlueprintMethodGraphDto graphDto = new BlueprintMethodGraphDto
            {
                IsTypeOverride = _isTypeOverride,
                IsBlueprintOverride = _isBlueprintOverride,
                MethodDeclaringType = MethodDeclaringType,
                MethodName = MethodName,
                MethodParameters = MethodParameters,
                Nodes = new List<BlueprintDesignNodeDto>(Nodes.Count),
                Arguments = new List<BlueprintArgumentDto>(Arguments.Count),
                Variables = new List<BlueprintVariableDto>(Variables.Count),
            };
            foreach (var arg in Arguments)
            {
                graphDto.Arguments.Add(arg.Serialize());
            }
            foreach (var v in Variables)
            {
                graphDto.Variables.Add(v.Serialize());
            }
            foreach (var node in Nodes.Values)
            {
                graphDto.Nodes.Add(node.Serialize());
            }

            return graphDto;
        }

        public bool Validate()
        {
            bool valid = true;
            bool eNew = false;
            bool rNew = false;
            NodeModelBase eNode = null;
            NodeModelBase rNode = null;
            var entry = Nodes.FirstOrDefault(x => x.Value.NodeType == NodeType.Entry).Value;
            if (entry == null)
            {
                eNode = NodeFactory.Build(NodeType.Entry, Vector2.zero, this);
                Nodes.Add(eNode.Guid, eNode);
                valid = false;
                eNew = true;
            }
            
            var ret = Nodes.FirstOrDefault(x => x.Value.NodeType == NodeType.Return).Value;
            if (ret == null)
            {
                rNode = NodeFactory.Build(NodeType.Return, Vector2.zero + Vector2.right * 200, this);
                Nodes.Add(rNode.Guid, rNode);
                valid = false;
                rNew = true;
            }

            if (eNew && rNew)
            {
                var leftPort = new BlueprintPinReference(PinNames.EXECUTE_OUT, eNode.Guid, true);
                var rightPort = new BlueprintPinReference(PinNames.EXECUTE_IN, rNode.Guid, true);
                var wireRef = new BlueprintWireReference(leftPort, rightPort);
                eNode.OutputWires.Add(wireRef);
                rNode.InputWires.Add(wireRef);
            }
            return valid;
        }

        #region - Method Settings -

        public void SetName(string newName)
        {
            MethodName = newName;
            NameChanged?.Invoke(this);
            ClassGraphModel.OnMethodUpdated(this);
        }

        #endregion


        #region - Arguments -

        public BlueprintArgument AddInputArgument(Type type)
        {
            var argument = new BlueprintArgument(this, type, $"InArg_{Arguments.Count}", Arguments.Count, false, false, false);
            Arguments.Add(argument);
            ArgumentChanged?.Invoke(this, argument, ChangeType.Added);
            return argument;
        }

        public BlueprintArgument AddOutputArgument(Type type)
        {
            bool alreadyHasReturn = false;
            foreach (var arg in Arguments)
            {
                if (arg.IsReturn)
                {
                    alreadyHasReturn = true;
                    break;
                }
            }

            var argument = new BlueprintArgument(this, type, $"OutArg_{Arguments.Count}", Arguments.Count, false, alreadyHasReturn, !alreadyHasReturn);
            Arguments.Add(argument);
            ArgumentChanged?.Invoke(this, argument, ChangeType.Added);
            return argument;
        }
        
        public void RemoveArgument(BlueprintArgument argument)
        {
            if (Arguments.Remove(argument))
            {
                ArgumentChanged?.Invoke(this, argument, ChangeType.Removed);
                bool hasReturn = false;
                foreach (var arg in Arguments)
                {
                    if (!arg.IsReturn)
                    {
                        continue;
                    }

                    hasReturn = true;
                    break;
                }

                // Need to reset the first out arg as the return value if it exists.
                if (!hasReturn)
                {
                    var firstOutArg = Arguments.FirstOrDefault(arg => arg.IsOut);
                    if (firstOutArg != null)
                    {
                        firstOutArg.IsReturn = true;
                        ArgumentChanged?.Invoke(this, firstOutArg, ChangeType.Updated);
                    }
                }
            }
        }

        public void OnArgumentUpdated(BlueprintArgument argument)
        {
            ArgumentChanged?.Invoke(this, argument, ChangeType.Updated);
        }

        public void OnArgumentsReordered()
        {
            Arguments.Sort((a, b) => a.ParameterIndex.CompareTo(b.ParameterIndex));
            ArgumentsReordered?.Invoke(this);
        }

        #endregion

        #region - Variables -

        public BlueprintVariable AddVariable(Type type)
        {
            var selection = Variables.FindAll(v => v.Name.StartsWith("LocalVar_")).Select(v => v.Name.Split('_')[1]);
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
            
            var variable = new BlueprintVariable($"LocalVar_{idx}", type, VariableScopeType.Method).WithMethodGraph(this);
            Variables.Add(variable);
            VariableChanged?.Invoke(this, variable, ChangeType.Added);
            return variable;
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

        public bool TryGetVariable(VariableScopeType variableScope, string variableName, out BlueprintVariable variable)
        {
            switch (variableScope)
            {
                case VariableScopeType.Method:
                    variable = Variables.FirstOrDefault(x => x.Name == variableName);
                    return variable != null;
                case VariableScopeType.Class:
                    variable = ClassGraphModel.Variables.FirstOrDefault(x => x.Name == variableName);
                    return variable != null;
                default:
                    variable = null;
                    return false;
            }
        }

        #endregion

        #region - Wires -

        public BlueprintWire AddWire(BlueprintPin left, BlueprintPin right)
        {
            var wire = new BlueprintWire(this);
            wire.Connect(left, right);
            WireChanged?.Invoke(this, wire, ChangeType.Added);
            return wire;
        }

        public bool RemoveWire(BlueprintWire wire)
        {
            if (Wires.Remove(wire.Guid))
            {
                WireChanged?.Invoke(this, wire, ChangeType.Removed);
            }

            return false;
        }

        public void OnWireUpdated(BlueprintWire wire)
        {
            WireChanged?.Invoke(this, wire, ChangeType.Updated);
        }
        
        #endregion

        #region - Nodes -

        public NodeModelBase AddNode()
        {
            return null;
        }

        public bool RemoveNode(NodeModelBase node)
        {
            return false;
        }
        
        public void OnUpdateNode(NodeModelBase node)
        {
            NodeChanged?.Invoke(this, node, ChangeType.Updated);
        }

        #endregion

        public void Edit()
        {
            ClassGraphModel.OpenMethodForEdit(this);
        }

        public void Delete()
        {
            ClassGraphModel.RemoveMethod(this);
        }
    }

    [Serializable]
    public struct BlueprintWireDto
    {
        public string Guid;
        public uint Uuid;
        public bool IsExecuteWire;
        
        public string LeftGuid;
        public string LeftName;

        public string RightGuid;
        public string RightName;
    }
    
    public class BlueprintWire
    {
        
        public string Guid { get; set; }
        public uint Uuid { get; }
        public bool IsExecuteWire { get; set; }

        public string LeftGuid { get; set; }
        public string LeftName { get; set; }
        
        public string RightGuid { get; set; }
        public string RightName { get; set; }
        
        
        private readonly BlueprintMethodGraph _graph;

        public BlueprintWire(BlueprintMethodGraph graph)
        {
            _graph = graph;
            Guid = System.Guid.NewGuid().ToString();
            Uuid = Guid.GetStableHashU32();
        }

        public bool IsConnected()
        {
            if (LeftGuid.EmptyOrNull() || RightGuid.EmptyOrNull())
            {
                return false;
            }

            if (!_graph.Nodes.TryGetValue(LeftGuid, out var leftNode))
            {
                return false;
            }
            
            if (!_graph.Nodes.TryGetValue(RightGuid, out var rightNode))
            {
                return false;
            }
            
            return leftNode.OutputPins.ContainsKey(LeftName) && rightNode.InputPins.ContainsKey(RightName);
        }

        public void Connect(BlueprintPin leftPin, BlueprintPin rightPin)
        {
            IsExecuteWire = leftPin.IsExecutePin;
            
            LeftGuid = leftPin.Node.Guid;
            LeftName = leftPin.PortName;
            
            RightGuid = rightPin.Node.Guid;
            RightName = rightPin.PortName;
        }
        
        public void Disconnect()
        {
            LeftGuid = null;
            LeftName = null;
            
            RightGuid = null;
            RightName = null;
        }
        

        public BlueprintWireDto Serialize()
        {
            return new BlueprintWireDto()
            {
                Guid = Guid,
                Uuid = Uuid,
                IsExecuteWire = IsExecuteWire,
                
                LeftGuid = LeftGuid,
                LeftName = LeftName,
                
                RightGuid = RightGuid,
                RightName = RightName,
            };
        }
    }
}