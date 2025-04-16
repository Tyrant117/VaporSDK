using System;
using System.Collections.Generic;

namespace Vapor.Blueprints
{
    [Serializable]
    public struct BlueprintMethodGraphDto
    {
        public bool IsTypeOverride;
        public bool IsBlueprintOverride;
        public bool IsUnityOverride;
        public Type MethodDeclaringType;
        public string MethodName;
        public string[] MethodParameters;
        public List<BlueprintArgumentDto> Arguments;
        public List<BlueprintVariableDto> Variables;
        public List<BlueprintDesignNodeDto> Nodes;
        public List<BlueprintWireDto> Wires;
    }
}