using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GltfGenerator
{
    public class ArrayOfTypeReferencesConverter : JsonConverter<IList<TypeReference>>
    {
        public override IList<TypeReference> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var singleItem = ReadItem(ref reader);
            if (singleItem != null)
            {
                return new[] { singleItem };
            }

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new ArgumentException("Unexpected token type " + reader.TokenType);
            }

            var tokens = new List<TypeReference>();
            reader.Read();
            while (reader.TokenType != JsonTokenType.EndArray)
            {
                singleItem = ReadItem(ref reader);
                if (singleItem != null)
                {
                    tokens.Add(singleItem);
                    reader.Read();
                    continue;
                }

                throw new ArgumentException("Unexpected token type " + reader.TokenType);
            }

            return tokens.ToArray();
        }

        private TypeReference ReadItem(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return new TypeReference { IsReference = false, Name = reader.GetString() };
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                return ReadObject(ref reader);
            }

            return null;
        }

        private TypeReference ReadObject(ref Utf8JsonReader reader)
        {
            reader.Read();

            if (reader.TokenType != JsonTokenType.PropertyName || (reader.GetString() != "$ref" && reader.GetString() != "type"))
            {
                throw new ArgumentException("Unexpected token type " + reader.TokenType);
            }

            var isRef = reader.GetString() == "$ref";

            reader.Read();
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new ArgumentException("Unexpected token type " + reader.TokenType);
            }

            var retval = new TypeReference
            {
                IsReference = isRef,
                Name = reader.GetString()
            };
            reader.Read();

            return retval;
        }

        public override bool CanConvert(Type type)
        {
            return typeof(IList<TypeReference>).IsAssignableFrom(type);
        }

        public override void Write(Utf8JsonWriter writer, IList<TypeReference> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
