using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
         
        public static string ToQueryString(this object source) 
            => source.ToPropertyValueDictionary(true)
                .Select(key => key.Key + "=" + HttpUtility.UrlEncode($"{key.Value}"))
                .Join("&");
    }
}