using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions{
	public static partial class LinqExtensions{
		public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source) => [source.Last()];
		public static IEnumerable<T> TakeLast<T>(this T[] source, int n) => source.Skip(Math.Max(0, source.Length - n));
		public static IEnumerable<T> TakeOrOriginal<T>(this IEnumerable<T> source, int count) 
			=> count > 0 ? source.Take(count) : source;
		
		public static IEnumerable<T> TakeAllButLast<T>(this IEnumerable<T> source) {
			using var it = source.GetEnumerator();
			bool hasRemainingItems;
			var isFirst = true;
			T item = default;
			do{
				hasRemainingItems = it.MoveNext();
				if (hasRemainingItems){
					if (!isFirst) yield return item;
					item = it.Current;
					isFirst = false;
				}
			} while (hasRemainingItems);
		}
	}
}