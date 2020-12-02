using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Xpo;
using Fasterflect;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.TestsLib.Blazor{
    public class XpoDataStoreProviderAccessor {
        public IXpoDataStoreProvider DataStoreProvider { get; set; }
    }

    public class TestBlazorApplication : BlazorApplication, ITestApplication{
        private IXpoDataStoreProvider GetDataStoreProvider(string connectionString) {
            XpoDataStoreProviderAccessor accessor = ServiceProvider.GetRequiredService<XpoDataStoreProviderAccessor>();
            lock(accessor) {
                accessor.DataStoreProvider ??= XPObjectSpaceProvider.GetDataStoreProvider(connectionString, null, true);
            }
            return accessor.DataStoreProvider;
        }

        // private readonly bool _transmitMessage;

        public TestBlazorApplication(Type sutModule, bool transmitMessage = true, bool handleExceptions=true):this(){
            
            SUTModule = sutModule;
            
            TraceClientConnected = this.ClientConnect();
            TraceClientBroadcast = this.ClientBroadcast();
            this.WhenCreateCustomObjectSpaceProvider()
                .SelectMany(t => {
                    var xpoDataStoreProvider = ServiceProvider.GetRequiredService<XpoDataStoreProviderAccessor>().DataStoreProvider;
                })
        }

        public bool TransmitMessage => false;

        public IObservable<Unit> TraceClientBroadcast{ get; set; }


        public IObservable<Unit> TraceClientConnected{ get; set; }
        
        public Type SUTModule{ get; }

        // protected override void Dispose(bool disposing){
        //     if (_transmitMessage){
        //         TraceClientConnected.ToTaskWithoutConfigureAwait().GetAwaiter().GetResult();
        //         TraceClientBroadcast.ToTaskWithoutConfigureAwait().GetAwaiter().GetResult();
        //     }
        //
        //     base.Dispose(disposing);
        // }

        public TestBlazorApplication() {
            // ServiceProvider = Mock.Of<IServiceProvider>();
            // typeof(BlazorApplication).Field("sharedModelManager",Flags.StaticPrivate).Set(null,null);
        }

        


//        protected override LayoutManager CreateLayoutManagerCore(bool simple){
//            if (!simple){
//                var controlMock = new Mock<Control>(){CallBase = true};
//                var layoutManagerMock = new Mock<WinLayoutManager>(){CallBase = true};
//                layoutManagerMock.Setup(_ => _.LayoutControls(It.IsAny<IModelNode>(), It.IsAny<ViewItemsCollection>())).Returns(controlMock.Object);
//            
//                return layoutManagerMock.Object;
//            }
//
//            return new WinSimpleLayoutManager();
//        }

        protected override string GetModelCacheFileLocationPath(){
            return null;
        }

        protected override string GetDcAssemblyFilePath(){
            return null;
        }


        protected override string GetModelAssemblyFilePath(){
            return $@"{AppDomain.CurrentDomain.ApplicationPath()}\ModelAssembly{Guid.NewGuid()}.dll";
        }
    }

}