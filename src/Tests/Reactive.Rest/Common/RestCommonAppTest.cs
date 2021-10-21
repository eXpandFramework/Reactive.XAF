using System.Net.Http;
using System.Threading;
using DevExpress.ExpressApp;
using Moq;
using NUnit.Framework;
using Xpand.TestsLib.Blazor;

namespace Xpand.XAF.Modules.Reactive.Rest.Tests.Common {
    public abstract class RestCommonAppTest:RestCommonTest {
        private RestModule _blazorModule;
        protected Mock<HttpMessageHandler> HandlerMock;

        [OneTimeSetUp]
        public override void Init() {
            base.Init();
            SynchronizationContext.SetSynchronizationContext(new RendererSynchronizationContext());
            _blazorModule = BlazorModule();
            Application = _blazorModule.Application;
        }
        public override void Dispose() { }

        [OneTimeTearDown]
        public override void Cleanup() {
            
            Application?.Dispose();
            WebHost?.Dispose();
        }

        public override void Setup() {
            base.Setup();
            RestService.CacheStorage.Clear();
            HandlerMock = new Mock<HttpMessageHandler>();
            RestService.HttpClient=new HttpClient(HandlerMock.Object);
        }

        public XafApplication Application { get; private set; }
        
    }
}