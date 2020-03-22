using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.BaseImpl;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.CollectionSource;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Reactive.Logger.Hub.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Services;

using Task = System.Threading.Tasks.Task;

namespace Xpand.XAF.Modules.Reactive.Logger.Hub.Tests{
    [NonParallelizable]
    public class ReactiveLoggerHubTests : BaseTest{
        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public async Task Start_Server_After_Logon(){
            
            using (var application = HubModule(nameof(Start_Server_After_Logon)).Application){
                
                var startServer = application.WhenTraceOnNext(nameof(ReactiveLoggerHubService.StartServer))
                    .FirstAsync().SubscribeReplay();
                var startServerSave = application.WhenTraceOnNextEvent(nameof(ReactiveLoggerHubService.StartServer))
                    .FirstAsync().SubscribeReplay();

                application.Logon();
                application.CreateObjectSpace();
                await startServer.Timeout(Timeout).ToTaskWithoutConfigureAwait();

                await startServerSave.Timeout(Timeout).ToTaskWithoutConfigureAwait();
                
            }
        }

        [Test()]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public async Task Connect_Client(){
            
            using (var clientWinApp = new ClientWinApp()){
                
                clientWinApp.AddModule<ReactiveLoggerHubModule>(typeof(RLH),typeof(BaseObject));
                clientWinApp.Logon();
                clientWinApp.CreateObjectSpace();

                var connectClient = clientWinApp.WhenTraceOnNextEvent(nameof(ReactiveLoggerHubService.ConnectClient)).FirstAsync()
                    .SubscribeOn(Scheduler.Default)
                    .SubscribeReplay();
                using (var application = Platform.Win.NewApplication<ReactiveLoggerHubModule>()){
                    application.AddModule<RXLoggerHubTestsModule>(nameof(Connect_Client), typeof(RLH),typeof(BaseObject));
                    application.Logon();
                    application.CreateObjectSpace();
                    await connectClient.FirstAsync();
                }
                connectClient = clientWinApp.WhenTraceOnNextEvent(nameof(ReactiveLoggerHubService.ConnectClient)).FirstAsync()
                    .SubscribeReplay();
                using (var application = Platform.Win.NewApplication<ReactiveLoggerHubModule>()){
                    application.AddModule<RXLoggerHubTestsModule>($"{nameof(Connect_Client)}_2",typeof(RLH),typeof(BaseObject));
                    application.Logon();
                    application.CreateObjectSpace();

                    await connectClient.FirstAsync();
                    connectClient.Test().ItemCount.ShouldBe(1);
                }
            }
        }

        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public async Task Display_TraceEvent_On_New_Client(){
            XpoTypesInfoHelper.GetXpoTypeInfoSource().XPDictionary.CollectClassInfos(GetType().Assembly);
            using (var application = Platform.Win.NewApplication<ReactiveLoggerHubModule>()){
                
                var startServer = application.WhenTraceOnNextEvent(nameof(ReactiveLoggerHubService.StartServer))
                    .FirstAsync().SubscribeReplay().SubscribeOn(Scheduler.Default);
                var connecting = TraceEventHub.Connecting.FirstAsync().SubscribeReplay();
                application.AddModule<RXLoggerHubTestsModule>(nameof(Display_TraceEvent_On_New_Client),typeof(RLH));
                application.Logon();
                application.CreateObjectSpace();
                
                await startServer.Timeout(Timeout);
                
                using (var clientWinApp = new ClientWinApp()){
                    clientWinApp.EditorFactory=new EditorsFactory();
                    clientWinApp.AddModule<ReactiveLoggerHubModule>();
                    clientWinApp.Logon();

                    
                    var listView = clientWinApp.CreateObjectView<ListView>(typeof(TraceEvent));
                    var collectionReloaded = listView.CollectionSource.WhenCollectionReloaded().FirstAsync().SubscribeReplay();
                    clientWinApp.CreateViewWindow().SetView(listView);
                    
                    await connecting.Timeout(Timeout);

                    var receive = TraceEventReceiver.TraceEvent.FirstAsync(_ => _.Method==nameof(XafApplicationRXExtensions.WhenDetailViewCreated)).SubscribeReplay();
                    var broadcast = TraceEventHub.Broadcasted.FirstAsync(_ => _.Method==nameof(XafApplicationRXExtensions.WhenDetailViewCreated))
                        .SubscribeReplay();
                    var detailViewCreated=application.WhenDetailViewCreated().FirstAsync().SubscribeReplay();
                    
                    
                    
                    application.CreateObjectView<DetailView>(typeof(RLH));


                    await broadcast.Timeout(Timeout);
                    await receive.Timeout(Timeout);
                    await detailViewCreated.Timeout(Timeout);
                    await collectionReloaded;
                    var events = listView.CollectionSource.Objects<TraceEvent>().ToArray();
                    events.FirstOrDefault(_ => _.Method==nameof(XafApplicationRXExtensions.WhenDetailViewCreated)).ShouldNotBeNull();
                    events.FirstOrDefault(_ => _.Location==nameof(ReactiveLoggerHubService)).ShouldNotBeNull();
                }
            }
        }

        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public async Task Display_TraceEvent_On_Running_Client(){
            XpoTypesInfoHelper.GetXpoTypeInfoSource().XPDictionary.CollectClassInfos(GetType().Assembly);
            using (var clientWinApp = new ClientWinApp()){
                clientWinApp.EditorFactory=new EditorsFactory();
                clientWinApp.AddModule<ReactiveLoggerHubModule>();
                clientWinApp.Logon();
                var listView = clientWinApp.CreateObjectView<ListView>(typeof(TraceEvent));
                var viewWindow = clientWinApp.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(),true );
                viewWindow.SetView(listView);
                
                using (var application = Platform.Win.NewApplication<ReactiveLoggerHubModule>()){
                    var startServer = application.WhenTraceOnNextEvent(nameof(ReactiveLoggerHubService.StartServer))
                        .FirstAsync().SubscribeReplay().SubscribeOn(Scheduler.Default);
                    var connecting = TraceEventHub.Connecting.FirstAsync().SubscribeReplay();
                    
                    application.AddModule<RXLoggerHubTestsModule>(nameof(Display_TraceEvent_On_Running_Client),typeof(RLH));
                    application.Logon();
                    application.CreateObjectSpace();

                    await startServer.Timeout(Timeout);
                    await connecting.Timeout(Timeout);
                    
                    var viewCreated = clientWinApp.WhenTraceOnNextEvent(nameof(XafApplicationRXExtensions.WhenDetailViewCreated))
                        .FirstAsync().SubscribeReplay();
                    var whenDetailViewCreated = application.WhenDetailViewCreated().FirstAsync().SubscribeReplay();
                    application.CreateObjectView<DetailView>(typeof(RLH));
                    await viewCreated.Timeout(Timeout).ToTaskWithoutConfigureAwait();
                    await listView.CollectionSource.WhenCollectionReloaded().FirstAsync();
                    await whenDetailViewCreated;
                    var events = listView.CollectionSource.Objects<TraceEvent>().ToArray();
                    events.FirstOrDefault(_ => _.Method==nameof(XafApplicationRXExtensions.WhenDetailViewCreated)).ShouldNotBeNull();
                    events.FirstOrDefault(_ => _.Location==nameof(ReactiveLoggerHubService)).ShouldNotBeNull();
                }
                

            }
        }

        internal ReactiveLoggerHubModule HubModule(string title,params ModuleBase[] modules){
            var xafApplication = Platform.Win.NewApplication<ReactiveLoggerHubModule>();
            xafApplication.Modules.AddRange(modules);
            return xafApplication.AddModule<ReactiveLoggerHubModule>(title);
        }


    }
}