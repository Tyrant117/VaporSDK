using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.Blueprints
{
    public interface IBlueprintGraph
    {
        bool IsEvaluating { get; }
        
        void Invoke(object[] parameters, Action<IBlueprintGraph> returnCallback);
        bool TryGetNode(string nodeGuid, out BlueprintBaseNode node);
        bool TryGetParameterValue(string paramName, out object value);
        Dictionary<string, object> GetParameters();
        
        bool TryGetTempValue(string fieldName, out object value);
        bool TrySetTempValue(string fieldName, object value);
        
        bool TryGetReturnValue<T>(string paramName, out T value);
        void WriteReturnValues(Dictionary<string,object> returnValues);
        Dictionary<string, object> GetResults();
        void Return();
    }

    public class BlueprintFunctionGraph : IBlueprintGraph
    {
        public bool IsEvaluating { get; private set; }
        private Dictionary<string, BlueprintBaseNode> Nodes { get; } = new();

        private readonly Dictionary<string, object> _inputParameters;
        private readonly Dictionary<string, object> _tempData;
        private readonly Dictionary<string, object> _returnParameters;
        private readonly BlueprintBaseNode _entryNode;
        private readonly List<string> _parameters;
        private Action<IBlueprintGraph> _returnCallback;

        public BlueprintFunctionGraph(BlueprintGraphSo so)
        {
            _parameters = new List<string>();
            _inputParameters = new Dictionary<string, object>();
            _tempData = new Dictionary<string, object>();
            _returnParameters = new Dictionary<string, object>();

            foreach (var temp in so.TempData)
            {
                var pair = temp.ToTuple();
                if (pair.Item2 == typeof(string))
                {
                    _tempData.Add(pair.Item1, string.Empty);
                }
                else
                {
                    _tempData.Add(pair.Item1, Activator.CreateInstance(pair.Item2));
                }
            }
            
            foreach (var blueprintNode in so.BlueprintNodes)
            {
                var compiledNode = blueprintNode.Compile();
                Nodes.Add(blueprintNode.Guid, compiledNode);
                switch (blueprintNode.NodeType)
                {
                    case BlueprintNodeType.Entry:
                    {
                        _entryNode = compiledNode;
                        foreach (var outPort in blueprintNode.OutPorts)
                        {
                            if (!outPort.Value.IsTransitionPort)
                            {
                                _parameters.Add(outPort.Key);
                            }
                        }

                        break;
                    }
                    case BlueprintNodeType.Return:
                    {
                        foreach (var inPort in blueprintNode.InPorts)
                        {
                            if (!inPort.Value.IsTransitionPort)
                            {
                                _returnParameters.Add(inPort.Key, null);
                            }
                        }

                        break;
                    }
                }
            }
            
            foreach (var node in Nodes)
            {
                node.Value.Init(this);
            }
        }

        public void Invoke(object[] parameters, Action<IBlueprintGraph> returnCallback = null)
        {
            Assert.IsTrue(parameters.Length == _parameters.Count, $"Parameters Count Mismatch - Length: {parameters.Length} - Expected Length: {_parameters.Count}");
            _returnCallback = returnCallback;
            for (int i = 0; i < _parameters.Count; i++)
            {
                var param = _parameters[i];
                var value = parameters[i];
                _inputParameters[param] = value;
            }

            IsEvaluating = true;
            _entryNode.InvokeAndContinue();
        }
        
        public bool TryGetNode(string nodeGuid, out BlueprintBaseNode node)
        {
            return Nodes.TryGetValue(nodeGuid, out node);
        }

        public bool TryGetParameterValue(string paramName, out object value)
        {
            return _inputParameters.TryGetValue(paramName, out value);
        }

        public Dictionary<string, object> GetParameters() => _inputParameters;
        
        public bool TryGetTempValue(string fieldName, out object value)
        {
            return _tempData.TryGetValue(fieldName, out value);
        }

        public bool TrySetTempValue(string fieldName, object value)
        {
            if (!_tempData.ContainsKey(fieldName))
            {
                return false;
            }

            _tempData[fieldName] = value;
            return true;
        }

        public bool TryGetReturnValue<T>(string paramName, out T value)
        {
            if (_returnParameters.TryGetValue(paramName, out object returnValue))
            {
                value = (T)returnValue;
                return true;
            }
            value = default;
            return false;
        }

        public void WriteReturnValues(Dictionary<string, object> returnValues)
        {
            foreach (var returnValue in returnValues)
            {
                Assert.IsTrue(_returnParameters.ContainsKey(returnValue.Key), $"Returning Unexpected Parameter: {returnValue.Key}");
                _returnParameters[returnValue.Key] = returnValue.Value;
            }
        }

        public Dictionary<string, object> GetResults() => _returnParameters;

        public void Return()
        {
            IsEvaluating = false;
            _returnCallback?.Invoke(this);
        }
    }
}
