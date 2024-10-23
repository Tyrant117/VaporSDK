using Newtonsoft.Json;
using UnityEngine;
using System;

namespace Vapor.VisualScripting
{
    public class RectConverter : JsonConverter<Rect>
    {
        public override void WriteJson(JsonWriter writer, Rect value, JsonSerializer serializer)
        {
            // Start writing the JSON object
            writer.WriteStartObject();

            // Write each property
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);

            writer.WritePropertyName("y");
            writer.WriteValue(value.y);

            writer.WritePropertyName("width");
            writer.WriteValue(value.width);

            writer.WritePropertyName("height");
            writer.WriteValue(value.height);

            // End writing the JSON object
            writer.WriteEndObject();
        }

        public override Rect ReadJson(JsonReader reader, Type objectType, Rect existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // Ensure we're reading an object
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException("Expected StartObject token.");

            float x = 0f, y = 0f, width = 0f, height = 0f;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string propertyName = (string)reader.Value;

                    reader.Read(); // Move to the value

                    switch (propertyName)
                    {
                        case "x":
                            x = Convert.ToSingle(reader.Value);
                            break;
                        case "y":
                            y = Convert.ToSingle(reader.Value);
                            break;
                        case "width":
                            width = Convert.ToSingle(reader.Value);
                            break;
                        case "height":
                            height = Convert.ToSingle(reader.Value);
                            break;
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    break;
                }
            }

            // Return the new Rect instance
            return new Rect(x, y, width, height);
        }
    }
}
