using System.Linq;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static bool ContainsAny(this string input, params string[] values)
            => values.WhereNotNullOrEmpty().Any(input.Contains);
        
        public static bool ContainsAll(this string input, params string[] values)
            => values.WhereNotNullOrEmpty().All(input.Contains);
    }
}