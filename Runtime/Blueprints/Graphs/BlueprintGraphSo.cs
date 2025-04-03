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
        public string AssemblyQualifiedTypeName;
        [TypeSelector(TypeSelectorAttribute.T.Subclass, typeof(Component))]
        public string TypeTest;
        [HideLabel]
        public string GraphJson;
        // [HideInInspector] public string CompiledGraphJson;
        
        
        [Button]
        private void ResetGraph()
        {
            GraphJson = string.Empty;
            // CompiledGraphJson = string.Empty;
        }
        
        public IBlueprintGraph Graph { get; set; }
        // public BlueprintDesignGraph DesignGraph { get; private set; }

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

        #region - Design Graph -
        // public void OpenGraph()
        // {
        //     if (DesignGraphJson.EmptyOrNull())
        //     {
        //         DesignGraph = new BlueprintDesignGraph(this, new BlueprintDesignGraphDto());
        //         DesignGraph.Validate();
        //     }
        //     else
        //     {
        //         var dto = JsonConvert.DeserializeObject<BlueprintDesignGraphDto>(DesignGraphJson, NewtonsoftUtility.SerializerSettings);
        //         DesignGraph = new BlueprintDesignGraph(this, dto);
        //     }
        // }
        
        // public void SaveGraph()
        // {
        //     DesignGraph.Validate();
        //     DesignGraphJson = DesignGraph?.Serialize();
        // }
        #endregion
    }
}