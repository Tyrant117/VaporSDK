using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;
using Vapor.Inspector;
using Vapor.Keys;
using Vapor.NewtonsoftConverters;

namespace Vapor.Blueprints
{
    // [Serializable]
    // public struct BlueprintCompiledClassGraphDto
    // {
    //     public List<BlueprintVariableDto> Variables;
    //     public List<BlueprintCompiledMethodGraphDto> Methods;
    // }
    
    // [Serializable]
    // public struct BlueprintCompiledMethodGraphDto
    // {
    //     public List<BlueprintVariableDto> InputArguments;
    //     public List<BlueprintVariableDto> OutputArguments;
    //     public List<BlueprintVariableDto> TemporaryVariables;
    //     public List<BlueprintCompiledNodeDto> Nodes;
    // }

    [DatabaseKeyValuePair, KeyOptions(includeNone: false, category: "Graphs")]
    public class BlueprintGraphSo : NamedKeySo, IDatabaseInitialize
    {
        public enum BlueprintGraphType
        {
            BehaviourGraph,
            ClassGraph,
        }

        public BlueprintGraphType GraphType;
        [HideInInspector]
        public string ParentType;
        [HideInInspector]
        public BlueprintGraphSo ParentObject;
        
        [HideLabel]
        public string GraphJson;
        
        [Button]
        private void ResetGraph()
        {
            GraphJson = string.Empty;
        }
        
        public IBlueprintGraph Graph { get; set; }

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
    }
}