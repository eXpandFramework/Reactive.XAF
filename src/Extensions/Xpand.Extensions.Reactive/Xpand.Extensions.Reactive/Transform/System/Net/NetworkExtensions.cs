using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fasterflect;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Network;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform.System.Text.Json;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StreamExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Reactive.Transform.System.Net {
    public static class NetworkExtensions {
        public static readonly HttpStatusCode[] WorthRetryingCodes = [
            HttpStatusCode.TooManyRequests, IpBanned, WafLimit, HttpStatusCode.BadGateway, HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized, HttpStatusCode.RequestTimeout,HttpStatusCode.InternalServerError,HttpStatusCode.ServiceUnavailable, 
            HttpStatusCode.GatewayTimeout
        ];
        public const HttpStatusCode TooManyRequests = HttpStatusCode.TooManyRequests;
        public const HttpStatusCode Unauthorized = HttpStatusCode.Unauthorized;
        public const HttpStatusCode IpBanned = (HttpStatusCode)418;
        public const HttpStatusCode WafLimit = (HttpStatusCode)403;
        
        private static readonly Subject<(HttpResponseMessage message, object instance)> ResponseSubject = new();
        private static readonly Subject<(HttpResponseMessage message,object instance)> ObjectSentSubject = new();
        public static IObservable<(HttpResponseMessage message, object instance)> Object => ObjectSentSubject.AsObservable();
        public static IObservable<(HttpResponseMessage message, object instance)> Responded => ResponseSubject.AsObservable();

        public static bool WorthRetry(this HttpResponseMessage message) 
            => WorthRetryingCodes.Contains(message.StatusCode);

        public static IObservable<TimeSpan> RetryAfter(this HttpResponseException e,Func<TimeSpan,IObservable<Unit>> selector=null) {
            IObservable<TimeSpan> Retry(TimeSpan retryAfter1) => Observable.If(() => retryAfter1>TimeSpan.Zero,retryAfter1.Timer())
                .DefaultIfEmpty().To(retryAfter1);
            return e.HttpResponseMessage.RetryAfter().Observe()
                .SelectMany(retryAfter => selector?.Invoke(retryAfter).To<TimeSpan>() ?? Retry(retryAfter));
        }

        public static HttpClient HttpClient { get; set; }=new();
        
        public static TimeSpan RetryAfter(this HttpResponseMessage responseMessage){
            
                
            var dateTime = DateTime.Now;
            var retryAfterDelta = responseMessage.Headers.RetryAfter?.Delta;
            DateTime? delay = null;
            if (retryAfterDelta.HasValue){
                delay = dateTime.Add(retryAfterDelta.Value.Add(10.Seconds()));    
            }
            return (responseMessage.StatusCode switch{
                WafLimit =>delay?? dateTime.AddMinutes(5),
                IpBanned => delay??dateTime.AddMinutes(20),
                TooManyRequests => delay??dateTime.AddSeconds(61),
                Unauthorized => DateTime.Now.AddSeconds(-1),
                _ =>responseMessage.WorthRetry()? (delay??dateTime.AddSeconds(61)): DateTime.Now.AddSeconds(-1)
            }).Subtract(DateTime.Now);
        }

        public static T Sign<T>(this T requestMessage,string key,string secret) where T:HttpRequestMessage{
            if (key != null) {
                var toSing = requestMessage.RequestUri!.PathAndQuery;
                if (requestMessage.Method != HttpMethod.Get) {
                    toSing = requestMessage.RequestUri.AbsolutePath.JoinString("?",requestMessage.Content!.ReadAsStringAsync().Result);
                }
                var sign = secret.Bytes().CryptoSign(toSing).Replace("-", "").ToLower();
                requestMessage.Headers.Add("Signature", sign);
                requestMessage.Headers.Add("APIKEY", key);
            }
            return requestMessage;
        }

        public static IObservable<T> Send<T>(this HttpMethod httpMethod, string requestUrl,
            ConcurrentDictionary<object, IConnectableObservable<object>> cache, T obj, string key = null,
            string secret = null, Func<HttpResponseMessage, IObservable<T>> deserializeResponse = null, TimeSpan? pollInterval = null)
            where T : class ,new()
            => httpMethod.Observe()
                .Cache(cache, requestUrl, method => Observable.FromAsync(() => HttpClient.SendAsync(method.NewHttpRequestMessage( requestUrl,obj, key, secret)))
                        .Do(message => ResponseSubject.OnNext((message, obj))),pollInterval).
                Send(obj,deserializeResponse);
        public static string UserAgent { get; } = DetectInstalledUserAgent();

        private static string DetectInstalledUserAgent() {
            var paths = new[] {
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
            };

            foreach (var path in paths)
                if (File.Exists(path)) {
                    var v = FileVersionInfo.GetVersionInfo(path).FileVersion;
                    return path.Contains("msedge.exe")
                        ? $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{v} Safari/537.36 Edg/{v}"
                        : $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{v} Safari/537.36";
                }

            return
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        }
        public static void UseSystemBrowserUserAgent(this HttpRequestHeaders headers) {
            if (headers.Contains("User-Agent")) {
                headers.Remove("User-Agent");
            }
            headers.Add("User-Agent", UserAgent);
        }
        public static IObservable<T> Send<T>(this HttpClient client, HttpMethod httpMethod, string requestUrl,
            object obj = null, string key = null, string secret = null, Func<HttpResponseMessage, IObservable<T>> deserializeResponse = null) where T : class,new()
            =>  Observable.FromAsync(() => client.SendAsync(httpMethod.NewHttpRequestMessage(requestUrl,obj, key, secret)))
                .Do(message => ResponseSubject.OnNext((message, obj)))
                .WhenResponseObject(obj??typeof(T).CreateInstance(),httpResponseMessage => deserializeResponse?.Invoke(httpResponseMessage));

        private static readonly JsonDocument EmptyArrayDoc = JsonDocument.Parse("[]");

        [SuppressMessage("ReSharper", "UnusedParameter.Global")]
        public static JsonDocument EmptyArrayJsonDocument(this HttpClient client)
            => EmptyArrayDoc;
        
        public static IObservable<T> Send<T>(this HttpClient client, HttpRequestMessage httpRequestMessage) where T:class,new()
            => client.Send(httpRequestMessage, default(Func<HttpResponseMessage, IObservable<T>>));

        public static IObservable<T> Send<T>(this HttpClient client, HttpRequestMessage httpRequestMessage, Func<HttpResponseMessage, IObservable<T>> deserializeResponse) where T:class,new()
            => Observable.FromAsync(() => client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead))
                .Do(message => ResponseSubject.OnNext((message, null)))
                .WhenResponseObject(CreateInstance<T>(), deserializeResponse.Invoke);

        private static object CreateInstance<T>() => typeof(T) == typeof(string) ? "" : typeof(T).CreateInstance();

        public static IObservable<T> SendRequest<T>(this HttpClient client, HttpRequestMessage httpRequestMessage, 
            T obj = null, Func<HttpResponseMessage, IObservable<T>> deserializeResponse = null) where T : class,new()
            => client.SendRequest(httpRequestMessage,null,obj,deserializeResponse);
        
        public static IObservable<T> SendRequest<T>(this HttpClient client, HttpRequestMessage httpRequestMessage,Action<HttpResponseMessage> onResponse, 
            T obj = null, Func<HttpResponseMessage, IObservable<T>> deserializeResponse = null) where T : class,new()
            => client.Request( httpRequestMessage, onResponse, obj)
                .SendRequest(obj??typeof(T).CreateInstance(), deserializeResponse);

        static IObservable<HttpResponseMessage> Request<T>(this HttpClient client, HttpRequestMessage httpRequestMessage, Action<HttpResponseMessage> onResponse=null, T obj=null) where T : class,new() 
            => client.SendAsync(httpRequestMessage,HttpCompletionOption.ResponseHeadersRead)
                .ToObservable().EnsureSuccessStatusCode()
                .Do(message => onResponse?.Invoke(message))
                .Do(message => ResponseSubject.OnNext((message, obj)));

        public static IObservable<(HttpResponseMessage message,JsonObject[] objects)> WhenResponseObject(this HttpClient client, HttpRequestMessage httpRequestMessage) 
            => client.WhenResponse(httpRequestMessage).Select(t => (t.message,t.response.Bytes().DeserializeJsonNode().ToJsonObjects().ToArray()));

        public static IObservable<(Stream response, HttpResponseMessage message)> WhenResponse(this HttpClient client, HttpRequestMessage httpRequestMessage) 
            => client.Request<HttpResponseMessage>(httpRequestMessage).WhenResponseObject(null, httpResponseMessage => httpResponseMessage.Observe())
                .SelectMany(result => Observable.FromAsync(() => result.Content.ReadAsStreamAsync()).Pair(result));

        
        public static IObservable<HttpResponseMessage> EnsureSuccessStatusCode(this IObservable<HttpResponseMessage> source) 
            => source.If(message => !message.IsSuccessStatusCode, message => Observable.If(message.IsJsonResponse,message.WhenJsonDocument())
                .Merge(Observable.If(() => !message.IsJsonResponse(),Observable.FromAsync(() => message.Content.ReadAsStringAsync())
                    .SelectMany(content => new HttpResponseException($"{message.StatusCode}, {message.ReasonPhrase}, {content}", message).Throw<(HttpResponseMessage[] objects, JsonDocument document)>())))
                .SelectMany(t => t.objects),message => message.Observe());

        private static IObservable<(HttpResponseMessage[] objects, JsonDocument document)> WhenJsonDocument(this HttpResponseMessage message) 
            => Observable.FromAsync(() => message.Content.ReadAsStreamAsync()).WhenJsonDocument(document =>
                    new HttpResponseException(document.RootElement.ToString(), message).Throw<HttpResponseMessage>());

        public static IObservable<(T[] objects, JsonDocument document)> WhenResponseDocument<T>(this HttpClient client,
            HttpRequestMessage httpRequestMessage, Func<(JsonDocument document, HttpResponseMessage message), IObservable<T>> selector) 
            => client.Request<HttpResponseMessage>(httpRequestMessage)
                .SelectMany(message => message.Content.ReadAsStreamAsync().ToObservable()
                    .SelectMany(stream => stream.WhenJsonDocument(document => selector((document, message)))));

        public static IObservable<(JsonDocument document, HttpResponseMessage message)> WhenResponseDocument(
            this HttpClient client, HttpRequestMessage httpRequestMessage)
            => client.Request<HttpResponseMessage>(httpRequestMessage)
                .SelectMany(message => message.Content.ReadAsStreamAsync().ToObservable()
                    .SelectMany(stream => stream.WhenJsonDocument(false)
                        .Select(document => (document, message))));
        
        
        public static IObservable<(T[] objects, JsonDocument document)> WhenResponseDocument<T>(this HttpClient client,
            string url, Func<JsonDocument, IObservable<T>> selector) 
            => client.GetStreamAsync(url).ToObservable()
                .SelectMany(stream => stream.WhenJsonDocument(selector));
        public static IObservable<(JsonDocument document, HttpResponseMessage message)> WhenResponseDocument(this HttpClient client, string url,HttpMethod httpMethod=null) 
            => client.WhenResponseDocument(new HttpRequestMessage(httpMethod??HttpMethod.Get, url));
        
        public static IObservable<T> SelectMany<T>(this IObservable<(T[] objects, JsonDocument document)> source) => source.SelectMany(t => t.objects);
        
        public static IObservable<T> SelectElement<T>(this IObservable<(JsonDocument document,HttpResponseMessage message)> source,Func<JsonElement,IObservable<T>> selector) 
            => source.SelectDocument(document => selector(document.RootElement));

        public static IObservable<JsonDocument> SelectDocument(this IObservable<(JsonDocument document, HttpResponseMessage message)> source)
            => source.Select(t => t.document);
        
        public static IObservable<string> SelectString(this IObservable<(JsonDocument document, HttpResponseMessage message)> source)
            => source.SelectDocument(document => document.RootElement.GetString().Observe());
        
        public static IObservable<T> SelectDocument<T>(this IObservable<(JsonDocument document, HttpResponseMessage message)> source,Func<JsonDocument,IObservable<T>> selector)
            => source.SelectMany(t => selector(t.document).FinallySafe(() => t.document.Dispose()));
        public static IObservable<JsonElement> SelectMany(this IObservable<(JsonDocument document,HttpResponseMessage message)> source,bool dispose=true) 
            => source.SelectMany(t => t.document.SelectMany(dispose));
        private static IObservable<T> WhenResponseObject<T>(this IObservable<HttpResponseMessage> source,
            object obj, Func<HttpResponseMessage,IObservable<T>> deserializeResponse) 
            => source.SelectMany(responseMessage => deserializeResponse.DeserializeResponse()(responseMessage)
                .DoOnComplete(() => ObjectSentSubject.OnNext((responseMessage,  obj))));

        public static IObservable<T> SendRequest<T>(this HttpClient client, HttpMethod httpMethod,string url, 
            T obj = null, Func<HttpResponseMessage, IObservable<T>> deserializeResponse = null) where T : class,new()
            => client.SendRequest(new HttpRequestMessage(httpMethod, url),obj,deserializeResponse);
        
        public static IObservable<T> Send<T>(this HttpMethod httpMethod, string requestUrl, string key = null,
            string secret = null, Func<HttpResponseMessage, IObservable<T>> deserializeResponse = null) where T : class,new() 
            => HttpClient.Send(httpMethod, requestUrl,null,key,secret,deserializeResponse);
        
        public static IObservable<T> Send<T>(this HttpMethod httpMethod, string requestUrl, Func<HttpResponseMessage, IObservable<T>> deserializeResponse = null
        , string key = null, string secret = null) where T : class,new() 
            => HttpClient.Send(httpMethod, requestUrl,null,key,secret,deserializeResponse);

        private static IObservable<T> SendRequest<T>(this IObservable<HttpResponseMessage> source,
            object obj, Func<HttpResponseMessage, IObservable<T>> deserializeResponse) where T : class, new() 
            => source.WhenResponseObject(obj, httpResponseMessage =>
                    deserializeResponse(httpResponseMessage) ?? Deserialize<T>(obj)(httpResponseMessage));

        private static IObservable<T> Send<T>(this IObservable<HttpResponseMessage> source,
            object obj, Func<HttpResponseMessage, IObservable<T>> deserializeResponse) where T:class,new() 
            => source.WhenResponseObject(obj, httpResponseMessage =>
                deserializeResponse?.Invoke(httpResponseMessage) ?? Deserialize<T>(obj)(httpResponseMessage));

        private static Func<HttpResponseMessage, IObservable<T>> Deserialize<T>(object obj) where T:class,new() 
            => message => message.DeserializeJson<T>(obj?.GetType());
        private static IObservable<T> DeserializeJson<T>(this HttpResponseMessage responseMessage,Type returnType) where T:class,new() 
            => returnType != null ? responseMessage.DeserializeJson(returnType).ToObservable().Cast<T>()
                    .If(obj => obj?.GetType().IsArray ?? false, arg => ((IEnumerable<T>)arg).ToNowObservable(),
                        arg => arg.Observe()).Select(arg => arg) :
                responseMessage.DeserializeJson<T>().ToObservable(Transform.ImmediateScheduler).Select(arg => arg);


        private static Func<HttpResponseMessage, IObservable<T>> DeserializeResponse<T>(this Func<HttpResponseMessage, IObservable<T>> deserializeResponse) 
            => deserializeResponse ?? (message =>  message.DeserializeJson<T>().ToObservable(Transform.ImmediateScheduler));

        public static T SetContent<T>(this T message,  string content, string key = null, string secret = null,bool formDataContent=false) where T:HttpRequestMessage {
            if (message.Method != HttpMethod.Get&&!string.IsNullOrEmpty(content) )
                message.Content = new StringContent(content, Encoding.UTF8, formDataContent ? "application/x-www-form-urlencoded" : "application/json");

            return message.Sign(key, secret);
        }

        public static HttpRequestMessage NewHttpRequestMessage(this HttpMethod httpMethod, string requestUri,object o=null, string key=null,string secret=null) 
            => new HttpRequestMessage(httpMethod,requestUri).SetContent(o.Serialize(),key,secret);
        
    }
    
    public class HttpResponseException(string paramName, HttpResponseMessage httpResponseMessage)
        : HttpRequestException(paramName) {
        public HttpResponseMessage HttpResponseMessage{ get; } = httpResponseMessage;

        public override string ToString() {
            return new[]{base.ToString(),HttpResponseMessage.RequestMessage?.RequestUri?.ToString()}.JoinNewLine();
        }
    }

    
}