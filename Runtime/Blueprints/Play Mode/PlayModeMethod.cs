using System;
using System.Collections.Generic;
using System.Linq;

namespace Vapor.Blueprints
{
    public class PlayModeMethod
    {
        public bool IsEvaluating { get; private set; }
        public Stack<PlayModeIteratorNode> IteratorNodeStack { get; set; } = new();
        
        private readonly PlayModeClass _playModeClass;
        private readonly BlueprintMethodGraphDto _model;
        private readonly Dictionary<string, object> _localVariables = new();
        private readonly object[] _arguments;
        private readonly bool _hasReturnValue;
        private object _returnValue;
        private readonly object[] _outArguments;
        private readonly Dictionary<string, PlayModeNodeBase> _nodes = new();
        private readonly PlayModeEntryNode _entryNode;

        public PlayModeMethod(PlayModeClass playModeClass, BlueprintMethodGraphDto dto)
        {
            _playModeClass = playModeClass;
            _model = dto;
            int argCount = 0;
            int outArgCount = 0;
            foreach (var arg in dto.Arguments)
            {
                if (arg is { IsOut: false, IsReturn: false })
                {
                    argCount++;
                }
                else
                {
                    if (arg is { IsOut: true, IsReturn: false })
                    {
                        outArgCount++;
                    }
                    else
                    {
                        _hasReturnValue = true;
                    }
                }
            }
            _arguments = new object[argCount];
            _outArguments = new object[outArgCount];
            
            foreach (var node in dto.Nodes)
            {
                var compiled = PlayModeCompiler.Compile(node, dto.Wires);
                
                if (node.NodeEnumType == NodeType.Entry)
                {
                    _entryNode = compiled as PlayModeEntryNode;
                }
                _nodes.Add(node.Guid, compiled);
            }

            foreach (var node in _nodes.Values)
            {
                node.Init(_playModeClass, this);
            }
        }

        public object Invoke(object[] parameters, out object[] outParameters)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                _arguments[i] = parameters[i];
            }

            foreach (var variable in _model.Variables)
            {
                if (variable.ConstructorName.Equals("Default(T)"))
                {
                    var defaultValue = TypeUtility.CastToType(variable.DefaultParametersValue[0].Item2, variable.DefaultParametersValue[0].Item1);
                    _localVariables[variable.Id] = defaultValue;
                }
                else
                {
                    var defaultValue = Activator.CreateInstance(variable.Type, variable.DefaultParametersValue.Select(t => TypeUtility.CastToType(t.Item2, t.Item1)));
                    _localVariables[variable.Id] = defaultValue;
                }
            }

            IsEvaluating = true;
            _entryNode.InvokeAndContinue();
            IsEvaluating = false;
            
            outParameters = new object[_outArguments.Length];
            for (int i = 0; i < _outArguments.Length; i++)
            {
                outParameters[i] = _outArguments[i];
            }

            return _hasReturnValue ? _returnValue : null;
        }

        public object GetArgument(int index)
        {
            return _arguments[index];
        }
        
        public object GetLocalVariable(string variableId)
        {
            return _localVariables[variableId];
        }
        
        public void SetLocalVariable(string variableId, object value)
        {
            _localVariables[variableId] = value;
        }
        
        public PlayModeNodeBase GetNode(string nodeGuid)
        {
            return _nodes[nodeGuid];
        }

        public void SetReturnValue(object value)
        {
            _returnValue = value;
        }
        
        public void SetOutArguments(int index, object argument)
        {
            _outArguments[index] = argument;
        }

        public void Break()
        {
            if (IteratorNodeStack.TryPeek(out var top))
            {
                top.Break();
            }
        }
    }
}