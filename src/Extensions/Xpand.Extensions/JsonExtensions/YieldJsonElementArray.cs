using System.Collections.Generic;
using System.Text.Json;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.JsonExtensions {
    public static partial class JsonExtensions {
        public static IEnumerable<JsonElement> YieldJsonElementArray(this JsonElement jsonElement) 
            => jsonElement.ValueKind == JsonValueKind.Array ? jsonElement.EnumerateArrayUnboxed() : jsonElement.YieldItem();
    }
}