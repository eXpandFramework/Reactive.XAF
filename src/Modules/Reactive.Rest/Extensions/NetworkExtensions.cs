using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Rest.Extensions {
    internal static class NetworkExtensions {
        private static readonly Subject<(HttpResponseMessage message, string content,object instance)> ObjectSentSubject = new();
        public static IObservable<(HttpResponseMessage message, string content,object instance)> Object => ObjectSentSubject.AsObservable();
        static readonly Lazy<HttpClient> LayHttpClient=new(() => HttpClient);
        public static HttpClient HttpClient=new();

        static NetworkExtensions() {
            
        }

        static HttpRequestMessage Sign(this HttpRequestMessage requestMessage,string key,string secret) {
            if (key != null) {
                using HMACSHA256 hmac = new HMACSHA256(Encoding.ASCII.GetBytes(secret));
                var toSing = requestMessage.RequestUri.PathAndQuery;
                if (requestMessage.Method != HttpMethod.Get) {
                    toSing = $"{requestMessage.RequestUri.AbsolutePath}?{requestMessage.Content.ReadAsStringAsync().Result}";
                }
                var sign = BitConverter.ToString(hmac.ComputeHash(Encoding.ASCII.GetBytes(toSing))).Replace("-", "").ToLower();
                requestMessage.Headers.Add("Signature", sign);
                requestMessage.Headers.Add("APIKEY", key);
            }
            
            return requestMessage;
        }

        internal static IObservable<T> Send<T>(this HttpMethod httpMethod,string requestUrl,T obj,string key=null,string secret=null,Func<string,T[]> deserializeResponse=null,TimeSpan? pollInterval=null) where T : class {
            deserializeResponse ??= s => obj.GetType().Deserialize<T>(s);
            return httpMethod.ReturnObservable()
                .Poll(requestUrl, method => Observable.FromAsync(() => LayHttpClient.Value.SendAsync(method.NewHttpRequestMessage(obj, requestUrl, key, secret)))
                        .TraceRestModule(message => $"{message.RequestMessage.RequestUri.PathAndQuery}-{message.StatusCode}-{obj}")
                        .SelectMany(response => response.Content.ReadAsStringAsync().ToObservable().Select(s => new{response,json=s}))
                    ,pollInterval)
                .SelectMany(t => {
                    if (t.response.IsSuccessStatusCode) {
                        var objects = deserializeResponse(t.json).ToObservable(Scheduler.Immediate);
                        if (t.response.RequestMessage.Method != HttpMethod.Get && t.json == "true") {
                            objects = Observable.Empty<T>();
                        }
        
                        return objects.Do(obj1 => ObjectSentSubject.OnNext((t.response, t.json, obj1)));
                    }
        
                    return Observable.Throw<T>(new Exception(t.response.ToString()));
                })
                .TraceRestModule(arg => $"{arg.GetType()}-{arg}");
        }

        
        internal static T[] Deserialize<T>(this Type type, string json) => type.FromJson(json).Cast<T>().ToArray();

        private static readonly ConcurrentDictionary<object, IConnectableObservable<object>> CacheStorage = new();

        public static IObservable<T2> Poll<T,T2>(this IObservable<T> source,object key,Func<T,IObservable<T2>> secondSelector,TimeSpan? interval)  where T2:class 
            => source.SelectMany(message => {
                if (interval.HasValue) {
                    if (CacheStorage.TryGetValue(key, out var value)) {
                        return value.Select(o => o).FirstAsync().Cast<T2>().Finally(() => {});
                    }
                    var publish = Observable.Interval(interval.Value).StartWith(0).SelectMany(_ => secondSelector(message))
                        .Publish().RefCount().Replay(1);
                    publish.Connect();
                    CacheStorage.TryAdd(key, publish);
                    return publish.Select(o => o).FirstAsync().Cast<T2>().Finally(() => { });
                }
                return secondSelector(message);
            });



        private static HttpRequestMessage NewHttpRequestMessage(this HttpMethod httpMethod, object o,string requestUri, string key=null,string secret=null) 
            => new HttpRequestMessage(httpMethod,requestUri) {
                Content = httpMethod == HttpMethod.Get ? null : new StringContent(JsonConvert.SerializeObject(o), Encoding.UTF8, "application/json")
            }.Sign(key,secret);

        public static IObservable<IDictionary<TKey, TSource>> SampleByKey<TSource, TKey>(
            this IObservable<TSource> source,
            Func<TSource, TKey> keySelector,
            TimeSpan interval,
            IEqualityComparer<TKey> keyComparer = default)
        {
            return source
                .Scan(ImmutableDictionary.Create<TKey, TSource>(keyComparer),
                    (dict, x) => dict.SetItem(keySelector(x), x))
                .Publish(published => Observable
                    .Interval(interval)
                    .WithLatestFrom(published, (_, dict) => dict)
                    .TakeUntil(published.LastOrDefaultAsync()));
        }
    }
}