using System;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Language.Flow;
using Moq.Protected;

namespace Xpand.TestsLib.Common {
    public static class MockExtensions {
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