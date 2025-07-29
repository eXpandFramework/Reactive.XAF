using System.Runtime.CompilerServices;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static string PrefixCaller(this object value, [CallerMemberName] string caller = "")
            => new[]{$"{value}",caller}.WhereNotNullOrEmpty().JoinCommaSpace();
        public static string SuffixCaller(this object value, [CallerMemberName] string caller = "")
            => new[]{caller,$"{value}"}.WhereNotNullOrEmpty().JoinCommaSpace();
    }
}