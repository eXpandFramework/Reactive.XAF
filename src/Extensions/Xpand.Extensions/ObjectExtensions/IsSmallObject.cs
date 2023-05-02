using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xpand.Extensions.JsonExtensions;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static bool IsSmallObject(this object obj, int thresholdInBytes = 85000) {
            if (obj == null) return true;
            var jsonString = obj.Serialize();
            return Encoding.UTF8.GetByteCount(jsonString) - EstimateJsonOverhead(jsonString) < thresholdInBytes;
        }

        public static bool IsSmallObject(this JsonDocument document, int thresholdInBytes = 85000) {
            if (document == null) return true;
            using var memoryStream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(memoryStream)) {
                document.WriteTo(writer);
            }
            return (int)memoryStream.Length < thresholdInBytes;
        }

        private static int EstimateJsonOverhead(string jsonString) {
            var bracesCount = jsonString.Count(c => c is '{' or '}');
            var quotesCount = jsonString.Count(c => c == '\"');
            var colonsCount = jsonString.Count(c => c == ':');
            var commasCount = jsonString.Count(c => c == ',');
            return bracesCount + quotesCount + colonsCount + commasCount;
        }
    }
}