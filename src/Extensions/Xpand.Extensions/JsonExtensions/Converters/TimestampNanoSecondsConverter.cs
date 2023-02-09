using System;
using Newtonsoft.Json;

namespace Xpand.Extensions.JsonExtensions.Converters{
    public class TimestampNanoSecondsConverter : JsonConverter{
        private const decimal TicksPerNanosecond = TimeSpan.TicksPerMillisecond / 1000m / 1000;
        public override bool CanConvert(Type objectType) => objectType == typeof(DateTime);
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) 
            => reader.Value != null ? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(
                (long)Math.Round(long.Parse(reader.Value.ToString()!) * TicksPerNanosecond)) : null;
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) 
            => writer.WriteValue((long)Math.Round(((DateTime)value! - new DateTime(1970, 1, 1)).Ticks / TicksPerNanosecond));
    }
}