using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static Dictionary<string, object> ToPropertyValueDictionary(this object source,bool skipDefaultValues=false,bool includeReadOnly=false) {
            if (source is IDictionary<string, object> dictionary) {
                return new Dictionary<string, object>(dictionary);
            }
            return source.GetType().GetProperties()
                .Where(info => !info.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any())
                .Where(info => info.GetCustomAttributes(typeof(BrowsableAttribute), false).Cast<BrowsableAttribute>()
                    .All(attribute => attribute.Browsable))
                .Where(info => !skipDefaultValues || !info.GetValue(source, null).IsDefaultValue())
                .Where(info => includeReadOnly || info.CanWrite)
                .ToDictionary(info => info.InfoName(), info => info.GetValue(source, null));
        }

        private static string InfoName(this PropertyInfo p){
            var attribute = p.GetCustomAttributes(typeof(JsonPropertyAttribute)).OfType<JsonPropertyAttribute>().FirstOrDefault();
            return attribute != null ? attribute.PropertyName : p.Name;
        }

    }
}