using System;
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
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Workflow.BusinessObjects;
using Xpand.XAF.Modules.Workflow.BusinessObjects.Commands;
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

            await application.StartWinTest(TestLogic)
                .Timeout(30.Seconds()).FirstOrDefaultAsync();

            var messageOptions = await messageOptionsSubject.FirstAsync();
            messageOptions.ShouldNotBeNull();
            messageOptions.Message.ShouldContain("42");
            messageOptions.Message.ShouldContain("test-string");
            messageOptions.Type.ShouldBe(InformationType.Info);
        }
        
        
    }
}