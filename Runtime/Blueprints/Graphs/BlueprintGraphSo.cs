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

    [DatabaseKeyValuePair, KeyOptions(includeNone: true, category: "Blueprints")]
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
        [HideInInspector]
        public long Version;
        [HideInInspector] 
        public string LastOpenedMethod;
        
        [HideLabel]
        public string GraphJson;
        
        [Button]
        private void ResetGraph()
        {
            GraphJson = string.Empty;
            LastOpenedMethod = string.Empty;
            Version = 0;
        }

        public void InitializedInDatabase()
        {
            RuntimeDataStore<BlueprintTypeContainer>.InitDatabase(100);
        }

        public void PostInitializedInDatabase()
        {
            var type = Type.GetType("MyAssemblyQualifiedType");
            RuntimeDataStore<BlueprintTypeContainer>.Add(Key, new BlueprintTypeContainer { Type = type });
        }
    }

    public struct BlueprintTypeContainer
    {
        public Type Type;
    }
}