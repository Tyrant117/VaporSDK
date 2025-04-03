using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

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
        public string Name { get; private set; }
        public bool IsEvaluating { get; private set; }
        private Dictionary<string, BlueprintBaseNode> Nodes { get; } = new();

        private readonly bool _isMock;
        private readonly Dictionary<string, object> _inputParameters;
        private readonly Dictionary<string, object> _tempData;
        private readonly Dictionary<string, object> _returnParameters;
        private readonly BlueprintBaseNode _entryNode;
        private readonly List<string> _parameters;
        private readonly List<Type> _parameterTypes;
        private Action<IBlueprintGraph> _returnCallback;

        public BlueprintFunctionGraph(BlueprintGraphSo so, bool isMockGraph = false)
        {
            Name = so.DisplayName;
            _isMock = isMockGraph;
            _parameters = new List<string>();
            _parameterTypes = new List<Type>();
            _inputParameters = new Dictionary<string, object>();
            _tempData = new Dictionary<string, object>();
            _returnParameters = new Dictionary<string, object>();

            // foreach (var temp in so.DesignGraph.Variables)
            // {
            //     if (temp.Type == typeof(string))
            //     {
            //         _tempData.Add(temp.Name, string.Empty);
            //     }
            //     else
            //     {
            //         if (!temp.Type.IsSubclassOf(typeof(Object)))
            //         {
            //             _tempData.Add(temp.Name, Activator.CreateInstance(temp.Type));
            //         }
            //         else
            //         {
            //             if (!_isMock)
            //             {
            //                 _tempData.Add(temp.Name, null);
            //             }
            //             else
            //             {
            //                 if (temp.Type.IsSubclassOf(typeof(ScriptableObject)))
            //                 {
            //                     var tmpSo = ScriptableObject.CreateInstance(temp.Type);
            //                     tmpSo.hideFlags = HideFlags.HideAndDontSave;
            //                     _tempData.Add(temp.Name, tmpSo);
            //                 }
            //                 else if(temp.Type.IsSubclassOf(typeof(Component)))
            //                 {
            //                     var go = new GameObject(temp.Name)
            //                     {
            //                         hideFlags = HideFlags.HideAndDontSave
            //                     };
            //                     _tempData.Add(temp.Name, go.AddComponent(temp.Type));
            //                 }
            //                 else
            //                 {
            //                     _tempData.Add(temp.Name, null);
            //                 }
            //             }
            //         }
            //     }
            // }
            //
            // foreach (var blueprintNode in so.DesignGraph.Current.Nodes)
            // {
            //     var runtimeNode = blueprintNode.ConvertToRuntime();
            //     Nodes.Add(blueprintNode.Guid, runtimeNode);
            //     if (blueprintNode.Type == typeof(EntryNodeType))
            //     {
            //         _entryNode = runtimeNode;
            //         foreach (var outPort in blueprintNode.OutPorts)
            //         {
            //             if (!outPort.Value.IsExecutePin)
            //             {
            //                 _parameters.Add(outPort.Key);
            //                 _parameterTypes.Add(outPort.Value.Type);
            //             }
            //         }
            //     }
            //     else if (blueprintNode.Type == typeof(ReturnNodeType))
            //     {
            //         foreach (var inPort in blueprintNode.InPorts)
            //         {
            //             if (!inPort.Value.IsExecutePin)
            //             {
            //                 _returnParameters.Add(inPort.Key, null);
            //             }
            //         }
            //     }
            // }

            foreach (var node in Nodes)
            {
                node.Value.Init(this);
            }
        }

        public void Invoke(object[] parameters, Action<IBlueprintGraph> returnCallback)
        {
            Assert.IsTrue(parameters.Length == _parameters.Count, $"{Name} - Parameters Count Mismatch - Length: {parameters.Length} - Expected Length: {_parameters.Count}");
            _returnCallback = returnCallback;
            for (int i = 0; i < _parameters.Count; i++)
            {
                var param = _parameters[i];
                var value = parameters[i];
                var type = _parameterTypes[i];
                Assert.IsTrue(type.IsAssignableFrom(value.GetType()), $"{Name} - {param} is not assignable from {value.GetType()}");
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
            
            if (!_isMock)
            {
                return;
            }

            // Clear instantiated data.
            foreach (var tmp in _tempData)
            {
                if (tmp.Value.IsObjectNull() || !tmp.Value.GetType().IsSubclassOf(typeof(Object)))
                {
                    continue;
                }

                var tmpObject = tmp.Value as Object;
                Object.DestroyImmediate(tmpObject);
            }
        }
    }
}
