using System.Collections.Generic;

namespace Xpand.Extensions.LinqExtensions{
	public static partial class LinqExtensions{
		public static bool TryGetValue<T>(this IList<T> array, int index, out T value){
			if (IsValidIndex(array, index)){
				value = array[index];
				return true;
			}

			value = default(T);
			return false;
		}
	}
}