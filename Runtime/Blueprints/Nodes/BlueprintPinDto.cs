using System;
using System.Collections.Generic;

namespace Vapor.Blueprints
{
    [Serializable]
    public struct BlueprintPinDto
    {
        public string PinName;
        public Type PinType;
        public object Content;
        public List<string> WireGuids;
    }
}