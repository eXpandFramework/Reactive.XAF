using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using Fasterflect;
using HarmonyLib;
using Moq;
using Moq.Language.Flow;
using Moq.Protected;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System.Net;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;

namespace Xpand.TestsLib.Common {
    public class HarmonyTest:Harmony {
        public HarmonyTest() : base(TestContext.CurrentContext.Test.FullName){
        }
    }

    public class MockHttpClient:HttpClient {
        
    }
    public static class MockExtensions {
        private static Mock<HttpWebResponse> _mockResponse;
        private static Func<Uri,bool> _matchUri;

        public static Mock<HttpWebResponse> MockResponse(this TestScheduler testScheduler, HttpStatusCode statusCode,DateTimeOffset? date=null,string responseString=null) {
            responseString ??= TestContext.CurrentContext.Test.Name;
            date ??= testScheduler.Now;
            var mockResponse = new Mock<HttpWebResponse>();
            mockResponse.Setup(response => response.GetResponseStream())
                .Returns(new MemoryStream(responseString.Bytes()));
            mockResponse.Setup(response => response.StatusCode).Returns(statusCode);
            mockResponse.Setup(response => response.Headers).Returns(new WebHeaderCollection{ { "Date", date.ToString() } });
            return mockResponse;
        }
        
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static bool WebRequestCreate(Uri requestUri, ref WebRequest __result){
            if (_matchUri(requestUri)){
                var mockRequest = new Mock<HttpWebRequest>();
                var timer = Observable.Timer(TimeSpan.FromMilliseconds(200));
                if (_mockResponse.Object.StatusCode != HttpStatusCode.OK) {
                    mockRequest.Setup(request => request.GetResponseAsync())
                        .Returns(() => timer.SelectMany(_ =>
                            new WebException("", null, WebExceptionStatus.ReceiveFailure,
                                _mockResponse.Object).Throw<WebResponse>()).ToTask());    
                }
                else {
                    mockRequest.Setup(request => request.GetResponseAsync())
                        .Returns(() => timer.Select(_ => _mockResponse.Object).Cast<WebResponse>().ToTask());
                }
                
                __result = mockRequest.Object;
                return false;
            }
            return true;
        }

        public static IDisposable PatchWebRequest(this  Harmony harmony,Func<Uri,bool> matchUri,Mock<HttpWebResponse> mockResponse){
            var methodInfo = typeof(WebRequest).GetMethod(nameof(WebRequest.CreateHttp),new[]{typeof(Uri)});
            var harmonyMethod = new HarmonyMethod(typeof(MockExtensions), nameof(WebRequestCreate));
            harmony.Patch(methodInfo, harmonyMethod);
            _matchUri = matchUri;
            _mockResponse=mockResponse;
            return Observable.While(() => TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Inconclusive, Observable.Empty<Unit>())
                .Finally(() => harmony.Unpatch(methodInfo, harmonyMethod.method))
                .SubscribeOn(Scheduler.Default).Subscribe();
        }

        public static Mock<T> GetMock<T>(this T t) where T : class => Mock.Get(t);

        public static void VerifySend<THandler>(this Mock<THandler> handlerMock, Times times,Func<HttpRequestMessage, bool> filter) where THandler:HttpMessageHandler 
            => handlerMock.Protected().Verify("SendAsync", times, ItExpr.Is<HttpRequestMessage>(message => filter==null||filter(message)),
                ItExpr.IsAny<CancellationToken>());

        public static void SetupReceive(this Mock<WebSocket> mock, byte[] bytes,Func<Task<WebSocketReceiveResult>> resultSelector=null) {
            mock.Setup(socket => socket.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
                .Returns((ArraySegment<byte> buffer, CancellationToken token) => {
                    Array.Copy(bytes,buffer.Array!,bytes.Length);
                    return resultSelector?.Invoke()??new WebSocketReceiveResult(bytes.Length,WebSocketMessageType.Text, true).Observe().Delay(1000.Milliseconds()).ToTask(token);
                });
            mock.Setup(socket => socket.State).Returns(WebSocketState.Open);
        }

        public static IReturnsResult<THandler> SetupSendEmptyArray<THandler>(this Mock<THandler> handlerMock,
            Action<HttpResponseMessage> configure = null, IScheduler scheduler = null) where THandler : HttpMessageHandler
            => handlerMock.SetupSend(message => {
                message.Content = new StringContent("[]");
                configure?.Invoke(message);
            }, scheduler);
        
        public static IReturnsResult<THandler> SetupSendArray<THandler>(this Mock<THandler> handlerMock,
            Action<HttpResponseMessage> configure = null, IScheduler scheduler = null) where THandler : HttpMessageHandler
            => handlerMock.SetupSend(message => {
                message.Content = new StringContent(@"[
    {
        ""id"": 1,
        ""name"": ""John Doe"",
        ""isActive"": true
    }
]
");
                configure?.Invoke(message);
            }, scheduler);
        
        public static IReturnsResult<THandler> SetupSend<THandler>(this Mock<THandler> handlerMock,
            Action<HttpResponseMessage> configure=null, IScheduler scheduler = null) where THandler : HttpMessageHandler 
            => handlerMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage requestMessage, CancellationToken token)
                    =>  new HttpResponseMessage { StatusCode = HttpStatusCode.OK, RequestMessage = requestMessage }.Observe()
                        .Do(message => message.Content=new StringContent("[]"))
                        .Do(message => configure?.Invoke(message))
                        .Delay(Delay,scheduler??=Scheduler.Default)
                        .ToTask(token, scheduler??=Scheduler.Default));
        
        public static ISetup<THandler, Task<HttpResponseMessage>> SetupSendAsync<THandler>(this Mock<THandler> handlerMock) where THandler : HttpMessageHandler 
            => handlerMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());

        public static IObservable<HttpResponseMessage> WhenMockedResponse(this XafApplication application,
            Action<HttpClient> configure = null, IScheduler scheduler = null) 
            => application.WhenMockedResponse<HttpClient>(configure, scheduler);

        public static IObservable<HttpResponseMessage> WhenMockedResponse<TClient,THandler>(this XafApplication application,
            Action<TClient> configure = null, IScheduler scheduler = null) where TClient : HttpClient, new() where THandler:HttpMessageHandler {
            var mock = new Mock<THandler>();
            var httpClient = (TClient)typeof(TClient).CreateInstance(mock.Object);
            if (configure != null) {
                configure(httpClient);
            }
            else {
                NetworkExtensions.HttpClient = httpClient;   
            }
            return mock.WhenResponse();

        }

        public static IObservable<HttpResponseMessage> WhenMockedResponse<TClient>(this XafApplication application,
            Action<TClient> configure = null, IScheduler scheduler = null) where TClient : HttpClient, new() 
            => application.WhenMockedResponse<TClient, HttpMessageHandler>(configure);

        public static IObservable<HttpResponseMessage> WhenResponse<THandler>(this Mock<THandler> handlerMock,
            IScheduler scheduler = null) where THandler : HttpMessageHandler 
            => Observable.Create<HttpResponseMessage>(observer => {
                handlerMock.SetupSend(observer.OnNext, scheduler);
                return Disposable.Empty;
            });


        public static TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(200);
    }
}