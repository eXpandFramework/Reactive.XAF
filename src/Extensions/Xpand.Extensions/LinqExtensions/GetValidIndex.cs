using System;
using System.Collections.Generic;

namespace Xpand.Extensions.LinqExtensions{
	public static partial class LinqExtensions{
		public static int GetValidIndex<T>(this IList<T> array, int index){
			return Math.Max(0, Math.Min(index, array.Count - 1));
		}
	}
}