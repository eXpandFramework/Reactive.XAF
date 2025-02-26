using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;

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

        public static string AsString(this Dictionary<string, (string oldValue, string newValue)> dictionary)
            => dictionary.ToDictionary(pair => pair.Key,
                pair => new { old = pair.Value.oldValue, newVal = pair.Value.newValue }).Serialize();
        
        public static string AsString(this Dictionary<string, (object oldValue, object newValue)> dictionary)
            => dictionary.ToDictionary(pair => pair.Key, pair => new { old = pair.Value.oldValue, newVal = pair.Value.newValue }).Serialize();

        public static Dictionary<string, (string oldValue, string newValue)> ToStringDictionary(
            this  Dictionary<string, (object oldValue, object newValue)> values)
            => values.ToDictionary(pair => pair.Key, pair => ($"{pair.Value.oldValue}", $"{pair.Value.newValue}"));

        public static Dictionary<string, (object oldValue, object newValue)> CompareTypeInfoValue(this IObjectSpaceLink instance, Dictionary<string, object> values) {
            var newValues = instance.MemberInfoValueDictionary();
            var changes = new Dictionary<string,(object oldValue,object newValue)>();
            return newValues.Keys.Select(key => {
                var newValue = newValues[key];
                values.TryGetValue(key, out var oldValue);
                if ((oldValue?.GetType().ToTypeInfo().IsDomainComponent ?? false)||
                    (newValue?.GetType().ToTypeInfo().IsDomainComponent ?? false)) {
                    oldValue=instance.ObjectSpace.GetObject(oldValue);
                }
                if (Comparer.Default.Compare(newValue, oldValue) != 0) {
                    changes[key] = (oldValue, newValue);
                }
                return changes;
            }).Distinct().LastOrDefault()??new();
        }
        
        public static Dictionary<string, object> MemberInfoValueDictionary(this IObjectSpaceLink project) 
            => project.ObjectSpace.IsNewObject(project)?new(): project.GetTypeInfo().Members
                .Where(info => !info.IsReadOnly && info.IsPublic && !info.IsService && info.IsPersistent)
                .ToDictionary(info => info.Name, info => info.GetValue(project));
    }
}