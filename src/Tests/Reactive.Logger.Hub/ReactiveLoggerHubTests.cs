using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Xpo;
using DevExpress.ExpressApp.Xpo.Updating;
using DevExpress.Persistent.BaseImpl;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive.Logger.Hub.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Services;

using Task = System.Threading.Tasks.Task;

namespace Xpand.XAF.Modules.Reactive.Logger.Hub.Tests{
    [NonParallelizable]
    public class ReactiveLoggerHubTests : BaseTest{
        static ReactiveLoggerHubTests() {
            ReactiveTraceListener.DisableFileWriter = true;
        }
        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)][Order(0)]
        public async Task Start_Server_After_Logon() {
            using var application = HubModule().Application;
            var startServer = application.WhenTraceOnNext(nameof(ReactiveLoggerHubService.StartServer))
                .FirstAsync().SubscribeReplay();
            var startServerSave = application.WhenTraceOnNextEvent(nameof(ReactiveLoggerHubService.StartServer))
                .FirstAsync().SubscribeReplay();

            application.Logon();
            application.CreateObjectSpace();
            await startServer.Timeout(Timeout);

            await startServerSave.Timeout(Timeout);
        }

        [Test()]
        [XpandTest]
        [Apartment(ApartmentState.STA)][Order(100)]
        public async Task Connect_Client() {
            using var clientWinApp = new ClientWinApp();
            clientWinApp.AddModule<ReactiveLoggerHubModule>(typeof(RLH),typeof(BaseObject));
            var connectClient = clientWinApp.WhenTraceOnNextEvent(nameof(ReactiveLoggerHubService.ConnectClient)).FirstAsync()
                .SubscribeOn(Scheduler.Default)
                .SubscribeReplay();
            clientWinApp.Logon();
            clientWinApp.CreateObjectSpace();

                
            using (var application = Platform.Win.NewApplication<ReactiveLoggerHubModule>()){
                application.AddModule<ReactiveLoggerHubModule>(nameof(Connect_Client), typeof(RLH),typeof(BaseObject));
                application.Logon();
                application.CreateObjectSpace();
                await connectClient.FirstAsync().Timeout(Timeout);
            }
            connectClient = clientWinApp.WhenTraceOnNextEvent(nameof(ReactiveLoggerHubService.ConnectClient)).FirstAsync()
                .SubscribeReplay();
            using (var application = Platform.Win.NewApplication<ReactiveLoggerHubModule>()){
                application.AddModule<ReactiveLoggerHubModule>($"{nameof(Connect_Client)}_2",typeof(RLH),typeof(BaseObject));
                application.Logon();
                application.CreateObjectSpace();

                await connectClient.FirstAsync().Timeout(Timeout);
                connectClient.Test().ItemCount.ShouldBe(1);
            }
        }

        [Test()]
        [XpandTest][Ignore("")]
        [Apartment(ApartmentState.STA)][Order(300)]
        public async Task Display_TraceEvent_On_New_Client(){
            var dictionary = XpoTypesInfoHelper.GetXpoTypeInfoSource().XPDictionary;
            dictionary.CollectClassInfos(GetType().Assembly);
            dictionary.CollectClassInfos(typeof(ModuleInfo).Assembly);
            using var application = Platform.Win.NewApplication<ReactiveLoggerHubModule>();
            var startServer = application.WhenTraceOnNextEvent(nameof(ReactiveLoggerHubService.StartServer))
                .FirstAsync().SubscribeReplay().SubscribeOn(Scheduler.Default);
            var connecting = TraceEventHub.Connecting.FirstAsync().SubscribeReplay();
            application.AddModule<ReactiveLoggerHubModule>(typeof(RLH));
            application.Model.ToReactiveModule<IModelReactiveModuleLogger>().ReactiveLogger.TraceSources[nameof(ReactiveModule)].Level=SourceLevels.Verbose;
            application.Logon();
            application.CreateObjectSpace();
                
            await startServer.Timeout(Timeout);
            var receive = TraceEventReceiver.TraceEvent.FirstAsync(_ => _.Method==nameof(XafApplicationRxExtensions.WhenDetailViewCreated)).SubscribeReplay();
            var broadcast = TraceEventHub.Trace.FirstAsync(_ => _.Method==nameof(XafApplicationRxExtensions.WhenDetailViewCreated))
                .SubscribeReplay();
            using var clientWinApp = new ClientWinApp {EditorFactory = new EditorsFactory()};
            clientWinApp.AddModule<ReactiveLoggerHubModule>();
            clientWinApp.Model.BOModel.GetClass(typeof(TraceEvent)).DefaultListView.UseServerMode = false;
            clientWinApp.Logon();

                    
            var listView = clientWinApp.NewObjectView<ListView>(typeof(TraceEvent));
            var collectionReloaded = listView.CollectionSource.WhenCollectionReloaded().FirstAsync().SubscribeReplay();
            clientWinApp.CreateViewWindow().SetView(listView);
                    
            await connecting.Timeout(Timeout);

                    
            var detailViewCreated=application.WhenDetailViewCreated().FirstAsync().SubscribeReplay();
                    
                    
                    
            application.NewObjectView<DetailView>(typeof(RLH));


            await broadcast.Timeout(Timeout);
            await receive.Timeout(Timeout);
            await detailViewCreated.Timeout(Timeout);
            await collectionReloaded;
            var events = listView.CollectionSource.Objects<TraceEvent>().ToArray();
            events.FirstOrDefault(_ => _.Method==nameof(XafApplicationRxExtensions.WhenDetailViewCreated)).ShouldNotBeNull();
            events.FirstOrDefault(_ => _.Location==nameof(ReactiveLoggerHubService)).ShouldNotBeNull();
        }

        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)][Order(200)]
        public async Task Display_TraceEvent_On_Running_Client(){
            XpoTypesInfoHelper.GetXpoTypeInfoSource().XPDictionary.CollectClassInfos(GetType().Assembly);
            using var clientWinApp = new ClientWinApp {EditorFactory = new EditorsFactory()};
            clientWinApp.AddModule<ReactiveLoggerHubModule>();
            clientWinApp.Model.BOModel.GetClass(typeof(TraceEvent)).DefaultListView.UseServerMode = false;
            clientWinApp.Logon();
            var listView = clientWinApp.NewObjectView<ListView>(typeof(TraceEvent));
            var viewWindow = clientWinApp.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(),true );
            viewWindow.SetView(listView);

            using var application = Platform.Win.NewApplication<ReactiveLoggerHubModule>();
            var startServer = application.WhenTraceOnNextEvent(nameof(ReactiveLoggerHubService.StartServer))
                .FirstAsync().SubscribeReplay().SubscribeOn(Scheduler.Default);
            var connecting = TraceEventHub.Connecting.FirstAsync().SubscribeReplay();
                    
            application.AddModule<ReactiveLoggerHubModule>(nameof(Display_TraceEvent_On_Running_Client),typeof(RLH));
            application.Model.ToReactiveModule<IModelReactiveModuleLogger>().ReactiveLogger.TraceSources[nameof(ReactiveModule)].Level=SourceLevels.Verbose;
            application.Logon();
            application.CreateObjectSpace();

            await startServer.Timeout(Timeout);
            await connecting.Timeout(Timeout);
                    
            var viewCreated = clientWinApp.WhenTraceOnNextEvent(nameof(XafApplicationRxExtensions.WhenDetailViewCreated))
                .FirstAsync().SubscribeReplay();
            var whenDetailViewCreated = application.WhenDetailViewCreated().FirstAsync().SubscribeReplay();
            application.NewObjectView<DetailView>(typeof(RLH));
            await viewCreated.Timeout(Timeout).ToTaskWithoutConfigureAwait();
            await listView.CollectionSource.WhenCollectionReloaded().FirstAsync();
            await whenDetailViewCreated;
            var events = listView.CollectionSource.Objects<TraceEvent>().ToArray();
            events.FirstOrDefault(_ => _.Method==nameof(XafApplicationRxExtensions.WhenDetailViewCreated)).ShouldNotBeNull();
            events.FirstOrDefault(_ => _.Location==nameof(ReactiveLoggerHubService)).ShouldNotBeNull();
        }

        internal ReactiveLoggerHubModule HubModule(params ModuleBase[] modules){
            var xafApplication = Platform.Win.NewApplication<ReactiveLoggerHubModule>();
            xafApplication.Modules.AddRange(modules);
            return xafApplication.AddModule<ReactiveLoggerHubModule>();
        }


    }
}