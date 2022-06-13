using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Xpand.Extensions.JsonExtensions {
    
    public static partial class JsonExtensions {
        public static JToken ToJToken(this object instance) => instance==null?null:JToken.FromObject(instance);
        public static JToken DeserializeJson(this string json) => JToken.Parse(json);
        public static T[] Deserialize<T>(this Type type, string json) => type.Deserialize(json).Cast<T>().ToArray();
        public static T[] DeserializeJson<T>(this string json) {
            using var stringReader = new StringReader(json);
            using var reader = new JsonTextReader(stringReader);
            return reader.DeserializeSingleOrList<T>();
        }

        public static object[] Deserialize(this Type type,string json) {
            using var stringReader = new StringReader(json);
            using var reader = new JsonTextReader(stringReader);
            return reader.DeserializeSingleOrList(type);
        }
        
        public static Dictionary<string, object> DeserializeDictionary(this byte[] bytes) 
            => JsonConvert.DeserializeObject<Dictionary<string,object>>(Encoding.UTF8.GetString(bytes));
        public static JArray DeserializeArray(this byte[] bytes,string key) 
            => (JArray)JsonConvert.DeserializeObject<Dictionary<string,object>>(Encoding.UTF8.GetString(bytes))?[key];
    }
}