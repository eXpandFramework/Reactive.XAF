using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Newtonsoft.Json.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<object> ToObject<T>(this IObservable<T> source) 
            => source.Select(_ => default(object));
        
        public static IObservable<TResult> ToObject<TResult>(this IObservable<JToken> source) 
            => source.Select(jToken => jToken.ToObject<TResult>());
        
        public static IEnumerable<object> ToObject<T>(this IEnumerable<T> source) 
            => source.Select(_ => default(object));
    }
}