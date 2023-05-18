using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Xpand.Extensions.LinqExtensions{
	public static partial class LinqExtensions{
		public static TValue TryUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TValue, TValue> updateFactory) {
			while(dict.TryGetValue(key, out var curValue)) {
				if(dict.TryUpdate(key, updateFactory(curValue), curValue))
					return curValue;
			}
			return default;
		}

		public static TValue Update<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TValue, TValue> updateFactory)
			=> dict.TryUpdate(key, updateFactory);

		public static TValue Update<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Action<TValue> updateFactory)
			=> dict.TryUpdate(key, value => {
				updateFactory(value);
				return value;
			});
		
		public static bool TryGetValue<T>(this IList<T> array, int index, out T value){
			if (IsValidIndex(array, index)){
				value = array[index];
				return true;
			}
			value = default;
			return false;
		}
	}
}