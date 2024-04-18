using System.Collections;
using System.Linq;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static string ToQueryString(this object source) 
            => ((IEnumerable)source.ToPropertyValueDictionary(true)
                .Select(key => key.Key.JoinString("=",key.Value?.ToString().UrlEncode()))
                .ToArray()).Join("&");
    }
}