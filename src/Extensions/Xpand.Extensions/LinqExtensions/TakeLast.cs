using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions{
	public static partial class LinqExtensions{
		public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source) => new []{source.Last()};
		public static IEnumerable<T> TakeLast<T>(this T[] source, int n) => source.Skip(Math.Max(0, source.Length - n));
	}
}