using System;

namespace Vapor.Blueprints
{
    [Serializable]
    public struct BlueprintWireDto
    {
        public string Guid;
        public bool IsExecuteWire;
        
        public string LeftGuid;
        public string LeftName;

        public string RightGuid;
        public string RightName;
    }
}