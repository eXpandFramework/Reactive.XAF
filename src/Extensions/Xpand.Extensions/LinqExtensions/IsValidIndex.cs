using System.Collections.Generic;

namespace Xpand.Extensions.LinqExtensions{
	public static partial class LinqExtensions{
		public static bool IsValidIndex<T>(this IList<T> array, int index){
			return array != null && index >= 0 && index < array.Count;
		}
	}
}