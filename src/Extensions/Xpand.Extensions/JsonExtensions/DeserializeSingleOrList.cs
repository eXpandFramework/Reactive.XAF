using System;
using Newtonsoft.Json;

namespace Xpand.Extensions.JsonExtensions {
    public static partial class JsonExtensions {
        public static T[] DeserializeSingleOrList<T>(this JsonReader jsonReader) {
            if (jsonReader.Read()) {
                var jsonSerializer = new JsonSerializer();
                switch (jsonReader.TokenType) {
                    case JsonToken.StartArray:
                        return jsonSerializer.Deserialize<T[]>(jsonReader);
                    case JsonToken.StartObject:
                        return new [] { jsonSerializer.Deserialize<T>(jsonReader) };
                }
            }

            throw new InvalidOperationException("Unexpected JSON input");
        }
        public static object[] DeserializeSingleOrList(this JsonReader jsonReader,Type objectTpe) {
            if (jsonReader.Read()) {
                switch (jsonReader.TokenType) {
                    case JsonToken.StartArray:
                        return (object[]) new JsonSerializer().Deserialize(jsonReader,objectTpe.MakeArrayType());
                    case JsonToken.StartObject:
                        return new []{new JsonSerializer().Deserialize(jsonReader,objectTpe)};
                }
            }

            throw new InvalidOperationException("Unexpected JSON input");
        }
    }
}