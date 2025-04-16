using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using VaporEditor.Blueprints;

namespace Vapor.Blueprints
{
    public class BlueprintMethodGraph : IBlueprintGraphModel
    {
        public BlueprintClassGraphModel ClassGraphModel { get; }


        public bool IsAbstract { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsPure { get; set; }
        public VariableAccessModifier AccessModifier { get; set; }
        public bool IsOverride => _isTypeOverride || _isBlueprintOverride;
        public bool IsUnityOverride => _isUnityOverride;
        
        public Type MethodDeclaringType { get; set; }
        public string MethodName { get; private set; }
        public string[] MethodParameters { get; }
        public MethodInfo MethodInfo { get; }

        public List<BlueprintArgument> Arguments { get; }
        public Dictionary<string, BlueprintVariable> Variables { get; }
        public Dictionary<string, NodeModelBase> Nodes { get; }
        public Dictionary<string, BlueprintWire> Wires { get; }

        private readonly bool _isTypeOverride;
        private readonly bool _isBlueprintOverride;
        private readonly bool _isUnityOverride;

        public event Action<BlueprintMethodGraph> NameChanged;
        public event Action<BlueprintMethodGraph> ArgumentsReordered;
        public event Action<BlueprintMethodGraph, BlueprintArgument, ChangeType, bool> ArgumentChanged;
        public event Action<BlueprintMethodGraph, BlueprintVariable, ChangeType, bool> VariableChanged;
        public event Action<BlueprintMethodGraph, NodeModelBase, ChangeType, bool> NodeChanged;
        public event Action<BlueprintMethodGraph, BlueprintWire, ChangeType, bool> WireChanged;

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

            if (dto.IsUnityOverride)
            {
                _isUnityOverride = true;
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
                Variables = new Dictionary<string, BlueprintVariable>(dto.Variables.Count);
                foreach (var v in dto.Variables)
                {
                    Variables.Add(v.Id, new BlueprintVariable(v).WithMethodGraph(this));
                }
            }
            Variables ??= new Dictionary<string, BlueprintVariable>();
            
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

            if (dto.Wires != null)
            {
                Wires = new Dictionary<string, BlueprintWire>(dto.Wires.Count);
                foreach (var wireDto in dto.Wires)
                {
                    BlueprintWire wire = new BlueprintWire(this, wireDto);
                    Wires.Add(wire.Guid, wire);
                }
            }
            Wires ??= new Dictionary<string, BlueprintWire>();

            foreach (var w in Wires.Values)
            {
                if (Nodes.TryGetValue(w.LeftGuid, out var leftNode) && leftNode.OutputPins.TryGetValue(w.LeftName, out var leftOutputPin))
                {
                    leftOutputPin.Wires.Add(w);
                }

                if (Nodes.TryGetValue(w.RightGuid, out var rightNode) && rightNode.InputPins.TryGetValue(w.RightName, out var rightOutputPin))
                {
                    rightOutputPin.Wires.Add(w);
                }
            }
            
            foreach (var node in Nodes.Values)
            {
                node.PostConnectWires();
            }
        }
        
        public BlueprintMethodGraphDto Serialize()
        {
            BlueprintMethodGraphDto graphDto = new BlueprintMethodGraphDto
            {
                IsTypeOverride = _isTypeOverride,
                IsBlueprintOverride = _isBlueprintOverride,
                IsUnityOverride = _isUnityOverride,
                MethodDeclaringType = MethodDeclaringType,
                MethodName = MethodName,
                MethodParameters = MethodParameters,
                Nodes = new List<BlueprintDesignNodeDto>(Nodes.Count),
                Arguments = new List<BlueprintArgumentDto>(Arguments.Count),
                Variables = new List<BlueprintVariableDto>(Variables.Count),
                Wires = new List<BlueprintWireDto>(Wires.Count),
            };
            foreach (var arg in Arguments)
            {
                graphDto.Arguments.Add(arg.Serialize());
            }
            foreach (var v in Variables.Values)
            {
                graphDto.Variables.Add(v.Serialize());
            }
            foreach (var node in Nodes.Values)
            {
                graphDto.Nodes.Add(node.Serialize());
            }
            foreach (var w in Wires.Values)
            {
                graphDto.Wires.Add(w.Serialize());
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
                eNode = NodeFactory.Build(NodeType.Entry, Vector2.zero, this, null);
                eNode.PostBuildData();
                Nodes.Add(eNode.Guid, eNode);
                valid = false;
                eNew = true;
            }
            
            var ret = Nodes.FirstOrDefault(x => x.Value.NodeType == NodeType.Return).Value;
            if (ret == null)
            {
                rNode = NodeFactory.Build(NodeType.Return, Vector2.zero + Vector2.right * 200, this, null);
                rNode.PostBuildData();
                Nodes.Add(rNode.Guid, rNode);
                valid = false;
                rNew = true;
            }

            if (eNew && rNew)
            {
                // var leftPort = new BlueprintPinReference(PinNames.EXECUTE_OUT, eNode.Guid, true);
                // var rightPort = new BlueprintPinReference(PinNames.EXECUTE_IN, rNode.Guid, true);
                // var wireRef = new BlueprintWireReference(leftPort, rightPort);
                // eNode.OutputWires.Add(wireRef);
                // rNode.InputWires.Add(wireRef);
                
                AddWire(eNode.OutputPins[PinNames.EXECUTE_OUT], rNode.InputPins[PinNames.EXECUTE_IN]);
            }
            return valid;
        }

        #region - Method Settings -

        public void SetName(string newName)
        {
            MethodName = newName;
            NameChanged?.Invoke(this);
            ClassGraphModel.OnMethodUpdated(this);
            if (ClassGraphModel.Current == this)
            {
                ClassGraphModel.Graph.LastOpenedMethod = MethodName;
            }
        }

        #endregion


        #region - Arguments -

        public BlueprintArgument AddInputArgument(Type type, bool ignoreUndo = false)
        {
            var argument = new BlueprintArgument(this, type, $"InArg_{Arguments.Count}", Arguments.Count, false, false, false);
            Arguments.Add(argument);
            ArgumentChanged?.Invoke(this, argument, ChangeType.Added, ignoreUndo);
            return argument;
        }

        public BlueprintArgument AddOutputArgument(Type type, bool ignoreUndo = false)
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
            ArgumentChanged?.Invoke(this, argument, ChangeType.Added, ignoreUndo);
            return argument;
        }
        
        public BlueprintArgument AddArgument(BlueprintArgument argument, bool ignoreUndo)
        {
            Arguments.Add(argument);
            ArgumentChanged?.Invoke(this, argument, ChangeType.Added, ignoreUndo);
            return argument;
        }
        
        public void RemoveArgument(BlueprintArgument argument, bool ignoreUndo = false)
        {
            if (Arguments.Remove(argument))
            {
                ArgumentChanged?.Invoke(this, argument, ChangeType.Removed, ignoreUndo);
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
                        ArgumentChanged?.Invoke(this, firstOutArg, ChangeType.Modified, ignoreUndo);
                    }
                }
            }
        }

        public void OnArgumentUpdated(BlueprintArgument argument, bool ignoreUndo = false)
        {
            ArgumentChanged?.Invoke(this, argument, ChangeType.Modified, ignoreUndo);
        }

        public void OnArgumentsReordered()
        {
            Arguments.Sort((a, b) => a.ParameterIndex.CompareTo(b.ParameterIndex));
            ArgumentsReordered?.Invoke(this);
        }

        #endregion

        #region - Variables -

        public BlueprintVariable AddVariable(Type type, bool ignoreUndo = false)
        {
            var selection = Variables.Values.ToList().FindAll(v => v.DisplayName.StartsWith("LocalVar_")).Select(v => v.DisplayName.Split('_')[1]);
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
            Variables.Add(variable.Id, variable);
            VariableChanged?.Invoke(this, variable, ChangeType.Added, ignoreUndo);
            return variable;
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

        public bool TryGetVariable(VariableScopeType variableScope, string variableId, out BlueprintVariable variable)
        {
            switch (variableScope)
            {
                case VariableScopeType.Method:
                    return Variables.TryGetValue(variableId, out variable);
                case VariableScopeType.Class:
                    return ClassGraphModel.Variables.TryGetValue(variableId, out variable);
                default:
                    variable = null;
                    return false;
            }
        }

        #endregion

        #region - Wires -

        public BlueprintWire AddWire(BlueprintPin left, BlueprintPin right, bool ignoreUndo = false)
        {
            var wire = new BlueprintWire(this);
            wire.Connect(left, right);
            Wires.Add(wire.Guid, wire);
            WireChanged?.Invoke(this, wire, ChangeType.Added, ignoreUndo);
            return wire;
        }
        
        public BlueprintWire AddWire(BlueprintWire wire, bool ignoreUndo)
        {
            Wires.Add(wire.Guid, wire);
            if (Nodes.TryGetValue(wire.LeftGuid, out var leftNode) 
                && leftNode.OutputPins.TryGetValue(wire.LeftName, out var leftPin) 
                && Nodes.TryGetValue(wire.RightGuid, out var rightNode) 
                && rightNode.InputPins.TryGetValue(wire.RightName, out var rightPin))
            {
                wire.Connect(leftPin, rightPin);
            }
            WireChanged?.Invoke(this, wire, ChangeType.Added, ignoreUndo);
            return wire;
        }
        

        public bool RemoveWire(BlueprintWire wire, bool ignoreUndo = false)
        {
            if (Wires.Remove(wire.Guid))
            {
                if (Nodes.TryGetValue(wire.LeftGuid, out var leftNode))
                {
                    leftNode.OnWireRemoved(wire);
                }

                if (Nodes.TryGetValue(wire.RightGuid, out var rightNode))
                {
                    rightNode.OnWireRemoved(wire);
                }
                WireChanged?.Invoke(this, wire, ChangeType.Removed, ignoreUndo);
            }

            return false;
        }

        public void OnWireUpdated(BlueprintWire wire, bool ignoreUndo = false)
        {
            WireChanged?.Invoke(this, wire, ChangeType.Modified, ignoreUndo);
        }
        
        #endregion

        #region - Nodes -

        public NodeModelBase AddNode(NodeType nodeType, Vector2 position, object userData, bool ignoreUndo = false)
        {
            var node = NodeFactory.Build(nodeType, position, this, userData);
            node.PostBuildData();
            Nodes.Add(node.Guid, node);
            NodeChanged?.Invoke(this, node, ChangeType.Added, ignoreUndo);
            return node;
        }
        
        public NodeModelBase AddNode(NodeModelBase node, bool ignoreUndo)
        {
            Nodes.Add(node.Guid, node);
            NodeChanged?.Invoke(this, node, ChangeType.Added, ignoreUndo);
            return node;
        }
        
        public NodeModelBase PasteNode(NodeModelBase node, bool ignoreUndo = false)
        {
            node.ResetGuid();
            Nodes.Add(node.Guid, node);
            NodeChanged?.Invoke(this, node, ChangeType.Added, ignoreUndo);
            return node;
        }

        public bool RemoveNode(NodeModelBase node, bool ignoreUndo = false)
        {
            if (Nodes.Remove(node.Guid))
            {
                var wires = new List<BlueprintWire>();
                foreach (var pin in node.InputPins.Values)
                {
                    wires.AddRange(pin.Wires);
                }
                foreach (var pin in node.OutputPins.Values)
                {
                    wires.AddRange(pin.Wires);
                }

                foreach (var wire in wires)
                {
                    wire.Delete();
                }

                NodeChanged?.Invoke(this, node, ChangeType.Removed, ignoreUndo);
                return true;
            }
            return false;
        }
        
        public void OnUpdateNode(NodeModelBase node, bool ignoreUndo = false)
        {
            NodeChanged?.Invoke(this, node, ChangeType.Modified, ignoreUndo);
        }

        public void OverwriteNode(NodeModelBase overwrite, bool ignoreUndo = false)
        {
            Nodes[overwrite.Guid] = overwrite;
            OnUpdateNode(overwrite, ignoreUndo);
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
}