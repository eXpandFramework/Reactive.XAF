using System;
using System.Linq;
using System.Net.Http;
using DevExpress.ExpressApp;
using Moq;
using NUnit.Framework;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Rest.Tests.BO;

namespace Xpand.XAF.Modules.Reactive.Rest.Tests.Common {
    public abstract class RestCommonAppTest:BlazorCommonAppTest {
        private RestModule _blazorModule;
        protected Mock<HttpMessageHandler> HandlerMock;
        protected override Type StartupType => typeof(RestStartup);

        protected RestModule BlazorModule(params ModuleBase[] modules) {
            var module = Application.AddModule<RestModule>(GetType().CollectExportedTypesFromAssembly().ToArray());
            Application.Logon();
            using var objectSpace = Application.CreateObjectSpace();
            return module;
        }

        [OneTimeSetUp]
        public override void Init() {
            base.Init();
            _blazorModule = BlazorModule();
            Application = _blazorModule.Application.ToBlazor();
        }

        public override void Setup() {
            base.Setup();
            RestService.CacheStorage.Clear();
            HandlerMock = new Mock<HttpMessageHandler>();
            RestService.HttpClient=new HttpClient(HandlerMock.Object);
        }


        
    }
}