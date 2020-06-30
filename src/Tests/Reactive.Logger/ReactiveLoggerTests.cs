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
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Reactive.Logger.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive.Logger.Tests{
    [NonParallelizable]
    [Serializable]
    public class ReactiveLoggerTests : BaseTest{
        [XpandTest]
        [TestCase("NoLastEvent")]
        [TestCase("DifferentLastEvent")]
        [TestCase("SameLastEvent")]
        public void Update_traceEvent_calls(string when){
            
            using (var application = Platform.Win.NewApplication<ReactiveLoggerModule>()){
                application.AddModule<TestReactiveLoggerModule>();

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
        [XpandTest]
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

        // [Test][Ignore(NotImplemented)]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public async Task Save_TraceEvent(){
            await SaveTraceEvent(afterSaveTrace: async () => {
                using (var appDomainContext2 = AppDomainContext.Create(AppDomain.CurrentDomain.SetupInformation)){
                    await RemoteFuncAsync.InvokeAsync(appDomainContext2.Domain, async () => {
                        await SaveTraceEvent().ConfigureAwait(false);
                        return Unit.Default;
                    }).ConfigureAwait(false);
                    appDomainContext2.Dispose();
                }
            });
        }

        private static async Task SaveTraceEvent(Action<XafApplication> created=null, Action afterSaveTrace=null){
            using (var application = Platform.Win.NewApplication<ReactiveLoggerModule>(false)){
                created?.Invoke(application);
                application.AddModule<ReactiveLoggerModule>();
                application.CreateObjectSpace();
                var test = application.WhenTraceEvent().FirstAsync(_ => _.Value == "test").SubscribeReplay(Scheduler.Default);
                ReactiveLoggerModule.TraceSource.TraceMessage("test");
                application.Logon();

                await test.Timeout(TimeSpan.FromSeconds(1)).ToTaskWithoutConfigureAwait();
                var objectSpace = application.CreateObjectSpace();
                objectSpace.GetObjectsQuery<TraceEvent>().FirstOrDefault(_ => _.Value.Contains("test")).ShouldNotBeNull();

                afterSaveTrace?.Invoke();
            }
        }

        [Test]
        [XpandTest]
        public  void Populate_TracedSource_Modules_to_Model(){
            
	        using (var application = LoggerModule().Application){
		        var logger = application.Model.ToReactiveModule<IModelReactiveModuleLogger>().ReactiveLogger;
		        logger.TraceSources.Count.ShouldBeGreaterThanOrEqualTo(TestsLib.Extensions.ModulePorts.Count-2);
		        var module = logger.TraceSources[nameof(ReactiveLoggerModule)];
		        module.ShouldNotBeNull();
                
		        module.Level.ShouldBe(SourceLevels.Verbose);

	        }
        }


        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public async Task Refresh_TraceEvent_ListView_when_trace(){
            using (var application = LoggerModule().Application){
	            var listView = application.NewObjectView<ListView>(typeof(TraceEvent));
                application.CreateViewWindow().SetView(listView);
                var refresh = application.WhenTraceEvent(typeof(ReactiveLoggerService), RXAction.OnNext,
                    nameof(ReactiveLoggerService.RefreshViewDataSource)).FirstAsync().SubscribeReplay();
                var test = application.WhenTraceEvent().FirstAsync(_ => _.Value == "test").SubscribeReplay();
                ReactiveLoggerModule.TraceSource.TraceMessage("test");

                await refresh.ToTaskWithoutConfigureAwait();
                await test;
                
                
            }
        }

        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public async Task Trace_Events_Before_CompatibityCheck(){
            using (var application = Platform.Win.NewApplication<ReactiveLoggerModule>()){
                
                application.AddModule<ReactiveLoggerModule>();
                var test = application.WhenTraceEvent().FirstAsync(_ => _.Value == "test").SubscribeReplay();
                ReactiveLoggerModule.TraceSource.TraceMessage("test");
                application.Logon();
                application.CreateObjectSpace();

                await test.Timeout(Timeout);
            }
        }

        internal ReactiveLoggerModule LoggerModule(params ModuleBase[] modules){
            var xafApplication = Platform.Win.NewApplication<ReactiveLoggerModule>();
            xafApplication.Modules.AddRange(modules);
            var module = xafApplication.AddModule<ReactiveLoggerModule>(typeof(RL));
            xafApplication.Logon();
            xafApplication.CreateObjectSpace();
            return module.Application.Modules.OfType<ReactiveLoggerModule>().First();
        }
    }
}