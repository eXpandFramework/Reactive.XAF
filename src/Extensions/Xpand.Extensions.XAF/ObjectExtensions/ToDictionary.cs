using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static Dictionary<object, T> ToBODictionary<T>(this IEnumerable<T> source) where T : IObjectSpaceLink 
            => source.ToDictionary(link => link.KeyValue(), link => link);

        public static Dictionary<TKey, TLink> ToDictionary<TKey,TLink>(this IEnumerable<TLink> source) where TLink : IObjectSpaceLink 
            => source.ToDictionary(link => link.KeyValue<TKey>(), link => link);

        public static Dictionary<string, TLink> ToStringDictionary<TLink>(this IEnumerable<TLink> source,
            IEqualityComparer<string> comparer=null) where TLink : IObjectSpaceLink 
            => source.ToDictionary(link => link.KeyValue<string>(), link => link,comparer);
        
        public static ConcurrentDictionary<object, T> ToBOConcurrentDictionary<T>(this IEnumerable<T> source) where T : IObjectSpaceLink 
            => source.ToConcurrentDictionary(link => link.KeyValue(), link => link);

        public static ConcurrentDictionary<TKey, TLink> ToConcurrentDictionary<TKey,TLink>(this IEnumerable<TLink> source) where TLink : IObjectSpaceLink 
            => source.ToConcurrentDictionary(link => link.KeyValue<TKey>(), link => link);
        
    }
}