using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using AppDomainToolkit;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.XAF.Modules.Reactive.Logger.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive.Logger.Tests{
    [NonParallelizable]
    [Serializable]
    public class ReactiveLoggerTests : BaseTest{
        
        [TestCase("NoLastEvent")]
        [TestCase("DifferentLastEvent")]
        [TestCase("SameLastEvent")]
        public void Update_traceEvent_calls(string when){
            
            using (var application = Platform.Win.NewApplication<ReactiveLoggerModule>()){
                application.AddModule<TestReactiveLoggerModule>();

                application.Title = nameof(Update_traceEvent_calls);
                
                var objectSpace = application.CreateObjectSpace();
                if (when=="DifferentLastEvent"){
                    objectSpace.CreateObject<TraceEvent>();
                    objectSpace.CommitChanges();
                }
                else if (when=="SameLastEvent"){
                    var traceEvent = objectSpace.CreateObject<TraceEvent>();
                    traceEvent.Source = "source";
                    traceEvent.Location = "Location";
                    traceEvent.Method = "Method";
                    traceEvent.Action = "Action";
                    objectSpace.CommitChanges();
                }
                var message1 = new TraceEventMessage(){
                    Source = "source",Location = "Location",Method = "Method",Action = "Action"
                };
                var message2 = new TraceEventMessage(){
                    Source = "source",Location = "Location",Method = "Method",Action = "Action2"
                };

                var messages = new ITraceEvent[]{message1,message1,message2};
                var save = objectSpace.SaveTraceEvent(messages).SubscribeReplay();

                var testObserver = save.Test();
                testObserver.ItemCount.ShouldBe(2);
                if (when!="SameLastEvent"){
                    testObserver.Items[0].Called.ShouldBe(2);
                    testObserver.Items[1].Called.ShouldBe(1);
                }
                else{
                    testObserver.Items[0].Called.ShouldBe(3);
                    testObserver.Items[1].Called.ShouldBe(1);
                }
                
                
            }
        }

        [Test]
        public  void Populate_TracedSource_Modules_to_Model(){
            
            using (var application = LoggerModule(nameof(Populate_TracedSource_Modules_to_Model)).Application){
                var logger = application.Model.ToReactiveModule<IModelReactiveModuleLogger>().ReactiveLogger;
                logger.TraceSources.Count.ShouldBeGreaterThan(5);
                var module = logger.TraceSources[nameof(ReactiveLoggerModule)];
                module.ShouldNotBeNull();
                
                module.Level.ShouldBe(SourceLevels.Verbose);

                module = logger.TraceSources.Last();
                
                module.Level.ShouldBe(SourceLevels.Verbose);
            }
        }


        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task SaveTrace_When_AuthendiationStandard(){
            await SaveTraceEvent(application => {
                    application.SetupSecurity();
                });
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Do_Not_Trace_If_TraceSources_Level_Off(){

            Should.Throw<TimeoutException>(async () => {
                await SaveTraceEvent(application => {
                    application.WhenModelChanged().FirstAsync()
                        .Select(_ => {
                            var logger = application.Model.ToReactiveModule<IModelReactiveModuleLogger>().ReactiveLogger;
                            logger.TraceSources[nameof(ReactiveLoggerModule)].Level = SourceLevels.Off;
                            return Unit.Default;
                        }).Subscribe();
                });
            });
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Save_TraceEvent(){
            await SaveTraceEvent(afterSaveTrace: async () => {
                using (var appDomainContext2 = AppDomainContext.Create(AppDomain.CurrentDomain.SetupInformation)){
                    await RemoteFuncAsync.InvokeAsync(appDomainContext2.Domain, async () => {
                        await SaveTraceEvent();
                        return Unit.Default;
                    });
                    appDomainContext2.Dispose();
                }
            });
        }

        private static async Task SaveTraceEvent(Action<XafApplication> created=null, Action afterSaveTrace=null){
            using (var application = Platform.Win.NewApplication<ReactiveLoggerModule>(false)){
                created?.Invoke(application);
                application.AddModule<TestReactiveLoggerModule>();
                application.Title = nameof(Save_TraceEvent);
                application.CreateObjectSpace();
                var test = application.WhenTraceEvent().FirstAsync(_ => _.Value == "test").SubscribeReplay(Scheduler.Default);
                ReactiveLoggerModule.TraceSource.TraceMessage("test");
                application.Logon();

                await test.Timeout(TimeSpan.FromSeconds(1));
                var objectSpace = application.CreateObjectSpace();
                objectSpace.GetObjectsQuery<TraceEvent>().FirstOrDefault(_ => _.Value.Contains("test")).ShouldNotBeNull();

                afterSaveTrace?.Invoke();
            }
        }


        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Refresh_TraceEvent_ListView_when_trace(){
            using (var application = Platform.Win.NewApplication<ReactiveLoggerModule>()){
                application.AddModule<TestReactiveLoggerModule>();
                application.Title = nameof(Refresh_TraceEvent_ListView_when_trace);
                application.Logon();
                var listView = application.CreateObjectView<ListView>(typeof(TraceEvent));
                application.CreateViewWindow().SetView(listView);
                var refresh = application.WhenTraceEvent(typeof(ReactiveLoggerService), RXAction.OnNext,
                    nameof(ReactiveLoggerService.RefreshViewDataSource)).FirstAsync().SubscribeReplay();
                var test = application.WhenTraceEvent().FirstAsync(_ => _.Value == "test").SubscribeReplay();
                ReactiveLoggerModule.TraceSource.TraceMessage("test");

                await refresh;
                await test;
                
                
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Trace_Events_Before_CompatibityCheck(){
            using (var application = Platform.Win.NewApplication<ReactiveLoggerModule>()){
                application.Title = nameof(Trace_Events_Before_CompatibityCheck);
                var reactiveLoggerModule = new ReactiveLoggerModule();
                var traceSetup = application.WhenTraceEvent(typeof(ReactiveModuleBase),RXAction.OnNext,nameof(ReactiveLoggerModule.SetupCompleted))
                    .FirstAsync(_ => _.Value==nameof(ReactiveLoggerModule)).SubscribeReplay();
                var setup = reactiveLoggerModule.SetupCompleted.FirstAsync().SubscribeReplay();
                application.Modules.Add(reactiveLoggerModule);
                application.AddModule<TestReactiveLoggerModule>();
                application.Logon();
                application.CreateObjectSpace();

                await setup;
                await traceSetup.Timeout(Timeout);
            }
        }

        internal ReactiveLoggerModule LoggerModule(string title,params ModuleBase[] modules){
            var xafApplication = Platform.Win.NewApplication<ReactiveLoggerModule>();
            xafApplication.Title = title;
            xafApplication.Modules.AddRange(modules);
            var module = xafApplication.AddModule<TestReactiveLoggerModule>(typeof(RL));
            xafApplication.Logon();
            xafApplication.CreateObjectSpace();
            return module.Application.Modules.OfType<ReactiveLoggerModule>().First();
        }
    }
}