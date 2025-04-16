using System;

namespace Vapor.Blueprints
{
    [Serializable]
    public struct BlueprintArgumentDto
    {
        public Type Type;
        public string ParameterName;
        public string DisplayName;
        public int ParameterIndex;
        public bool IsRef;
        public bool IsReturn;
        public bool IsOut;
    }
}