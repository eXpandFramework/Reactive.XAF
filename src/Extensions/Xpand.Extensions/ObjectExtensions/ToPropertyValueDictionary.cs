using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static Dictionary<object, object> ToPropertyValueDictionary(this object source,bool skipDefaultValues=false,bool includeReadOnly=false) 
            => source is IDictionary dictionary ? Enumerable.Cast<object>(dictionary.Keys).ToDictionary(o => o, o => dictionary[o])
                : source.GetType().GetProperties().Where(info => !info.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any())
                    .Where(info => Enumerable.Cast<BrowsableAttribute>(info.GetCustomAttributes(typeof(BrowsableAttribute), false))
                        .All(attribute => attribute.Browsable))
                    .Where(info => !skipDefaultValues || !info.GetValue(source, null).IsDefaultValue())
                    .Where(info => includeReadOnly || info.CanWrite)
                    .ToDictionary(info => (object)info.Name, info => info.GetValue(source, null));
    }
}