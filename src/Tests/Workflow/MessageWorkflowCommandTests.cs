using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Workflow.BusinessObjects;
using Xpand.XAF.Modules.Workflow.BusinessObjects.Commands;
using Xpand.XAF.Modules.Workflow.Services;
using Xpand.XAF.Modules.Workflow.Tests.BOModel;
using Xpand.XAF.Modules.Workflow.Tests.Common;

namespace Xpand.XAF.Modules.Workflow.Tests {
    public class MessageWorkflowCommandTests : BaseWorkflowTest {
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Shows_UI_Notification_With_Default_Settings() {
            await using var application = NewApplication();
            WorkflowModule(application);

            var messageOptionsSubject = new ReplaySubject<MessageOptions>();

            application.WhenCustomizeMessage()
                .SelectMany(e => {
                    e.Handled = true;
                    return e.Instance
                        .Do(options => {
                            messageOptionsSubject.OnNext(options);
                            messageOptionsSubject.OnCompleted();
                        });
                })
                .Subscribe();

            IObservable<Unit> TestLogic(Frame frame) => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
                    var commandA = space.CreateObject<TestCommand>();
                    commandA.Id = "CommandA";
                    commandA.CommandSuite = suite;
                    var messenger = space.CreateObject<MessageWorkflowCommand>();
                    messenger.StartAction = commandA;
                    messenger.CommandSuite = suite;
                    return suite.Commit();
                }).ConcatToUnit(messageOptionsSubject).ToUnit();

            await application.StartWinTest(TestLogic).FirstOrDefaultAsync();

            var messageOptions = await messageOptionsSubject.FirstAsync();
            messageOptions.ShouldNotBeNull();
            messageOptions.Message.ShouldContain("42");
            messageOptions.Message.ShouldContain("test-string");
            messageOptions.Type.ShouldBe(InformationType.Info);
        }
        
    
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Passes_Input_Through_To_Next_Command_Unchanged() {
            await using var application = NewApplication();
            WorkflowModule(application);

            var sinkReceivedSignal = new ReplaySubject<object[]>();

            IObservable<TestCommand> testLogic = application.UseProviderObjectSpace(space => {
                var suite = space.CreateObject<CommandSuite>();

                var dataSource = space.CreateObject<TestCommand>();
                dataSource.Id = "CommandA";
                dataSource.CommandSuite = suite;

                var messenger = space.CreateObject<MessageWorkflowCommand>();
                messenger.StartAction = dataSource;
                messenger.CommandSuite = suite;

                var dataSink = space.CreateObject<TestCommand>();
                dataSink.Id = "DataSink";
                dataSink.StartAction = messenger;
                dataSink.CommandSuite = suite;

                return suite.Commit()
                    .SelectMany(_ => dataSink.WhenExecuted().Take(1).Do(objects => {
                        sinkReceivedSignal.OnNext(objects);
                        sinkReceivedSignal.OnCompleted();
                    }))
                    .To(dataSource);
            });

            await application.StartWinTest(_ => testLogic.ConcatToUnit(sinkReceivedSignal)).FirstOrDefaultAsync();

            var receivedObjects = await sinkReceivedSignal.FirstAsync();
    
            receivedObjects.ShouldNotBeNull();
            receivedObjects.Length.ShouldBe(1);
            receivedObjects[0].ShouldBe("42;test-string");
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Formats_Message_With_Context_When_VerboseNotification_Is_True() {
            await using var application = NewApplication();
            WorkflowModule(application);

            var messageOptionsSubject = new ReplaySubject<MessageOptions>();

            using var _=application.WhenCustomizeMessage()
                .SelectMany(e => {
                    e.Handled = true;
                    return e.Instance
                        .Do(options => {
                            messageOptionsSubject.OnNext(options);
                            messageOptionsSubject.OnCompleted();
                        });
                })
                .Subscribe();

            IObservable<CommandSuite> testLogic = application.UseProviderObjectSpace(space => {
                var suite = space.CreateObject<CommandSuite>();
                suite.Name = "VerboseSuite";

                var dataSource = space.CreateObject<TestCommand>();
                dataSource.Id = "CommandA";
                dataSource.CommandSuite = suite;

                var messenger = space.CreateObject<MessageWorkflowCommand>();
                messenger.StartAction = dataSource;
                messenger.CommandSuite = suite;
                messenger.Verbose = true;

                return suite.Commit();
            });

            await application.StartWinTest(_ => testLogic.ConcatToUnit(messageOptionsSubject)) ;

            var messageOptions = await messageOptionsSubject.FirstAsync();
    
            messageOptions.ShouldNotBeNull();
            messageOptions.Message.ShouldContain("Suite: VerboseSuite");
            messageOptions.Message.ShouldContain("Command: Message");
            messageOptions.Message.ShouldContain("Int32: ");
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Completes_Silently_When_Input_Is_Empty() {
            await using var application = NewApplication();
            WorkflowModule(application);

            var notificationShown = false;
            var sinkExecuted = false;

            application.WhenCustomizeMessage().Take(1)
                .Do(_ => notificationShown = true)
                .Subscribe();

            IObservable<CommandSuite> testLogic = application.UseProviderObjectSpace(space => {
                var suite = space.CreateObject<CommandSuite>();

                var dataSource = space.CreateObject<TestCommand>();
                dataSource.Id = "DataSource";
                dataSource.ReturnEmpty = true;
                dataSource.CommandSuite = suite;

                var messenger = space.CreateObject<MessageWorkflowCommand>();
                messenger.StartAction = dataSource;
                messenger.CommandSuite = suite;

                var dataSink = space.CreateObject<TestCommand>();
                dataSink.Id = "DataSink";
                dataSink.StartAction = messenger;
                dataSink.CommandSuite = suite;

                return suite.Commit()
                    .SelectMany(_ => dataSink.WhenExecuted().Take(1).Do(_ => sinkExecuted = true))
                    .To(suite);
            });

            await application.StartWinTest(_ => testLogic).FirstOrDefaultAsync();

            notificationShown.ShouldBeFalse("A notification should not be shown for empty input.");
            sinkExecuted.ShouldBeTrue("The workflow should have continued to the next command.");
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Handles_Null_Values_In_Input_Array_Gracefully() {
            await using var application = NewApplication();
            WorkflowModule(application);

            var sinkExecutedSignal = new ReplaySubject<Unit>();
            var messageSignal = new ReplaySubject<Unit>();

            application.WhenCustomizeMessage().Take(1)
                .Do(_ => {
                    messageSignal.OnNext(Unit.Default);
                    messageSignal.OnCompleted();
                })
                .Subscribe();

            application.UseProviderObjectSpace(space => {
                var suite = space.CreateObject<CommandSuite>();

                var wf1 = space.CreateObject<WF>();
                wf1.Name = "ValidName";
                var wf2 = space.CreateObject<WF>();
                wf2.Name = null;

                var sourceCmd = space.CreateObject<ObjectExistWorkflowCommand>();
                sourceCmd.Object = sourceCmd.ObjectTypes.First(s => s.Name == typeof(WF).FullName);
                sourceCmd.OutputProperty = nameof(WF.Name);
                sourceCmd.CommandSuite = suite;

                var messenger = space.CreateObject<MessageWorkflowCommand>();
                messenger.StartAction = sourceCmd;
                messenger.CommandSuite = suite;

                var dataSink = space.CreateObject<TestCommand>();
                dataSink.Id = "DataSink";
                dataSink.StartAction = messenger;
                dataSink.CommandSuite = suite;

                return suite.Commit()
                    .SelectMany(_ => dataSink.WhenExecuted().Take(1).Do(_ => {
                        sinkExecutedSignal.OnNext(Unit.Default);
                        sinkExecutedSignal.OnCompleted();
                    }))
                    .To(suite);
            })
            .Subscribe();

            await application.StartWinTest(_ => sinkExecutedSignal.Merge(messageSignal));

            
            BusEvents.ShouldBeEmpty("No errors should be published when handling null input.");
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Correctly_Formats_IDefaultProperty_Objects_In_Message() {
            await using var application = NewApplication();
            WorkflowModule(application);

            var messageOptionsSubject = new ReplaySubject<MessageOptions>();

            application.WhenCustomizeMessage().Take(1)
                .SelectMany(e => {
                    e.Handled = true;
                    return e.Instance
                        .Do(options => {
                            messageOptionsSubject.OnNext(options);
                            messageOptionsSubject.OnCompleted();
                        });
                })
                .Subscribe();

            IObservable<CommandSuite> testLogic = application.UseProviderObjectSpace(space => {
                var suite = space.CreateObject<CommandSuite>();

                var commandToFind = space.CreateObject<TestCommand>();
                commandToFind.Id = "FindMe";
                commandToFind.CommandSuite = suite;

                var sourceCmd = space.CreateObject<ObjectExistWorkflowCommand>();
                sourceCmd.Object = sourceCmd.ObjectTypes.First(s => s.Name == typeof(TestCommand).FullName);
                sourceCmd.CommandSuite = suite;

                var messenger = space.CreateObject<MessageWorkflowCommand>();
                messenger.StartAction = sourceCmd;
                messenger.CommandSuite = suite;

                return suite.Commit();
            });

            await application.StartWinTest(_ => testLogic.ConcatToUnit(messageOptionsSubject)) ;

            var messageOptions = await messageOptionsSubject.FirstAsync();
    
            messageOptions.ShouldNotBeNull();
            messageOptions.Message.ShouldContain("Test Command: Test Command->Id: FindMe");
            messageOptions.Message.ShouldNotContain(typeof(TestCommand).FullName!);
        }
    }
}