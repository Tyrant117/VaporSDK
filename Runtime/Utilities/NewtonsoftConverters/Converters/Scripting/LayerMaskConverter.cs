using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using UnityEngine;

namespace Vapor.NewtonsoftConverters
{
    public class LayerMaskConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(LayerMask) || objectType == typeof(LayerMask?);
        }

        [return: MaybeNull]
        public override object ReadJson(JsonReader reader, Type objectType, [AllowNull] object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return objectType == typeof(LayerMask?)
                    ? (LayerMask?)null
                    : new LayerMask();
            }

            if (reader.TokenType == JsonToken.Integer)
            {
                return new LayerMask {
                    value = GetInt(reader.Value)
                };
            }

            throw new JsonReaderException($"Unexpected token when reading LayerMask. Expected 'null' or 'integer', got '{reader.TokenType}'.");
        }

        private static int GetInt(object value)
        {
            switch (value)
            {
                case int i: return i;
                case uint ui: return checked((int)ui);
                case long l: return checked((int)l);
                case ulong ul: return checked((int)ul);
                default: return 0;
            }
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] object value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
            }
            else
            {
                var layerMask = (LayerMask)value;
                writer.WriteValue(layerMask.value);
            }
        }
    }
    
    public class RenderingLayerMaskConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(RenderingLayerMask) || objectType == typeof(RenderingLayerMask?);
        }

        [return: MaybeNull]
        public override object ReadJson(JsonReader reader, Type objectType, [AllowNull] object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return objectType == typeof(RenderingLayerMask?)
                    ? (RenderingLayerMask?)null
                    : new RenderingLayerMask();
            }

            if (reader.TokenType == JsonToken.Integer)
            {
                return new RenderingLayerMask {
                    value = GetInt(reader.Value)
                };
            }

            throw new JsonReaderException($"Unexpected token when reading RenderingLayerMask. Expected 'null' or 'integer', got '{reader.TokenType}'.");
        }

        private static uint GetInt(object value)
        {
            return value switch
            {
                int i => (uint)i,
                uint ui => ui,
                long l => checked((uint)l),
                ulong ul => checked((uint)ul),
                _ => 0
            };
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] object value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
            }
            else
            {
                var layerMask = (RenderingLayerMask)value;
                writer.WriteValue(layerMask.value);
            }
        }
    }
    
    public class Hash128Converter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Hash128) || objectType == typeof(Hash128?);
        }

        [return: MaybeNull]
        public override object ReadJson(JsonReader reader, Type objectType, [AllowNull] object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return objectType == typeof(Hash128?)
                    ? (Hash128?)null
                    : new Hash128();
            }

            if (reader is { TokenType: JsonToken.String, Value: string stringValue })
            {
                return Hash128.Parse(stringValue);
            }

            throw new JsonReaderException($"Expected string when reading UnityEngine.Hash128 type, got '{reader.TokenType}' <{reader.Value}>.");
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] object value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(((Hash128)value).ToString());
            }
        }
    }
}
