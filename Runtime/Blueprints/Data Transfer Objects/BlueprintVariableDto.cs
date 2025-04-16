using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Vapor.Blueprints
{
    [Serializable]
    public struct BlueprintVariableDto
    {
        public string Id;
        public string VariableName;
        public string DisplayName;
        public Type Type;
        public VariableScopeType Scope;
        public VariableAccessModifier AccessModifier;
        public bool IsProperty;
        
        // Variable Constructions
        public string ConstructorName;
        public List<(Type, object)> DefaultParametersValue;
    }
}