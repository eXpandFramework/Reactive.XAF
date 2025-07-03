using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Xpand.Extensions.LinqExtensions{
    public static partial class LinqExtensions {
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueCreator,[CallerMemberName]string caller="") {
            if (!dictionary.TryGetValue(key, out var value)) {
                value = valueCreator();
                dictionary.Add(key, value);
            }
            return value;
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new() 
            => dictionary.GetOrAdd(key, () => new());

        public static IEnumerable<T> DoWhen<T>(this IEnumerable<T> source, Func<T,bool> when,Action<T> action) 
            => source.Do(obj => {
                if (when(obj)) {
                    action(obj);
                }
            });
        public static IEnumerable<T> DoWhenFirst<T>(this IEnumerable<T> source, Action<T> action) 
            => source.Select((arg1, i) => {
                if (i == 0) {
                    action(arg1);
                }

                return arg1;
            });
        public static IEnumerable<T> DoWhen<T>(this IEnumerable<T> source, Func<T,int,bool> when,Action<T> action) 
            => source.Select((arg1, i) => {
                if (when(arg1,i)) {
                    action(arg1);
                }

                return arg1;

            });
        
        public static T[] AddRange<T>(this IEnumerable<T> source,IEnumerable<T> enumerable,bool ignoreDuplicates=false) 
            => source is IList<T> list
                    ? enumerable.Where(arg => !ignoreDuplicates || !source.Contains(arg)).Execute(list.Add).ToArray()
                    : source.AddToArray(enumerable, ignoreDuplicates);
        
        public static T[] AddToArray<T>(this IEnumerable<T> source, T item, bool ignoreDuplicates = false) {
            var enumerable = source as T[] ?? source.ToArray();
            return !ignoreDuplicates && enumerable.Any(arg => arg.Equals(item))
                ? enumerable : enumerable.Concat(item.YieldItem()).ToArray();
        }

        public static T[] AddToArray<T>(this IEnumerable<T> source, IEnumerable<T> items, bool ignoreDuplicates = false) 
            => items.SelectMany(arg => source.AddToArray(arg, ignoreDuplicates)).ToArray();

        public static T Add<T>(this IList<T> source, T item, bool ignoreDuplicates = false) {
            if (!ignoreDuplicates || !source.Contains(item)) {
                source.Add(item);
                return item;
            }

            return default;
        }
            
    }
}