using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Xpand.Extensions.JsonExtensions{
    public class SafeIntConverter : JsonConverter<int> {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) 
            => reader.TokenType == JsonTokenType.Null ? 0 : reader.TokenType == JsonTokenType.String
                ? int.TryParse(reader.GetString(), out var value) ? value : 0 : reader.GetInt32();
        
        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) 
            => writer.WriteNumberValue(value);
    }
}