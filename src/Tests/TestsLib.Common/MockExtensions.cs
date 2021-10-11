using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Moq;
using Moq.Language.Flow;
using Moq.Protected;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Xpand.TestsLib.Common {
    public class HarmonyTest:Harmony {
        public HarmonyTest() : base(TestContext.CurrentContext.Test.FullName){
        }
    }
    public static class MockExtensions {
        private static Func<Uri,Mock<HttpWebResponse>> _mockResponse;

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static bool WebRequestCreate(Uri requestUri, ref WebRequest __result){
            var mockResponse = _mockResponse(requestUri);
            if (mockResponse!=null){
                var mockRequest = new Mock<HttpWebRequest>();
                mockRequest.Setup(request => request.GetResponseAsync())
                    .Throws(new WebException("", null, WebExceptionStatus.ReceiveFailure, mockResponse.Object));
                __result = mockRequest.Object;
                return false;
            }

            return true;
        }

        public static IDisposable PatchWebRequest(this  Harmony harmony,Func<Uri, Mock<HttpWebResponse>> mockResoponse){
            var methodInfo = typeof(WebRequest).GetMethod(nameof(WebRequest.CreateHttp),new[]{typeof(Uri)});
            var harmonyMethod = new HarmonyMethod(typeof(MockExtensions), nameof(WebRequestCreate));
            harmony.Patch(methodInfo, harmonyMethod);
            _mockResponse=mockResoponse;
            return Observable.While(() => TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Inconclusive, Observable.Empty<Unit>())
                .Finally(() => harmony.Unpatch(methodInfo, harmonyMethod.method))
                .SubscribeOn(Scheduler.Default).Subscribe();
        }

        public static Mock<T> GetMock<T>(this T t) where T : class => Mock.Get(t);

        public static void VerifySend(this Mock<HttpMessageHandler> handlerMock, Times times,Func<HttpRequestMessage, bool> filter) 
            => handlerMock.Protected().Verify("SendAsync", times, ItExpr.Is<HttpRequestMessage>(message => filter==null||filter(message)),
                ItExpr.IsAny<CancellationToken>());

        public static IReturnsResult<HttpMessageHandler> SetupSend(this Mock<HttpMessageHandler> handlerMock, Action<HttpResponseMessage> configure)
            => handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(async (HttpRequestMessage requestMessage, CancellationToken _)
                    => await Observable.Timer(TimeSpan.FromMilliseconds(200))
                        .Select(_ => new HttpResponseMessage {
                            StatusCode = HttpStatusCode.OK,
                            RequestMessage = requestMessage
                        })
                        .Do(configure));
    }
}