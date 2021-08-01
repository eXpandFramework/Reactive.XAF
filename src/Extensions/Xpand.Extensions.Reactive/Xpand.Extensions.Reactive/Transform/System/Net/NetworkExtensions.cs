using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.Transform.System.Net {
    public static class NetworkExtensions {
        private static readonly Subject<(HttpResponseMessage message, object instance)> ResponseSubject = new();
        private static readonly Subject<(HttpResponseMessage message, string content,object instance)> ObjectSentSubject = new();
        public static IObservable<(HttpResponseMessage message, string content,object instance)> Object => ObjectSentSubject.AsObservable();
        public static IObservable<(HttpResponseMessage message, object instance)> Responded => ResponseSubject.AsObservable();
        
        public static HttpClient HttpClient=new();

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

        public static IObservable<T> Send<T>(this HttpMethod httpMethod,string requestUrl,ConcurrentDictionary<object, IConnectableObservable<object>> cache,T obj,string key=null,string secret=null,Func<string,T[]> deserializeResponse=null,TimeSpan? pollInterval=null) where T : class 
            => httpMethod.ReturnObservable()
                .Cache(cache, requestUrl, method => Observable.FromAsync(() => HttpClient.SendAsync(method.NewHttpRequestMessage(obj, requestUrl, key, secret)))
                        .Do(message => ResponseSubject.OnNext((message, obj)))
                        .SelectMany(response => response.Content.ReadAsStringAsync().ToObservable().Select(s => new{response,json=s}))
                    ,pollInterval).Select(e => (e.json,e.response)).
                Send(obj,deserializeResponse);

        public static IObservable<T> Send<T>(this HttpMethod httpMethod,string requestUrl,T obj,string key=null,string secret=null,Func<string,T[]> deserializeResponse=null) where T : class 
            => Observable.FromAsync(() => HttpClient.SendAsync(httpMethod.NewHttpRequestMessage(obj, requestUrl, key, secret)))
                .Do(message => ResponseSubject.OnNext((message, obj)))
                .SelectMany(response => response.Content.ReadAsStringAsync().ToObservable().Select(s => new{response,json=s})).Select(e => (e.json,e.response)).
                Send(obj,deserializeResponse);

        private static IObservable<T> Send<T>(this IObservable<(string json, HttpResponseMessage response)> source,
            object obj, Func<string,T[]> deserializeResponse) {
            deserializeResponse ??= s => obj.GetType().Deserialize<T>(s);
            return source.SelectMany(t => {
                if (t.response.IsSuccessStatusCode)
                    if (t.response.RequestMessage.Method != HttpMethod.Get && t.json == "true") {
                        ObjectSentSubject.OnNext((t.response, t.json, obj));
                        return Observable.Empty<T>();
                    }
                    else
                        return deserializeResponse(t.json).ToObservable()
                            .Do(obj1 => ObjectSentSubject.OnNext((t.response, t.json, obj1))
                            );

                return Observable.Throw<T>(new Exception(t.response.ToString()));
            });
        }

        private static HttpRequestMessage NewHttpRequestMessage(this HttpMethod httpMethod, object o,string requestUri, string key=null,string secret=null) 
            => new HttpRequestMessage(httpMethod,requestUri) {
                Content = httpMethod == HttpMethod.Get ? null : new StringContent(o.Serialize(), Encoding.UTF8, "application/json")
            }.Sign(key,secret);

    }
}