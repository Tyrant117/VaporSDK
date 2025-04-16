using System;

namespace Vapor.Blueprints
{
    [Serializable]
    public struct BlueprintPinDto
    {
        public string PinName;
        public Type PinType;
        public object Content;
    }
}