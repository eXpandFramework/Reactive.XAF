using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Xpand.Extensions.LinqExtensions{
	public static partial class LinqExtensions{
		public static TValue TryUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TValue, TValue> updateFactory) {
			// return dict.AddOrUpdate(key, key1 => throw new NotImplementedException(key1.EnsureStringEndWith("")),
				// (key1, value) => updateFactory(value));
			while(dict.TryGetValue(key, out var curValue)) {
				if(dict.TryUpdate(key, updateFactory(curValue), curValue))
					return curValue;
			}
			return default;
		}

		public static TValue Update<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key,
			Func<TValue, TValue> updateFactory)
			=> dict.TryUpdate(key, updateFactory);
			// => dict.AddOrUpdate(key, key1 => throw new NotImplementedException(new[]{caller,", ",key1.EnsureStringEndWith("")}.JoinConcat()),
			// 	(_, value) => updateFactory(value));

			public static TValue Update<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key,
				Action<TValue> updateFactory)
				=> dict.TryUpdate(key, value => {
					updateFactory(value);
					return value;
				});
				
			// => dict.AddOrUpdate(key, key1 => throw new NotImplementedException(key1.EnsureStringEndWith("")),
			// 	(_, value) => {
			// 		updateFactory(value);
			// 		return value;
			// 	});

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