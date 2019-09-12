using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win.Templates;
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

        [Fact]
        public async Task Client_Detect_When_Hub_Is_Online(){
            
            using (var clientWinApp = new ClientWinApp()){
                clientWinApp.AddModule<ReactiveLoggerHubModule>(typeof(RLH),typeof(BaseObject));
                clientWinApp.Logon();
                var window = clientWinApp.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(), true);
                window.SetTemplate(new MainForm());
                clientWinApp.CreateObjectSpace();
                
                var detectOnlineHubTrace = clientWinApp.WhenTraceEvent(typeof(ReactiveLoggerHubService), RXAction.OnNext,
                    nameof(ReactiveLoggerHubService.DetectOnlineHub)).SubscribeReplay();
                
                using (var application = Platform.Win.NewApplication()){
                    
                    application.AddModule<RXLoggerHubTestsModule>(nameof(Client_Detect_When_Hub_Is_Online),typeof(RLH),typeof(BaseObject));
                    application.Logon();
                    application.CreateObjectSpace();
                    await detectOnlineHubTrace.FirstAsync();
                    application.Dispose();
                }
            }
        }

        [Fact]
        public async Task Client_Detect_When_Hub_Is_Offline(){
            using (var clientWinApp = new ClientWinApp()){
                clientWinApp.AddModule<ReactiveLoggerHubModule>(typeof(RLH),typeof(BaseObject));
                clientWinApp.Logon();
                var window = clientWinApp.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(), true);
                window.SetTemplate(new MainForm());

                var modelServerPort = await (clientWinApp.ServerPortsList().FirstAsync());
                var detectOnlineHub = modelServerPort.DetectOnlineHub().SubscribeReplay();
                using (var application = Platform.Win.NewApplication()){
                    application.AddModule<RXLoggerHubTestsModule>(nameof(Client_Detect_When_Hub_Is_Offline), typeof(RLH),typeof(BaseObject));
                    application.Logon();
                    await detectOnlineHub.FirstAsync();
                }
                using (var application = Platform.Win.NewApplication()){
                    application.AddModule<RXLoggerHubTestsModule>(nameof(Client_Detect_When_Hub_Is_Offline),typeof(RLH),typeof(BaseObject));
                    application.Logon();
                    await detectOnlineHub.Skip(1).FirstAsync();
                }
            }
        }

        [WinFormsFact]
        public async Task Connect_Client(){
            using (var clientWinApp = new ClientWinApp()){
                
                clientWinApp.AddModule<ReactiveLoggerHubModule>(typeof(RLH),typeof(BaseObject));
                clientWinApp.Logon();
                var listView = clientWinApp.CreateObjectView<ListView>(typeof(TraceEvent));
                var viewWindow = clientWinApp.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(),true );
                viewWindow.SetView(listView);
                var connectClient = clientWinApp.WhenTraceEvent(typeof(ReactiveLoggerHubService), RXAction.OnNext, nameof(ReactiveLoggerHubService.ConnectClient))
                    .SubscribeReplay();
                using (var application = Platform.Win.NewApplication()){
                    application.AddModule<RXLoggerHubTestsModule>(nameof(Connect_Client), typeof(RLH),typeof(BaseObject));
                    application.Logon();
                    await connectClient.FirstAsync();
                }

                using (var application = Platform.Win.NewApplication()){
                    application.AddModule<RXLoggerHubTestsModule>($"{nameof(Connect_Client)}_2",typeof(RLH),typeof(BaseObject));

                    await connectClient.Skip(1).FirstAsync();
                    Should.Throw<TimeoutException>(async () => await connectClient.Skip(2).FirstAsync().Timeout(TimeSpan.FromSeconds(1)));
                }
            }
        }


        [WinFormsFact]
        public async Task Display_TraceEvent_On_New_Client(){
            using (var application = Platform.Win.NewApplication()){
                
                var startServer = application.WhenTraceEvent(typeof(ReactiveLoggerHubService), RXAction.OnNext,
                    nameof(ReactiveLoggerHubService.StartServer)).FirstAsync().SubscribeReplay();
                var connecting = TraceEventHub.Connecting.FirstAsync().SubscribeReplay();
                application.AddModule<RXLoggerHubTestsModule>(nameof(Display_TraceEvent_On_New_Client),typeof(RLH));
                application.CreateObjectSpace();
                application.Logon();
                
                await startServer;

                using (var clientWinApp = new ClientWinApp()){
                    clientWinApp.AddModule<ReactiveLoggerHubModule>();
                    
                    clientWinApp.Logon();
                    var listView = clientWinApp.CreateObjectView<ListView>(typeof(TraceEvent));
                    var viewWindow = clientWinApp.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(),true );
                    viewWindow.SetView(listView);
                    
                    
                    await connecting;
                    
                    var viewCreated = clientWinApp.WhenTraceEvent(typeof(XafApplicationRXExtensions), RXAction.All,
                        nameof(XafApplicationRXExtensions.WhenDetailViewCreated)).FirstAsync().SubscribeReplay();
                    application.WhenDetailViewCreated().SubscribeReplay();
                    application.CreateObjectView<DetailView>(typeof(RLH));
                    await viewCreated;

                    var events = listView.CollectionSource.Objects<TraceEvent>().ToArray();
                    events.FirstOrDefault(_ => _.Method==nameof(XafApplicationRXExtensions.WhenDetailViewCreated)).ShouldNotBeNull();
                    events.FirstOrDefault(_ => _.Location==nameof(ReactiveLoggerHubService)).ShouldNotBeNull();

                }
            }
        }

        [WinFormsFact]
        public async Task Display_TraceEvent_On_Running_Client(){
            
            using (var clientWinApp = new ClientWinApp()){
                clientWinApp.AddModule<ReactiveLoggerHubModule>();
                var listView = clientWinApp.CreateObjectView<ListView>(typeof(TraceEvent));
                var viewWindow = clientWinApp.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(),true );
                viewWindow.SetView(listView);
                clientWinApp.CreateObjectSpace();
                clientWinApp.Logon();
            
                using (var application = Platform.Win.NewApplication()){
                    var startServer = application.WhenTraceEvent(typeof(ReactiveLoggerHubService), RXAction.OnNext,
                        nameof(ReactiveLoggerHubService.StartServer)).FirstAsync().SubscribeReplay();
                    var connecting = TraceEventHub.Connecting.FirstAsync().SubscribeReplay();
                    
                    application.AddModule<RXLoggerHubTestsModule>(nameof(Display_TraceEvent_On_Running_Client),typeof(RLH));
                    
                    application.CreateObjectSpace();
                    application.Logon();

                    await startServer;
                    await connecting;
                    
                    var viewCreated = clientWinApp.WhenTraceEvent(typeof(XafApplicationRXExtensions), RXAction.All,
                        nameof(XafApplicationRXExtensions.WhenDetailViewCreated)).FirstAsync().SubscribeReplay();
                    application.WhenDetailViewCreated().SubscribeReplay();
                    application.CreateObjectView<DetailView>(typeof(RLH));
                    await viewCreated;

                    var events = listView.CollectionSource.Objects<TraceEvent>().ToArray();
                    events.FirstOrDefault(_ => _.Method==nameof(XafApplicationRXExtensions.WhenDetailViewCreated)).ShouldNotBeNull();
                    events.FirstOrDefault(_ => _.Location==nameof(ReactiveLoggerHubService)).ShouldNotBeNull();
                }
                

            }
        }

        internal ReactiveLoggerHubModule HubModule(params ModuleBase[] modules){
            var xafApplication = Platform.Win.NewApplication();
            xafApplication.Modules.AddRange(modules);
            return xafApplication.AddModule<ReactiveLoggerHubModule>();
        }


    }
}