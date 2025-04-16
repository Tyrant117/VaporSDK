using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vapor.Blueprints;
using Vapor.NewtonsoftConverters;

namespace Vapor.Blueprints
{
    [Serializable]
    public struct BlueprintClassGraphDto
    {
        public bool IsObsolete;
        public string Namespace;
        public List<string> Usings;
        public Type ParentType;
        public BlueprintGraphSo ParentObject;
        public List<Type> ImplementedInterfaceTypes;
        public List<BlueprintVariableDto> Variables;
        public List<BlueprintMethodGraphDto> Methods;
        public static BlueprintClassGraphDto Load(string graphJson)
        {
            return JsonConvert.DeserializeObject<BlueprintClassGraphDto>(graphJson, NewtonsoftUtility.SerializerSettings);
        }
    }
}