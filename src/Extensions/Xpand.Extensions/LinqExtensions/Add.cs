using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.LinqExtensions{
    public static partial class LinqExtensions{
        public static IEnumerable<T> DoWhen<T>(this IEnumerable<T> source, Func<T,bool> when,Action<T> action) 
            => source.Do(obj => {
                if (when(obj)) {
                    action(obj);
                }
            });
        public static IEnumerable<T> ExactType<T>(this IEnumerable<T> source) 
            => source.Where(arg => arg.GetType()==typeof(T));

        public static T[] AddRange<T>(this IEnumerable<T> source,IEnumerable<T> enumerable,bool ignoreDuplicates=false) 
            => source is IList<T> list
                    ? enumerable.Where(arg => !ignoreDuplicates || !source.Contains(arg)).Execute(list.Add).ToArray()
                    : source.AddToArray(enumerable, ignoreDuplicates);

        public static void Add(this IList source, object item, bool ignoreDuplicates=false) {
            if (!ignoreDuplicates || !source.Contains(item)) {
                source.Add(item);
            }
        }

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