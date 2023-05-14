using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fasterflect;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.Network;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform.System.Text.Json;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Reactive.Transform.System.Net {
    public static class NetworkExtensions {
        public static readonly HttpStatusCode[] WorthRetryingCodes ={
            HttpStatusCode.TooManyRequests, IpBanned, WafLimit, HttpStatusCode.BadGateway, HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized, HttpStatusCode.RequestTimeout,HttpStatusCode.InternalServerError,HttpStatusCode.ServiceUnavailable, 
            HttpStatusCode.GatewayTimeout, 
        };
        public const HttpStatusCode TooManyRequests = HttpStatusCode.TooManyRequests;
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
            return e.HttpResponseMessage.RetryAfter().ReturnObservable()
                .SelectMany(retryAfter => selector?.Invoke(retryAfter).To<TimeSpan>() ?? Retry(retryAfter));
        }

        public static HttpClient HttpClient { get; set; }=new();
        
        public static TimeSpan RetryAfter(this HttpResponseMessage responseMessage){
            var dateTime = !responseMessage.Headers.Contains("Date") ? DateTime.Now
                : DateTimeOffset.Parse(responseMessage.Headers.GetValues("Date").First()).LocalDateTime;
            var retryAfterDelta = responseMessage.Headers.RetryAfter?.Delta;
            DateTime? delay = null;
            if (retryAfterDelta.HasValue){
                delay = dateTime.Add(retryAfterDelta.Value);    
            }
            return (responseMessage.StatusCode switch{
                WafLimit =>delay?? dateTime.AddMinutes(5),
                IpBanned => delay??dateTime.AddMinutes(20),
                TooManyRequests => delay??dateTime.AddSeconds(61),
                _ =>responseMessage.WorthRetry()? (delay??dateTime.AddSeconds(61)): DateTime.Now.AddSeconds(-1)
            }).Subtract(DateTime.Now);
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
            string secret = null, Func<HttpResponseMessage, IObservable<T>> deserializeResponse = null, TimeSpan? pollInterval = null)
            where T : class ,new()
            => httpMethod.ReturnObservable()
                .Cache(cache, requestUrl, method => Observable.FromAsync(() => HttpClient.SendAsync(method.NewHttpRequestMessage( requestUrl,obj, key, secret)))
                        .Do(message => ResponseSubject.OnNext((message, obj))),pollInterval).
                Send(obj,deserializeResponse);

        public static IObservable<T> Send<T>(this HttpClient client, HttpMethod httpMethod, string requestUrl,
            object obj = null, string key = null, string secret = null, Func<HttpResponseMessage, IObservable<T>> deserializeResponse = null) where T : class,new()
            =>  Observable.FromAsync(() => client.SendAsync(httpMethod.NewHttpRequestMessage(requestUrl,obj, key, secret)))
                .Do(message => ResponseSubject.OnNext((message, obj)))
                .WhenResponseObject(obj??typeof(T).CreateInstance(),httpResponseMessage => deserializeResponse?.Invoke(httpResponseMessage));

        public static IObservable<T> Send<T>(this HttpClient client, HttpRequestMessage httpRequestMessage) where T:class,new()
            => client.Send(httpRequestMessage, default(Func<HttpResponseMessage, IObservable<T>>));

        public static IObservable<T> Send<T>(this HttpClient client, HttpRequestMessage httpRequestMessage, Func<HttpResponseMessage, IObservable<T>> deserializeResponse) where T:class,new()
            => Observable.FromAsync(() => client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead))
                .Do(message => ResponseSubject.OnNext((message, null)))
                .WhenResponseObject(CreateInstance<T>(), deserializeResponse.Invoke);

        private static object CreateInstance<T>() => typeof(T) == typeof(string) ? "" : typeof(T).CreateInstance();

        public static IObservable<T> SendRequest<T>(this HttpClient client, HttpRequestMessage httpRequestMessage, 
            T obj = null, Func<HttpResponseMessage, IObservable<T>> deserializeResponse = null, [CallerMemberName] string caller = "") where T : class,new()
            => client.SendRequest(httpRequestMessage,null,obj,deserializeResponse,caller);
        
        public static IObservable<T> SendRequest<T>(this HttpClient client, HttpRequestMessage httpRequestMessage,Action<HttpResponseMessage> onResponse, 
            T obj = null, Func<HttpResponseMessage, IObservable<T>> deserializeResponse = null, [CallerMemberName] string caller = "") where T : class,new()
            => client.Request( httpRequestMessage, onResponse, obj)
                .SendRequest(obj??typeof(T).CreateInstance(), deserializeResponse);

        private static IObservable<HttpResponseMessage> Request<T>(this HttpClient client, HttpRequestMessage httpRequestMessage, Action<HttpResponseMessage> onResponse=null, T obj=default) where T : class,new() 
            => client.SendAsync(httpRequestMessage,HttpCompletionOption.ResponseHeadersRead)
                .ToObservable().EnsureSuccessStatusCode()
                .Do(message => onResponse?.Invoke(message))
                .Do(message => ResponseSubject.OnNext((message, obj)));

        public static IObservable<(HttpResponseMessage message,JsonObject[] objects)> WhenResponseObject(this HttpClient client, HttpRequestMessage httpRequestMessage) 
            => client.WhenResponse(httpRequestMessage).Select(t => (t.result.Message,t.response.DeserializeJsonNode().ToJsonObjects().ToArray()));
        
        static IObservable<(byte[] response, ResponseResult result)> WhenResponse(this HttpClient client, HttpRequestMessage httpRequestMessage) 
            => client.Request<ResponseResult>(httpRequestMessage).WhenResponseObject(null, httpResponseMessage => new ResponseResult(httpResponseMessage).ReturnObservable())
                .SelectMany(result => Observable.FromAsync(() => result.Message.Content.ReadAsByteArrayAsync()).ObserveOnDefault().Pair(result));

        // public static IObservable<HttpResponseMessage> EnsureSuccessStatusCode1(this IObservable<HttpResponseMessage> source)
        //     => source.If(message => !message.IsSuccessStatusCode,)
        public static IObservable<HttpResponseMessage> EnsureSuccessStatusCode(this IObservable<HttpResponseMessage> source) 
            => source.If(message => !message.IsSuccessStatusCode, message => Observable.If(message.IsJsonResponse,message.WhenJsonDocument())
                .Merge(Observable.If(() => !message.IsJsonResponse(),Observable.FromAsync(() => message.Content.ReadAsStringAsync())
                    .SelectMany(content => Observable.Throw<(HttpResponseMessage[] objects, JsonDocument document)>(new HttpResponseException($"{message.StatusCode}, {message.ReasonPhrase}, {content}", message)))))
                .SelectMany(t => t.objects),message => message.ReturnObservable());

        private static IObservable<(HttpResponseMessage[] objects, JsonDocument document)> WhenJsonDocument(this HttpResponseMessage message) 
            => Observable.FromAsync(() => message.Content.ReadAsStreamAsync()).WhenJsonDocument(document =>
                    Observable.Throw<HttpResponseMessage>(new HttpResponseException(document.RootElement.ToString(), message)))
                .Catch<(HttpResponseMessage[] objects, JsonDocument document),Exception>(exception 
                    => Observable.Throw<(HttpResponseMessage[] objects, JsonDocument document)>(new HttpResponseException($"{message.StatusCode}, {message.ReasonPhrase}, {exception}", message)));


        public static IObservable<(T[] objects, JsonDocument document)> WhenResponseDocument<T>(this HttpClient client,
            HttpRequestMessage httpRequestMessage, Func<(JsonDocument document, HttpResponseMessage message), IObservable<T>> selector) 
            => client.Request<ResponseResult>(httpRequestMessage)
                .SelectMany(message => message.Content.ReadAsStreamAsync().ToObservable()
                    .SelectMany(stream => stream.WhenJsonDocument(document => selector((document, message)))));

        public static IObservable<(JsonDocument document, HttpResponseMessage message)> WhenResponseDocument(
            this HttpClient client, HttpRequestMessage httpRequestMessage)
            => client.Request<ResponseResult>(httpRequestMessage)
                .SelectMany(message => message.Content.ReadAsStreamAsync().ToObservable()
                    .SelectMany(stream => stream.WhenJsonDocument()
                        .Finally(() => { }).Select(document => (document, message))));
        
        
        public static IObservable<(T[] objects, JsonDocument document)> WhenResponseDocument<T>(this HttpClient client,
            string url, Func<JsonDocument, IObservable<T>> selector) 
            => client.GetStreamAsync(url).ToObservable()
                .SelectMany(stream => stream.WhenJsonDocument(selector));
        
        public static IObservable<T> SelectMany<T>(this IObservable<(T[] objects, JsonDocument document)> source) => source.SelectMany(t => t.objects);
        
        public static IObservable<T> SelectElement<T>(this IObservable<(JsonDocument document,HttpResponseMessage message)> source,Func<JsonElement,IObservable<T>> selector) 
            => source.SelectDocument(document => selector(document.RootElement));

        public static IObservable<JsonDocument> SelectDocument(this IObservable<(JsonDocument document, HttpResponseMessage message)> source)
            => source.Select(t => t.document);
        
        public static IObservable<T> SelectDocument<T>(this IObservable<(JsonDocument document, HttpResponseMessage message)> source,Func<JsonDocument,IObservable<T>> selector)
            => source.SelectMany(t => selector(t.document).FinallySafe(() => t.document.Dispose()));
        public static IObservable<JsonElement> SelectMany(this IObservable<(JsonDocument document,HttpResponseMessage message)> source,bool dispose=true) 
            => source.SelectMany(t => t.document.SelectMany(dispose));
        private static IObservable<T> WhenResponseObject<T>(this IObservable<HttpResponseMessage> source,
            object obj, Func<HttpResponseMessage,IObservable<T>> deserializeResponse) 
            => source.SelectMany(responseMessage => deserializeResponse.DeserializeResponse()(responseMessage)
                .DoOnComplete(() => ObjectSentSubject.OnNext((responseMessage,  obj))));

        record ResponseResult {
            public HttpResponseMessage Message{ get; }
            
            public ResponseResult(HttpResponseMessage message) 
                => Message = message;

            public ResponseResult() {
            }
        }
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
                    .If(obj => obj?.GetType().IsArray ?? false, arg => arg.Cast<IEnumerable<T>>().ToNowObservable(),
                        arg => arg.ReturnObservable()).Select(arg => arg) :
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

    
    public class HttpResponseException:HttpRequestException {
        public HttpResponseMessage HttpResponseMessage{ get; }

        public HttpResponseException(string paramName, HttpResponseMessage httpResponseMessage) : base(paramName) => HttpResponseMessage = httpResponseMessage;
    }

    
}