using System.Text.Json;
using System.Text.Json.Nodes;

namespace Xpand.Extensions.JsonExtensions {
    public static partial class JsonExtensions {
        public static byte[] Utf8Bytes(this JsonArray array, JsonSerializerOptions options = null)
            => JsonSerializer.SerializeToUtf8Bytes(array, options);
    }
}