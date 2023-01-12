using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text;
using Fasterflect;
using Newtonsoft.Json.Linq;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Reactive.Transform.System.Net {
    public static class NetworkExtensions {
        public const HttpStatusCode TooManyRequests = HttpStatusCode.TooManyRequests;
        public const HttpStatusCode IpBanned = (HttpStatusCode)418;
        public const HttpStatusCode WafLimit = (HttpStatusCode)403;
        public const HttpStatusCode InternalError = HttpStatusCode.InternalServerError;
        private static readonly Subject<(HttpResponseMessage message, object instance)> ResponseSubject = new();
        private static readonly Subject<(HttpResponseMessage message, string content,object instance)> ObjectSentSubject = new();
        public static IObservable<(HttpResponseMessage message, string content,object instance)> Object => ObjectSentSubject.AsObservable();
        public static IObservable<(HttpResponseMessage message, object instance)> Responded => ResponseSubject.AsObservable();
        
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")] public static HttpClient HttpClient=new();

        public static TimeSpan RetryAfter(this HttpResponseMessage responseMessage){
            var dateTime = !responseMessage.Headers.Contains("Date") ? DateTime.Now
                : DateTimeOffset.Parse(responseMessage.Headers.GetValues("Date").First()).LocalDateTime;
            var retryAfterDelta = responseMessage.Headers.RetryAfter?.Delta;
            DateTime? delay = null;
            if (retryAfterDelta.HasValue){
                delay = dateTime.Add(retryAfterDelta.Value);    
            }

            var retryAfter = (responseMessage.StatusCode switch{
                WafLimit =>delay?? dateTime.AddMinutes(5),
                IpBanned => delay??dateTime.AddMinutes(20),
                TooManyRequests => delay??dateTime.AddSeconds(61),
                _ => DateTime.Now.AddSeconds(-1)
            }).Subtract(DateTime.Now);
            return retryAfter;
            
        }

        
        static T Sign<T>(this T requestMessage,string key,string secret) where T:HttpRequestMessage{
            if (key != null) {
                var toSing = requestMessage.RequestUri!.PathAndQuery;
                if (requestMessage.Method != HttpMethod.Get) {
                    toSing = $"{requestMessage.RequestUri.AbsolutePath}?{requestMessage.Content!.ReadAsStringAsync().Result}";
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
            where T : class ,new()
            => httpMethod.ReturnObservable()
                .Cache(cache, requestUrl, method => Observable.FromAsync(() => HttpClient.SendAsync(method.NewHttpRequestMessage( requestUrl,obj, key, secret)))
                        .Do(message => ResponseSubject.OnNext((message, obj)))
                        .SelectMany(response => response.Content.ReadAsStringAsync().ToObservable().Select(s => new{response,json=s}))
                    ,pollInterval).Select(e => (e.json,e.response)).
                Send(obj,deserializeResponse);

        public static IObservable<T> Send<T>(this HttpClient client, HttpMethod httpMethod, string requestUrl,
            object obj = null, string key = null, string secret = null, Func<string, T[]> deserializeResponse = null) where T : class,new()
            => Observable.FromAsync(() => client.SendAsync(httpMethod.NewHttpRequestMessage(requestUrl,obj, key, secret)))
                .Do(message => ResponseSubject.OnNext((message, obj)))
                .SelectMany(response => response.Content.ReadAsStringAsync().ToObservable()
                    .Select(s => new { response, json = s })).Select(e => (e.json, e.response))
                .Send(obj??typeof(T).CreateInstance(), deserializeResponse);
        
        public static IObservable<T> Send<T>(this HttpClient client, HttpMethod httpMethod, string requestUrl,
            object obj = null, string key = null, string secret = null, Func<string, IObservable<T>> deserializeResponse = null) where T : class,new()
            => Observable.FromAsync(() => client.SendAsync(httpMethod.NewHttpRequestMessage(requestUrl,obj, key, secret)))
                .Do(message => ResponseSubject.OnNext((message, obj)))
                .SelectMany(response => response.Content.ReadAsStringAsync().ToObservable()
                    .Select(s => new { response, json = s })).Select(e => (e.json, e.response))
                .WhenResponse(obj??typeof(T).CreateInstance(), deserializeResponse);

        public static IObservable<T> Send<T>(this HttpClient client, HttpRequestMessage httpRequestMessage) where T:class,new()
            => client.Send(httpRequestMessage, default(Func<string,T[]>));

        public static IObservable<T> Send<T>(this HttpClient client, HttpRequestMessage httpRequestMessage, Func<string, T[]> deserializeResponse ) where T:class,new()
            => Observable.FromAsync(() => client.SendAsync(httpRequestMessage,HttpCompletionOption.ResponseHeadersRead))
                .Do(message => ResponseSubject.OnNext((message, null)))
                .SelectMany(response => response.Content.ReadAsStringAsync().ToObservable().Select(s => (s, response)))
                .Send(typeof(T).CreateInstance(), deserializeResponse);
        
        public static IObservable<T> Send<T>(this HttpClient client, HttpRequestMessage httpRequestMessage, Func<string, IObservable<T>> deserializeResponse) where T:class,new()
            => Observable.FromAsync(() => client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead))
                .Do(message => ResponseSubject.OnNext((message, null)))
                .SelectMany(response => response.Content.ReadAsStringAsync().ToObservable().Select(s => (s, response)))
                .WhenResponse(CreateInstance<T>(), deserializeResponse);

        private static object CreateInstance<T>() => typeof(T) == typeof(string) ? "" : typeof(T).CreateInstance();

        public static IObservable<T> SendRequest<T>(this HttpClient client, HttpRequestMessage httpRequestMessage, 
            T obj = null, Func<string, T[]> deserializeResponse = null, [CallerMemberName] string caller = "") where T : class,new()
            => Observable.FromAsync(() => client.SendAsync(httpRequestMessage,HttpCompletionOption.ResponseHeadersRead))
                .Do(message => ResponseSubject.OnNext((message, obj)))
                .SelectMany(response => response.Content.ReadAsStringAsync().ToObservable()
                    .Select(s => new { response, json = s })).Select(e => (e.json, e.response))
                .SendRequest(obj??typeof(T).CreateInstance(), deserializeResponse);
        public static IObservable<JObject> SendRequest(this HttpClient client, HttpRequestMessage httpRequestMessage) 
            => client.SendRequest(httpRequestMessage,default,s => s.DeserializeJson().ToJObject().ToArray());
        
        public static IObservable<T> SendRequest<T>(this HttpClient client, HttpMethod httpMethod,string url, 
            T obj = null, Func<string, T[]> deserializeResponse = null) where T : class,new()
            => client.SendRequest(new HttpRequestMessage(httpMethod, url),obj,deserializeResponse);
        
        public static IObservable<JObject> SendRequest(this HttpClient client, HttpMethod httpMethod,string url) 
            => client.SendRequest<JObject>(new HttpRequestMessage(httpMethod, url));
        
        public static IObservable<JObject> Send(this HttpClient client, string url) 
            => client.SendRequest<JObject>(new HttpRequestMessage(HttpMethod.Get, url));

        public static IObservable<T> Send<T>(this HttpMethod httpMethod, string requestUrl, string key = null,
            string secret = null, Func<string, IObservable<T>> deserializeResponse = null) where T : class,new() 
            => HttpClient.Send(httpMethod, requestUrl,null,key,secret,deserializeResponse);
        
        public static IObservable<T> Send<T>(this HttpMethod httpMethod, string requestUrl, Func<string, IObservable<T>> deserializeResponse = null
        , string key = null, string secret = null) where T : class,new() 
            => HttpClient.Send(httpMethod, requestUrl,null,key,secret,deserializeResponse);

        private static IObservable<T> SendRequest<T>(this IObservable<(string json, HttpResponseMessage response)> source,
            object obj, Func<string,T[]> deserializeResponse) {
            deserializeResponse ??= s =>obj is IJsonFactory factory?new[] { (T)factory.FromJson(s) }: obj.GetType().Deserialize<T>(s);
            return source.WhenResponse(obj, s => deserializeResponse(s).ToNowObservable());
        }
        private static IObservable<T> Send<T>(this IObservable<(string json, HttpResponseMessage response)> source,
            object obj, Func<string,T[]> deserializeResponse) where T:class,new(){
            deserializeResponse ??= s =>obj is IJsonFactory factory?new[] { (T)factory.FromJson(s) }: obj.GetType().Deserialize<T>(s);
            return source.WhenResponse(obj, s => deserializeResponse(s).ToNowObservable());
        }
        private static IObservable<T> WhenResponse<T>(this IObservable<(string json, HttpResponseMessage response)> source,
            object obj, Func<string,IObservable<T>> deserializeResponse) {
            deserializeResponse ??= s => (obj is IJsonFactory factory ? new[] { (T)factory.FromJson(s) } : obj.GetType().Deserialize<T>(s)).ToNowObservable();
            return source.SelectMany(t => {
                if (t.response.IsSuccessStatusCode) {
                    if (t.response.RequestMessage!.Method != HttpMethod.Get && t.json == "true") {
                        ObjectSentSubject.OnNext((t.response, t.json, obj));
                        return Observable.Empty<T>();
                    }
                    return deserializeResponse(t.json)
                        .Do(obj1 => ObjectSentSubject.OnNext((t.response, t.json, obj1)));
                }

                return t.response.Content.ReadAsStringAsync().ToObservable()
                    .SelectMany(s => Observable.Throw<T>(new HttpResponseException(s, t.response)));

            });
        }

        public static T SetContent<T>(this T message,  string content, string key = null, string secret = null,bool formDataContent=false) where T:HttpRequestMessage {
            if (message.Method != HttpMethod.Get&&!string.IsNullOrEmpty(content) )
                message.Content = new StringContent(content, Encoding.UTF8, formDataContent ? "application/x-www-form-urlencoded" : "application/json");
            
            return message.Sign(key, secret);
        }

        public static HttpRequestMessage NewHttpRequestMessage(this HttpMethod httpMethod, string requestUri,object o=null, string key=null,string secret=null) 
            => new HttpRequestMessage(httpMethod,requestUri).SetContent(o.Serialize(),key,secret);
        
    }

    public class HttpResponseException:HttpRequestException {
        public HttpResponseMessage HttpResponseMessage{ get; }

        public HttpResponseException(string paramName, HttpResponseMessage httpResponseMessage) : base(paramName) => HttpResponseMessage = httpResponseMessage;
    }

    public interface IJsonFactory{
        object FromJson(string json);
    }
}