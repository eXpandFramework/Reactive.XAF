using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Xpand.Extensions.JsonExtensions{
    public class SafeDecimalConverter : JsonConverter<decimal> {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) 
            => reader.TokenType == JsonTokenType.Null ? 0m : reader.TokenType == JsonTokenType.String
                ? decimal.TryParse(reader.GetString(), out var value) ? value : 0m : reader.GetDecimal();
        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) 
            => writer.WriteNumberValue(value);
    }
}