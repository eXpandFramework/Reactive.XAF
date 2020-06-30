using System.Collections;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions{
	public static partial class LinqExtensions{
		public static string Join(this IEnumerable source, string separator) => string.Join(separator, source.Cast<object>());
	}
}