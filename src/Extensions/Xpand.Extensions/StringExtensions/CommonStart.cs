using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string CommonStart(this IEnumerable<string> strings) {
            var namespaces = strings.Where(s => s != null).ToArray();
            return Enumerable.Range(0, namespaces.Min(s => s.Length))
                .Reverse()
                .Select(len => new {len, possibleMatch = namespaces.First().Substring(0, len)})
                .Where(t => namespaces.All(f => f.StartsWith(t.possibleMatch)))
                .Select(t => t.possibleMatch).First().Trim('.');
        }
    }
}