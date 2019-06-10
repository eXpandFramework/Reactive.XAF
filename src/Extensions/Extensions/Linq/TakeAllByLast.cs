using System.Collections.Generic;

namespace Xpand.Source.Extensions.Linq{
    internal static partial class LinqExtensions{
        public static IEnumerable<T> TakeAllButLast<T>(this IEnumerable<T> source){
            using (var it = source.GetEnumerator()){
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
}