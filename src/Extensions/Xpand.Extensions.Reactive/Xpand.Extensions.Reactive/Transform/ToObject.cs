using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<object> ToObject<T>(this IObservable<T> source) 
            => source.Select(z => (object)z);
        
        public static IObservable<TResult> ToObject<TResult>(this IObservable<JsonNode> source) 
            => source.Select(jToken => jToken.Deserialize<TResult>());
        
        public static IEnumerable<object> ToObject<T>(this IEnumerable<T> source) 
            => source.Select(_ => default(object));
    }
}