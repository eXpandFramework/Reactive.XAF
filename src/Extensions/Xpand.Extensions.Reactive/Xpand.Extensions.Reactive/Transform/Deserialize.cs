using System;
using System.Linq;
using System.Reactive.Linq;
using Newtonsoft.Json.Linq;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<JObject[]> Deserialize(this IObservable<string> source) 
            => source.Select(json => (json.Substring(0, 1) == "[" ? JArray.Parse(json).Children<JObject>() : JObject.Parse(json).YieldItem()).ToArray());
        public static IObservable<JObject> DeserializeObjects(this IObservable<string> source) 
            => source.Deserialize().SelectMany();
        
        public static IObservable<JObject> DeserializeObject(this IObservable<string> source) 
            => source.Select(JObject.Parse);
        
        public static IObservable<T[]> Deserialize<T>(this IObservable<string> source) 
            => source.Select(s => s.DeserializeJson<T>());
    }
}