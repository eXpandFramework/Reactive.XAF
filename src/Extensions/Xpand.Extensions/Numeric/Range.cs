using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static IEnumerable<int> Range(this int start, int count)
            => Enumerable.Range(start, count);
    }
}