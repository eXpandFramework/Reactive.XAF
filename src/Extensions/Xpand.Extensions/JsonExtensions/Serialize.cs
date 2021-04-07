using Newtonsoft.Json;

namespace Xpand.Extensions.JsonExtensions {
    public static partial class JsonExtensions {
        public static string Serialize(this object source) => JsonConvert.SerializeObject(source);
    }
}