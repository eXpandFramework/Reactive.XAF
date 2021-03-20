using System.Linq;
using System.Web;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static string ToQueryString(this object source) 
            => source.GetType()
                .GetProperties()
                .Where(p => p.GetValue(source, null) != null)
                .Select(p => p.Name + "=" + HttpUtility.UrlEncode(p.GetValue(source, null).ToString()))
                .Join("&");
    }
}