using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace VaporEditor.Graphs
{
    public class FieldsOnlyContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            // check member types
            if (member.MemberType == MemberTypes.Property)
            {
                property.ShouldSerialize = _ => false; // Ignore all properties
            }

            return property;
        }
    }
}
