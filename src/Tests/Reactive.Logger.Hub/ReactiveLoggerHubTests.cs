using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.BaseImpl;
using Shouldly;
using TestsLib;
using Xpand.Source.Extensions.XAF.CollectionSource;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Logger.Hub.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Services;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Xpand.XAF.Modules.Reactive.Logger.Hub.Tests{
    [Collection(nameof(ReactiveLoggerHubModule))]
    public class ReactiveLoggerHubTests : BaseTest{
        public ReactiveLoggerHubTests(){
            XpoTypesInfoHelper.GetXpoTypeInfoSource().XPDictionary.CollectClassInfos(GetType().Assembly);
        }

        [WinFormsFact]
        public async Task Start_Server_After_Logon(){
            
            using (var application = HubModule(nameof(Start_Server_After_Logon)).Application){
                
                var startServer = application.WhenTraceOnNext(nameof(ReactiveLoggerHubService.StartServer))
                    .FirstAsync().SubscribeReplay();
                var startServerSave = application.WhenTraceOnNextEvent(nameof(ReactiveLoggerHubService.StartServer))
                    .FirstAsync().SubscribeReplay();

                application.Logon();
                await startServer.Timeout(Timeout);
                application.CreateObjectSpace();
                await startServerSave.Timeout(Timeout);
            }
        }

        [WinFormsFact(Skip = NotImplemented)]
        public async Task Connect_Client(){
            
            using (var clientWinApp = new ClientWinApp()){
                
                clientWinApp.AddModule<ReactiveLoggerHubModule>(typeof(RLH),typeof(BaseObject));
                clientWinApp.Logon();
                clientWinApp.CreateObjectSpace();

                var connectClient = clientWinApp.WhenTraceOnNextEvent(nameof(ReactiveLoggerHubService.ConnectClient)).FirstAsync()
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

        [WinFormsFact]
        public async Task Client_Detect_When_Hub_Is_Online(){
            
            using (var clientWinApp = new ClientWinApp()){
                clientWinApp.AddModule<ReactiveLoggerHubModule>(typeof(RLH),typeof(BaseObject));
                clientWinApp.Logon();
                clientWinApp.CreateObjectSpace();
                
                var detectOnlineHubTrace = clientWinApp.WhenTraceOnNextEvent(nameof(ReactiveLoggerHubService.DetectOnlineHub))
                    .SubscribeReplay();
                var detectOffline = clientWinApp.WhenTraceOnSubscribeEvent(nameof(ReactiveLoggerHubService.DetectOffLineHub)).SubscribeReplay();
                using (var application = Platform.Win.NewApplication<ReactiveLoggerHubModule>()){
                    
                    application.AddModule<RXLoggerHubTestsModule>(nameof(Client_Detect_When_Hub_Is_Online),typeof(RLH),typeof(BaseObject));
                    application.Logon();
                    application.CreateObjectSpace();
                    await detectOnlineHubTrace.FirstAsync().Timeout(Timeout);
                }

                await detectOffline.FirstAsync().Timeout(Timeout);
            }
        }


        [WinFormsFact]
        public async Task Display_TraceEvent_On_New_Client(){
            using (var application = Platform.Win.NewApplication<ReactiveLoggerHubModule>()){
                
                var startServer = application.WhenTraceOnNextEvent(nameof(ReactiveLoggerHubService.StartServer))
                    .FirstAsync().SubscribeReplay();
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
                    clientWinApp.CreateViewWindow().SetView(listView);
                    
                    await connecting.Timeout(Timeout);

                    var receive = TraceEventReceiver.TraceEvent.FirstAsync(_ => _.Method==nameof(XafApplicationRXExtensions.WhenDetailViewCreated)).SubscribeReplay();
                    var broadcast = TraceEventHub.Broadcasted.FirstAsync(_ => _.Method==nameof(XafApplicationRXExtensions.WhenDetailViewCreated))
                        .SubscribeReplay();
                    application.WhenDetailViewCreated().SubscribeReplay();
                    
                    
                    
                    application.CreateObjectView<DetailView>(typeof(RLH));


                    await broadcast.Timeout(Timeout);
                    await receive.Timeout(Timeout);

                    var events = listView.CollectionSource.Objects<TraceEvent>().ToArray();
                    events.FirstOrDefault(_ => _.Method==nameof(XafApplicationRXExtensions.WhenDetailViewCreated)).ShouldNotBeNull();
                    events.FirstOrDefault(_ => _.Location==nameof(ReactiveLoggerHubService)).ShouldNotBeNull();
                }
            }
        }

        [WinFormsFact]
        public async Task Display_TraceEvent_On_Running_Client(){
            
            using (var clientWinApp = new ClientWinApp()){
                clientWinApp.EditorFactory=new EditorsFactory();
                clientWinApp.AddModule<ReactiveLoggerHubModule>();
                clientWinApp.Logon();
                var listView = clientWinApp.CreateObjectView<ListView>(typeof(TraceEvent));
                var viewWindow = clientWinApp.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(),true );
                viewWindow.SetView(listView);
                
                using (var application = Platform.Win.NewApplication<ReactiveLoggerHubModule>()){
                    var startServer = application.WhenTraceOnNextEvent(nameof(ReactiveLoggerHubService.StartServer))
                        .FirstAsync().SubscribeReplay();
                    var connecting = TraceEventHub.Connecting.FirstAsync().SubscribeReplay();
                    
                    application.AddModule<RXLoggerHubTestsModule>(nameof(Display_TraceEvent_On_Running_Client),typeof(RLH));
                    application.Logon();
                    application.CreateObjectSpace();

                    await startServer.Timeout(Timeout);
                    await connecting.Timeout(Timeout);
                    
                    var viewCreated = clientWinApp.WhenTraceOnNextEvent(nameof(XafApplicationRXExtensions.WhenDetailViewCreated))
                        .FirstAsync().SubscribeReplay();
                    application.WhenDetailViewCreated().SubscribeReplay();
                    application.CreateObjectView<DetailView>(typeof(RLH));
                    await viewCreated.Timeout(Timeout).ToTaskWithoutConfigureAwait();
                    await listView.CollectionSource.WhenCollectionReloaded().FirstAsync();
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