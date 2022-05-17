using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using Fasterflect;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Reactive.Transform.System.Net {
    public static class NetworkExtensions {
        private static readonly Subject<(HttpResponseMessage message, object instance)> ResponseSubject = new();
        private static readonly Subject<(HttpResponseMessage message, string content,object instance)> ObjectSentSubject = new();
        public static IObservable<(HttpResponseMessage message, string content,object instance)> Object => ObjectSentSubject.AsObservable();
        public static IObservable<(HttpResponseMessage message, object instance)> Responded => ResponseSubject.AsObservable();
        
        public static HttpClient HttpClient=new();

        static T Sign<T>(this T requestMessage,string key,string secret) where T:HttpRequestMessage{
            if (key != null) {
                var toSing = requestMessage.RequestUri.PathAndQuery;
                if (requestMessage.Method != HttpMethod.Get) {
                    toSing = $"{requestMessage.RequestUri.AbsolutePath}?{requestMessage.Content.ReadAsStringAsync().Result}";
                }
                var sign = secret.Bytes().CryptoSign(toSing).Replace("-", "").ToLower();
                requestMessage.Headers.Add("Signature", sign);
                requestMessage.Headers.Add("APIKEY", key);
            }
            return requestMessage;
        }

        public static IObservable<T> Send<T>(this HttpMethod httpMethod, string requestUrl,
            ConcurrentDictionary<object, IConnectableObservable<object>> cache, T obj, string key = null,
            string secret = null, Func<string, T[]> deserializeResponse = null, TimeSpan? pollInterval = null)
            where T : class 
            => httpMethod.ReturnObservable()
                .Cache(cache, requestUrl, method => Observable.FromAsync(() => HttpClient.SendAsync(method.NewHttpRequestMessage( requestUrl,obj, key, secret)))
                        .Do(message => ResponseSubject.OnNext((message, obj)))
                        .SelectMany(response => response.Content.ReadAsStringAsync().ToObservable().Select(s => new{response,json=s}))
                    ,pollInterval).Select(e => (e.json,e.response)).
                Send(obj,deserializeResponse);

        public static IObservable<T> Send<T>(this HttpClient client, HttpMethod httpMethod, string requestUrl,
            object obj = null, string key = null, string secret = null, Func<string, T[]> deserializeResponse = null) where T : class
            => Observable.FromAsync(() => client.SendAsync(httpMethod.NewHttpRequestMessage(requestUrl,obj, key, secret)))
                .Do(message => ResponseSubject.OnNext((message, obj)))
                .SelectMany(response => response.Content.ReadAsStringAsync().ToObservable()
                    .Select(s => new { response, json = s })).Select(e => (e.json, e.response))
                .Send(obj??typeof(T).CreateInstance(), deserializeResponse);
        
        public static IObservable<T> Send<T>(this HttpClient client, HttpMethod httpMethod, string requestUrl,
            object obj = null, string key = null, string secret = null, Func<string, IObservable<T>> deserializeResponse = null) where T : class
            => Observable.FromAsync(() => client.SendAsync(httpMethod.NewHttpRequestMessage(requestUrl,obj, key, secret)))
                .Do(message => ResponseSubject.OnNext((message, obj)))
                .SelectMany(response => response.Content.ReadAsStringAsync().ToObservable()
                    .Select(s => new { response, json = s })).Select(e => (e.json, e.response))
                .Send(obj??typeof(T).CreateInstance(), deserializeResponse);
        
        public static IObservable<T> Send<T>(this HttpClient client, HttpRequestMessage httpRequestMessage, 
            object obj = null, Func<string, T[]> deserializeResponse = null) 
            => Observable.FromAsync(() => client.SendAsync(httpRequestMessage,HttpCompletionOption.ResponseHeadersRead))
                .Do(message => ResponseSubject.OnNext((message, obj)))
                .SelectMany(response => response.Content.ReadAsStringAsync().ToObservable().Select(s => (s, response)))
                .Send(obj??typeof(T).CreateInstance(), deserializeResponse);
        
        public static IObservable<T> Send<T>(this HttpClient client, HttpRequestMessage httpRequestMessage, 
            object obj = null, Func<string, IObservable<T>> deserializeResponse = null) {
            if (deserializeResponse == null) {
                
            }
            return Observable.FromAsync(() =>
                    client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead))
                .Do(message => ResponseSubject.OnNext((message, obj)))
                .SelectMany(response => response.Content.ReadAsStringAsync().ToObservable().Select(s => (s, response)))
                .Send(obj ?? typeof(T).CreateInstance(), deserializeResponse);
        }

        public static IObservable<(T instance, HttpResponseMessage message)> SendRequest<T>(this HttpClient client, HttpRequestMessage httpRequestMessage, 
            T obj = null, Func<string, T[]> deserializeResponse = null) where T : class
            => Observable.FromAsync(() => client.SendAsync(httpRequestMessage,HttpCompletionOption.ResponseHeadersRead))
                .Do(message => ResponseSubject.OnNext((message, obj)))
                .SelectMany(response => response.Content.ReadAsStringAsync().ToObservable()
                    .Select(s => new { response, json = s })).Select(e => (e.json, e.response))
                .SendRequest(obj??typeof(T).CreateInstance(), deserializeResponse);

        public static IObservable<T> Send<T>(this HttpMethod httpMethod, string requestUrl, T obj, string key = null,
            string secret = null, Func<string, T[]> deserializeResponse = null) where T : class 
            => HttpClient.Send(httpMethod, requestUrl,obj,key,secret,deserializeResponse);

        private static IObservable<(T,HttpResponseMessage message)> SendRequest<T>(this IObservable<(string json, HttpResponseMessage response)> source,
            object obj, Func<string,T[]> deserializeResponse) {
            deserializeResponse ??= s =>obj is IJsonFactory factory?new[] { (T)factory.FromJson(s) }: obj.GetType().Deserialize<T>(s);
            return source.SelectMany(t => {
                if (t.response.IsSuccessStatusCode)
                    if (t.response.RequestMessage.Method != HttpMethod.Get && t.json == "true") {
                        ObjectSentSubject.OnNext((t.response, t.json, obj));
                        return Observable.Empty< (T,HttpResponseMessage)>();
                    }
                    else
                        return deserializeResponse(t.json).ToNowObservable()
                            .Do(obj1 => ObjectSentSubject.OnNext((t.response, t.json, obj1)))
                            .Select(arg => (arg,t.response));

                return Observable.Throw< (T,HttpResponseMessage)>(new HttpResponseException(nameof(t.response.StatusCode),t.response));
            });
        }
        private static IObservable<T> Send<T>(this IObservable<(string json, HttpResponseMessage response)> source,
            object obj, Func<string,T[]> deserializeResponse) {
            deserializeResponse ??= s =>obj is IJsonFactory factory?new[] { (T)factory.FromJson(s) }: obj.GetType().Deserialize<T>(s);
            return source.SelectMany(t => {
                if (t.response.IsSuccessStatusCode)
                    if (t.response.RequestMessage.Method != HttpMethod.Get && t.json == "true") {
                        ObjectSentSubject.OnNext((t.response, t.json, obj));
                        return Observable.Empty<T>();
                    }
                    else
                        return deserializeResponse(t.json).ToNowObservable()
                            .Do(obj1 => ObjectSentSubject.OnNext((t.response, t.json, obj1)));

                return Observable.Throw<T>(new HttpResponseException(nameof(t.response.StatusCode),t.response));
            });
        }
        private static IObservable<T> Send<T>(this IObservable<(string json, HttpResponseMessage response)> source,
            object obj, Func<string,IObservable<T>> deserializeResponse) {
            deserializeResponse ??= s => (obj is IJsonFactory factory ? new[] { (T)factory.FromJson(s) } : obj.GetType().Deserialize<T>(s)).ToNowObservable();
            return source.SelectMany(t => {
                if (t.response.IsSuccessStatusCode)
                    if (t.response.RequestMessage.Method != HttpMethod.Get && t.json == "true") {
                        ObjectSentSubject.OnNext((t.response, t.json, obj));
                        return Observable.Empty<T>();
                    }
                    else
                        return deserializeResponse(t.json)
                            .Do(obj1 => ObjectSentSubject.OnNext((t.response, t.json, obj1)));

                return Observable.Throw<T>(new HttpResponseException(nameof(t.response.StatusCode),t.response));
            });
        }

        public static T SetContent<T>(this T message,  object o , string key = null, string secret = null) where T:HttpRequestMessage{
            message.Content = message.Method != HttpMethod.Get
                ? new StringContent(o.Serialize(), Encoding.UTF8, "application/json")
                : null;
            return message.Sign(key, secret);
        }

        public static HttpRequestMessage NewHttpRequestMessage(this HttpMethod httpMethod, string requestUri,object o=null, string key=null,string secret=null) 
            => new HttpRequestMessage(httpMethod,requestUri).SetContent(o,key,secret);

    }

    public class HttpResponseException:HttpRequestException {
        public HttpResponseMessage HttpResponseMessage{ get; }

        public HttpResponseException(string paramName, HttpResponseMessage httpResponseMessage) : base(paramName) {
            HttpResponseMessage = httpResponseMessage;
        }
    }

    public interface IJsonFactory{
        object FromJson(string json);
    }
}