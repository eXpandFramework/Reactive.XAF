using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using Shouldly;
using TestsLib;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Logger.Tests.BOModel;
using Xunit;

namespace Xpand.XAF.Modules.Reactive.Logger.Tests{
    [Collection(nameof(ReactiveLoggerModule))]
    public class ReactiveLoggerTests : BaseTest{
        [Theory]
        [InlineData("NoLastEvent")]
        [InlineData("DifferentLastEvent")]
        [InlineData("SameLastEvent")]
        public void Update_traceEvent_calls(string when){
            using (var application = LoggerModule().Application){
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

                var save = objectSpace.SaveTraceEvent(new ITraceEvent[]{message1,message1,message2}).SubscribeReplay();

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

        [Fact]
        public  void Populate_TracedSource_Modules_to_Model(){
            ReactiveModule.TraceSource.Switch = new SourceSwitch("SourceSwitch", "Verbose");
            using (var application = LoggerModule().Application){
                var logger = application.Model.ToReactiveModule<IModelReactiveModuleLogger>().ReactiveLogger;
                logger.TraceSources.Count.ShouldBeGreaterThan(5);
                var module = logger.TraceSources[nameof(ReactiveLoggerModule)];
                module.ShouldNotBeNull();
                
                module.Level.ShouldBe(SourceLevels.Verbose);

                module = logger.TraceSources.Last();
                
                module.Level.ShouldBe(SourceLevels.Verbose);
            }

            
        }


        [WinFormsFact]
        public async Task Save_TraceEvent(){
            using (var application = Platform.Win.NewApplication()){
                application.AddModule<ReactiveLoggerModule>();
                application.Logon();
                var listView = application.CreateObjectView<ListView>(typeof(TraceEvent));
                application.CreateViewWindow().SetView(listView);
                
                var test = application.WhenTraceEvent().FirstAsync(_ => _.Value == "test").SubscribeReplay();

                ReactiveLoggerModule.TraceSource.TraceMessage("test");

                await test;
                var objectSpace = application.CreateObjectSpace();
                objectSpace.GetObjectsQuery<TraceEvent>().FirstOrDefault(_ => _.Value.Contains("test")).ShouldNotBeNull();
            }
        }

        [WinFormsFact]
        public async Task Refresh_TraceEvent_ListView_when_trace(){
            using (var application = Platform.Win.NewApplication()){
                application.AddModule<ReactiveLoggerModule>();
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

        internal ReactiveLoggerModule LoggerModule(params ModuleBase[] modules){
            var xafApplication = Platform.Win.NewApplication();
            xafApplication.Modules.AddRange(modules);
            return xafApplication.AddModule<ReactiveLoggerModule>(typeof(RL));
        }


    }
}