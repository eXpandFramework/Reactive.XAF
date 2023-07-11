using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions{
	public static partial class LinqExtensions{
		[SuppressMessage("ReSharper", "IteratorNeverReturns")]
		public static IEnumerable<T> Repeat<T>(this IEnumerable<T> source) {
			var array = source as T[] ?? source.ToArray();
			for (;;)
				foreach (var item in array.ToArray())
					yield return item;
		}
	}
}