using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.StringExtensions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Xpand.Extensions.JsonExtensions {
    public static partial class JsonExtensions {
        public static IEnumerable<JsonNode> Descendants(this JsonNode root) => root.DescendantsAndSelf(false);

        public static IEnumerable<JsonNode> DescendantsAndSelf(this JsonNode root, bool includeSelf = true) 
            => root.DescendantItemsAndSelf(includeSelf).Select(i => i.node);
        
        public static IEnumerable<(JsonNode node, int? index, string name, JsonNode parent)> DescendantItemsAndSelf(this JsonNode root, bool includeSelf = true) => 
            (node: root, index: (int?)null, name: (string)null, parent: (JsonNode)null).Traverse((i) => i.node switch {
                    JsonObject o => o.AsDictionary().Select(p => (p.Value, (int?)null, p.Key.AsNullableReference(), i.node.AsNullableReference())),
                    JsonArray a => a.Select((item, index) => (item, index.AsNullableValue(), (string)null, i.node.AsNullableReference())),
                    _ => Enumerable.Empty<(JsonNode node, int? index, string name, JsonNode parent)>(),
                }, includeSelf);
        
        static T AsNullableReference<T>(this T item) where T : class => item;
        static T? AsNullableValue<T>(this T item) where T : struct => item;
        static IDictionary<string, JsonNode> AsDictionary(this JsonObject o) => o;
        public static JsonNode ToJsonNode(this object instance) => instance==null?null:JsonSerializer.SerializeToNode(instance);
        public static JsonDocument ToJsonDocument(this string json) => json.Bytes().ToJsonDocument();
        public static JsonDocument ToJsonDocument(this byte[] bytes) => bytes==null?null:JsonDocument.Parse(bytes);
        public static Task<JsonDocument> ToJsonDocumentAsync(this Stream stream) 
            => stream==null?null:JsonDocument.ParseAsync(stream);
        public static async Task<T> DeserializeJson<T>(this HttpResponseMessage message) 
            => (await message.Content.ReadAsByteArrayAsync()).Deserialize<T>();
        
        public static async Task<object[]> DeserializeJson(this HttpResponseMessage message,Type returnType) 
            => (await message.Content.ReadAsByteArrayAsync()).DeserializeJson(returnType).ToArray();

        public static JsonNode DeserializeJsonNode(this byte[] bytes,JsonSerializerOptions options=null) {
            var utf8Reader = new Utf8JsonReader(bytes);
            utf8Reader.Read();
            var isArray = utf8Reader.TokenType == JsonTokenType.StartArray;
            utf8Reader = new Utf8JsonReader(bytes);
            return !isArray ? JsonSerializer.Deserialize<JsonObject>(ref utf8Reader, options)!
                : new JsonArray(JsonSerializer.Deserialize<JsonObject[]>(ref utf8Reader, options)!.Cast<JsonNode>().ToArray()!);
        }

        public static T Deserialize<T>(this byte[] bytes,JsonSerializerOptions options=null) {
            var utf8Reader = new Utf8JsonReader(bytes);
            return JsonSerializer.Deserialize<T>(ref utf8Reader,options);
        }
        public static IEnumerable<object> DeserializeJson(this byte[] bytes,Type objectType,JsonSerializerOptions options=null) {
            var utf8Reader = new Utf8JsonReader(bytes);
            utf8Reader.Read();
            var needsFlatten = utf8Reader.TokenType == JsonTokenType.StartArray && !objectType.IsArray;
            return needsFlatten ? JsonSerializer.Deserialize(ref utf8Reader, objectType.MakeArrayType(), options).Cast<IEnumerable<object>>()
                : JsonSerializer.Deserialize(ref utf8Reader, objectType, options).YieldItem();
        }

        public static string Serialize<T>(this T value, JavaScriptEncoder encoder)
            => value.Serialize(new JsonSerializerOptions() { Encoder = encoder });
        
        public static string Serialize<T>(this T value,JsonSerializerOptions options=null)  
            => JsonSerializer.Serialize(value,options);
        public static JsonNode SerializeToNode(this object value,JsonSerializerOptions options=null)  
            => JsonSerializer.SerializeToNode(value,options);

        
        public static T Deserialize<T>(this string source, JsonSerializerOptions options = null)
            => source.Bytes().Deserialize<T>(options);

        public static JsonNode DeserializeJsonNode(this string source, JsonSerializerOptions options = null) 
            => source.Bytes().DeserializeJsonNode(options);

        public static bool IsJsonStartValid(this string source) {
            if (source.Length <= 0) return false;
            var substring = source.Substring(0,1);
            return new[]{"{","["}.Any(s => s==substring);
        }
        
        public static JsonArray ToJsonArray(this JsonNode node) 
            => node as JsonArray ?? new JsonArray(node);
        
        public static IEnumerable<JsonObject> ToJsonObjects(this JsonNode jsonNode) 
            => jsonNode.ToJsonArray().Cast<JsonObject>();

        
    }
}