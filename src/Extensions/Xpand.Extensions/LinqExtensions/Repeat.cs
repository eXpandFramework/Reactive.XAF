using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions{
	public static partial class LinqExtensions{
		public static IEnumerable<T> Repeat<T>(this IEnumerable<T> source){
			for (;;)
				foreach (var item in source.ToArray())
					yield return item;
		}
	}
}