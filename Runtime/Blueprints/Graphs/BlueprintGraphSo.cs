using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vapor.Inspector;
using Vapor.Keys;

namespace Vapor.Blueprints
{
    [DatabaseKeyValuePair, KeyOptions(includeNone: false, category: "Graphs")]
    public class BlueprintGraphSo : NamedKeySo, IDatabaseInitialize
    {
        [SerializeField, FoldoutGroup("Parameters"), ListDrawer(elementChangedMethodName: "OnInputElementChanged")]
        private List<BlueprintIOParameter> _inputParameters = new(); 
        public List<BlueprintIOParameter> InputParameters => _inputParameters;
        
        [SerializeField, FoldoutGroup("Parameters"), ListDrawer(elementChangedMethodName: "OnOutputElementChanged")]
        private List<BlueprintIOParameter> _outputParameters = new(); 
        public List<BlueprintIOParameter> OutputParameters => _outputParameters;
        
        [SerializeField, FoldoutGroup("Temporary Data"), ListDrawer(elementChangedMethodName: "OnTempDataElementChanged")]
        private List<BlueprintIOParameter> _tempData = new();
        public List<BlueprintIOParameter> TempData => _tempData;
        
        [SerializeField, FoldoutGroup("For Information Only")] 
        private List<BlueprintNodeDataModel> _nodes = new(); 
        public List<BlueprintNodeDataModel> BlueprintNodes => _nodes;
        
        public IBlueprintGraph Graph { get; set; }

        private void OnInputElementChanged(int index, string propertyName, object previousValue, object newValue)
        {
            //Debug.Log($"OnInputElementChanged {index} - {propertyName} - {previousValue} - {newValue}");
            if (index == -1)
            {
                return;
            }
            
            if (propertyName.EmptyOrNull() || propertyName != "FieldName")
            {
                return;
            }
            
            var fieldName = InputParameters[index].FieldName;
            var previousName = InputParameters[index].PreviousName;
            
            BlueprintNodes.ForEach(n =>
            {
                if (n.NodeType == BlueprintNodeType.Entry)
                {
                    var edges = n.OutEdges.FindAll(e => e.LeftSidePin.PinName == previousName);
                    foreach (var edge in edges)
                    {
                        int idx = n.OutEdges.IndexOf(edge);
                        var oldPort = n.OutEdges[idx].LeftSidePin;
                        var newPort = new BlueprintPinReference(fieldName, oldPort.NodeGuid, oldPort.IsExecutePin);
                        var newEdge = new BlueprintWireReference(newPort, n.OutEdges[idx].RightSidePin);
                        n.OutEdges[idx] = newEdge;
                    }
                }
                
                var edgeConnections = n.InEdges.FindAll(e => e.LeftSidePin.PinName == previousName);
                foreach (var edge in edgeConnections)
                {
                    int idx = n.InEdges.IndexOf(edge);
                    var oldPort = n.InEdges[idx].LeftSidePin;
                    var newPort = new BlueprintPinReference(fieldName, oldPort.NodeGuid, oldPort.IsExecutePin);
                    var newEdge = new BlueprintWireReference(newPort, n.InEdges[idx].RightSidePin);
                    n.InEdges[idx] = newEdge;
                }
            });
        }

        private void OnOutputElementChanged(int index, string propertyName,object previousValue, object newValue)
        {
            //Debug.Log($"OnOutputElementChanged {index} - {propertyName} - {previousValue} - {newValue}");
            if (index == -1)
            {
                return;
            }

            if (propertyName.EmptyOrNull() || propertyName != "FieldName")
            {
                return;
            }
            
            var fieldName = OutputParameters[index].FieldName;
            var previousName = OutputParameters[index].PreviousName;
            
            BlueprintNodes.ForEach(n =>
            {
                if (n.NodeType == BlueprintNodeType.Return)
                {
                    var edges = n.InEdges.FindAll(e => e.RightSidePin.PinName == previousName);
                    foreach (var edge in edges)
                    {
                        int idx = n.InEdges.IndexOf(edge);
                        var oldPort = n.InEdges[idx].RightSidePin;
                        var newPort = new BlueprintPinReference(fieldName, oldPort.NodeGuid, oldPort.IsExecutePin);
                        var newEdge = new BlueprintWireReference(n.InEdges[idx].LeftSidePin, newPort);
                        n.InEdges[idx] = newEdge;
                    }

                    if (!n.InputPortDataJson.EmptyOrNull())
                    {
                        n.InputPortDataJson = n.InputPortDataJson.Replace($"\"PortName\": \"{previousName}\",", $"\"PortName\": \"{fieldName}\",");
                    }
                }
            });
        }

        private void OnTempDataElementChanged(int index, string propertyName, object previousValue, object newValue)
        {
            Debug.Log($"OnTempDataElementChanged {index} - {propertyName} - {previousValue} - {newValue}");
            if (index == -1)
            {
                return;
            }

            if (propertyName.EmptyOrNull() || propertyName != "FieldName")
            {
                return;
            }

            var fieldName = TempData[index].FieldName;
            var previousName = TempData[index].PreviousName;
            BlueprintNodes.ForEach(n =>
            {
                // Works for Getters and Setters
                if (n.MethodName == previousName)
                {
                    n.MethodName = fieldName;
                    var edges = n.OutEdges.FindAll(e => e.LeftSidePin.PinName == previousName);
                    foreach (var edge in edges)
                    {
                        int idx = n.OutEdges.IndexOf(edge);
                        var oldPort = n.OutEdges[idx].LeftSidePin;
                        var newPort = new BlueprintPinReference(fieldName, oldPort.NodeGuid, oldPort.IsExecutePin);
                        var newEdge = new BlueprintWireReference(newPort, n.OutEdges[idx].RightSidePin);
                        n.OutEdges[idx] = newEdge;
                    }

                    var edgesIn = n.InEdges.FindAll(e => e.RightSidePin.PinName == previousName);
                    foreach (var edge in edgesIn)
                    {
                        int idx = n.InEdges.IndexOf(edge);
                        var oldPort = n.InEdges[idx].RightSidePin;
                        var newPort = new BlueprintPinReference(fieldName, oldPort.NodeGuid, oldPort.IsExecutePin);
                        var newEdge = new BlueprintWireReference(n.InEdges[idx].LeftSidePin, newPort);
                        n.InEdges[idx] = newEdge;
                    }
                }

                var edgeConnections = n.InEdges.FindAll(e => e.LeftSidePin.PinName == previousName);
                foreach (var edge in edgeConnections)
                {
                    int idx = n.InEdges.IndexOf(edge);
                    var oldPort = n.InEdges[idx].LeftSidePin;
                    var newPort = new BlueprintPinReference(fieldName, oldPort.NodeGuid, oldPort.IsExecutePin);
                    var newEdge = new BlueprintWireReference(newPort, n.InEdges[idx].RightSidePin);
                    n.InEdges[idx] = newEdge;
                }
            });
        }

        public void InitializedInDatabase()
        {
            Validate();
            Graph = new BlueprintFunctionGraph(this);
            RuntimeDataStore<IBlueprintGraph>.InitDatabase(RuntimeDatabase<BlueprintGraphSo>.Count);
        }

        public void PostInitializedInDatabase()
        {
            Debug.Log("Post Initialized Graph: " + Key);
            RuntimeDataStore<IBlueprintGraph>.Add(Key, Graph);
        }

        public void Validate()
        {
            var entry = BlueprintNodes.FirstOrDefault(x => x.NodeType == BlueprintNodeType.Entry);
            if (entry == null)
            {
                entry = BlueprintNodeDataModelUtility.CreateOrUpdateEntryNode(null, InputParameters);
                BlueprintNodes.Insert(0, entry);
            }
            else
            {
                BlueprintNodeDataModelUtility.CreateOrUpdateEntryNode(entry, InputParameters);
            }
            
            var ret = BlueprintNodes.FirstOrDefault(x => x.NodeType == BlueprintNodeType.Return);
            if (ret == null)
            {
                ret = BlueprintNodeDataModelUtility.CreateOrUpdateReturnNode(null, OutputParameters);
                BlueprintNodes.Add(ret);
            }
            else
            {
                BlueprintNodes.FindAll(x => x.NodeType == BlueprintNodeType.Return)
                    .ForEach(x => BlueprintNodeDataModelUtility.CreateOrUpdateReturnNode(x, OutputParameters));
            }
            
            BlueprintNodes.FindAll(x => x.NodeType == BlueprintNodeType.Getter)
                .ForEach(x =>
                {
                    var td = TempData.FirstOrDefault(td1 => td1.FieldName == x.MethodName);
                    if (td != null)
                    {
                        BlueprintNodeDataModelUtility.CreateOrUpdateGetterNode(x, td);
                    }
                });
            
            BlueprintNodes.FindAll(x => x.NodeType == BlueprintNodeType.Setter)
                .ForEach(x =>
                {
                    var td = TempData.FirstOrDefault(td => td.FieldName == x.MethodName);
                    if (td != null)
                    {
                        BlueprintNodeDataModelUtility.CreateOrUpdateSetterNode(x, td);
                    }
                });

            foreach (var n in BlueprintNodes)
            {
                n.Validate();
            }
        }

        public void Serialize()
        {
            foreach (var n in BlueprintNodes)
            {
                for (var i = n.InEdges.Count - 1; i >= 0; i--)
                {
                    var e = n.InEdges[i];
                    var leftNode = BlueprintNodes.FirstOrDefault(ln => ln.Guid == e.LeftSidePin.NodeGuid 
                                                                       && ln.OutEdges.Exists(oe => oe.LeftSidePin.PinName == e.LeftSidePin.PinName));
                    if (leftNode == null)
                    {
                        n.InEdges.RemoveAt(i);
                    }
                }

                for (var i = n.OutEdges.Count - 1; i >= 0; i--)
                {
                    var e = n.OutEdges[i];
                    var rightNode = BlueprintNodes.FirstOrDefault(rn => rn.Guid == e.RightSidePin.NodeGuid 
                                                                        && rn.InEdges.Exists(ie => ie.RightSidePin.PinName == e.RightSidePin.PinName));
                    if (rightNode == null)
                    {
                        n.OutEdges.RemoveAt(i);
                    }
                }
            }
            
            foreach (var n in BlueprintNodes)
            {
                n.Serialize();
            }
        }
    }
}