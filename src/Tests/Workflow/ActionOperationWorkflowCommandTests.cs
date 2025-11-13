using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Workflow.BusinessObjects;
using Xpand.XAF.Modules.Workflow.BusinessObjects.Commands;
using Xpand.XAF.Modules.Workflow.Services;
using Xpand.XAF.Modules.Workflow.Tests.BOModel;
using Xpand.XAF.Modules.Workflow.Tests.Common;

namespace Xpand.XAF.Modules.Workflow.Tests {
    public class ActionOperationWorkflowCommandTests : BaseWorkflowTest {
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Executes_When_Target_Action_Is_Executed() {
            await using var application = NewApplication();
            using var _=application.WhenApplicationModulesManager()
                .SelectMany(manager
                    => manager.RegisterViewSimpleAction(nameof(Executes_When_Target_Action_Is_Executed),action => action.SelectionDependencyType=SelectionDependencyType.Independent))
                .Subscribe();
            var capturedOutput = new ReplaySubject<object[]>();
            var executionCount = 0;
            using var __=application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ActionOperationWorkflowCommand>();
                    command.Action = command.Actions.First(s => s.Name == nameof(Executes_When_Target_Action_Is_Executed));
                    command.CommandSuite = suite;
                    return suite.Commit()
                        .SelectMany(_ => command.WhenExecuted())
                        .Do(objects => {
                            executionCount++;
                            capturedOutput.OnNext(objects);
                            capturedOutput.OnCompleted();
                        })
                        .To(suite);
                }))
                .Subscribe();
            WorkflowModule(application);
            
            await application.StartWinTest(frame => frame.DeferAction(_ => frame.SimpleAction(nameof(Executes_When_Target_Action_Is_Executed)).DoExecute()).IgnoreElements()
                    .ConcatToUnit(capturedOutput))
                .FirstOrDefaultAsync();

            executionCount.ShouldBe(1);
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Filters_By_ViewId_When_Specified() {
            await using var application = NewApplication();
            using var _=application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Filters_By_ViewId_When_Specified)))
                .Subscribe();
            var executionCount = 0;
            const string targetViewId = "WF_ListView";

            using var __=application.WhenSetupComplete()
                .SelectMany(_ => {
                    var setupDatabase = application.UseProviderObjectSpace(space => {
                        var suite = space.CreateObject<CommandSuite>();
                        var command = space.CreateObject<ActionOperationWorkflowCommand>();
                        command.Action = command.Actions.First(s => s.Name==nameof(Filters_By_ViewId_When_Specified));
                        command.View = command.Views.First(s => s.Name==targetViewId);
                        command.CommandSuite = suite;
                        return suite.Commit()
                            .MergeToUnit(command.WhenExecuted().Do(_ => executionCount++))
                            .To(suite);
                    });

                    return setupDatabase;
                }).Subscribe();

            WorkflowModule(application);
            
            IObservable<Unit> ExecutionLogic(Frame frame) {
                frame.SimpleAction(nameof(Filters_By_ViewId_When_Specified)).DoExecute();

                var showTargetView = application.Navigate(typeof(WF)).Take(1);

                var executeInTargetView = application.WhenFrame(targetViewId).Take(1)
                    .Do(targetFrame => targetFrame.SimpleAction(nameof(Filters_By_ViewId_When_Specified)).DoExecute())
                    .ToUnit();

                return executeInTargetView.MergeToUnit(showTargetView.IgnoreElements());
            }

            await application.StartWinTest(ExecutionLogic).FirstOrDefaultAsync();

            executionCount.ShouldBe(1, "The command should only execute when the action is triggered in the view specified by the 'View' property.");
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Outputs_Selected_Objects_When_Emission_Is_SelectedObjects() {
            await using var application = NewApplication();
            using var _=application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Outputs_Selected_Objects_When_Emission_Is_SelectedObjects)))
                .Subscribe();
            WorkflowModule(application);

            var capturedOutput = new ReplaySubject<object[]>();
            

            IObservable<Unit> SetupLogic() {
                var commandExecuted = application.WhenProviderObject<ActionOperationWorkflowCommand>()
                    .SelectMany(cmd => cmd.WhenExecuted().Take(1))
                    .Do(output => {
                        capturedOutput.OnNext(output);
                        capturedOutput.OnCompleted();
                    })
                    .ToUnit();

                var setupDatabase = application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ActionOperationWorkflowCommand>();
                    command.Action = command.Actions.First(s => s.Name==nameof(Outputs_Selected_Objects_When_Emission_Is_SelectedObjects));
                    command.Emission = ActionOperationWorkflowCommandEmission.SelectedObjects;
                    command.CommandSuite = suite;
                    return suite.Commit();
                });

                return setupDatabase.IgnoreElements().ConcatToUnit(commandExecuted);
            }

            IObservable<Unit> ExecutionLogic(Frame frame) {
                frame.SimpleAction(nameof(Outputs_Selected_Objects_When_Emission_Is_SelectedObjects)).DoExecute();
                return Observable.Return(Unit.Default);
            }

            await SetupLogic().IgnoreElements()
                .MergeToUnit(application.StartWinTest(ExecutionLogic)).FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.Length.ShouldBe(1);
            result.First().ShouldBeOfType<CommandSuite>();
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Outputs_View_Objects_When_Emission_Is_ViewObjects() {
            await using var application = NewApplication();
            using var _=application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Outputs_View_Objects_When_Emission_Is_ViewObjects)))
                .Subscribe();
            WorkflowModule(application);

            var capturedOutput = new ReplaySubject<object[]>();

            IObservable<Unit> SetupLogic() {
                var commandExecuted = application.WhenProviderObject<ActionOperationWorkflowCommand>()
                    .SelectMany(cmd => cmd.WhenExecuted().Take(1))
                    .Do(output => {
                        capturedOutput.OnNext(output);
                        capturedOutput.OnCompleted();
                    })
                    .ToUnit();

                var setupDatabase = application.UseProviderObjectSpace(space => {
                    space.CreateObject<WF>();
                    space.CreateObject<WF>();
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ActionOperationWorkflowCommand>();
                    command.Action = command.Actions.First(s => s.Name==nameof(Outputs_View_Objects_When_Emission_Is_ViewObjects));
                    command.Emission = ActionOperationWorkflowCommandEmission.ViewObjects;
                    command.CommandSuite = suite;
                    return suite.Commit();
                });

                return setupDatabase.IgnoreElements().ConcatToUnit(commandExecuted);
            }

            IObservable<Unit> ExecutionLogic(Frame frame) => frame.Application.Navigate(typeof(WF)).Take(1)
                .Do(_ => {
                    frame.View.ToListView().CollectionSource.Objects().Count().ShouldBe(2);
                    frame.SimpleAction(nameof(Outputs_View_Objects_When_Emission_Is_ViewObjects)).DoExecute();
                })
                .ToUnit();

            await SetupLogic().IgnoreElements()
                .MergeToUnit(application.StartWinTest(ExecutionLogic)).FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.Length.ShouldBe(2);
            result.All(o => o is WF).ShouldBeTrue();
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Outputs_Action_When_Emission_Is_Action() {
            await using var application = NewApplication();
            var actionId = nameof(Outputs_Action_When_Emission_Is_Action);
            using var _=application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(actionId))
                .Subscribe();
            WorkflowModule(application);

            var capturedOutput = new ReplaySubject<object[]>();

            IObservable<Unit> SetupLogic() {
                var commandExecuted = application.WhenProviderObject<ActionOperationWorkflowCommand>()
                    .SelectMany(cmd => cmd.WhenExecuted().Take(1))
                    .Do(output => {
                        capturedOutput.OnNext(output);
                        capturedOutput.OnCompleted();
                    })
                    .ToUnit();

                var setupDatabase = application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ActionOperationWorkflowCommand>();
                    command.Action = command.Actions.First(s => s.Name==actionId);
                    command.Emission = ActionOperationWorkflowCommandEmission.Action;
                    command.CommandSuite = suite;
                    return suite.Commit();
                });

                return setupDatabase.IgnoreElements().ConcatToUnit(commandExecuted);
            }

            IObservable<Unit> ExecutionLogic(Frame frame) {
                frame.SimpleAction(actionId).DoExecute();
                return Observable.Return(Unit.Default);
            }

            await SetupLogic().IgnoreElements()
                .MergeToUnit(application.StartWinTest(ExecutionLogic)).FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.Length.ShouldBe(1);
            var action = result.Single().ShouldBeOfType<SimpleAction>();
            action.Id.ShouldBe(actionId);
        }
        
         [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Projects_SelectedObjects_With_OutputProperty() {
            await using var application = NewApplication();
            using var _=application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Projects_SelectedObjects_With_OutputProperty)))
                .Subscribe();
            WorkflowModule(application);

            var capturedOutput = new ReplaySubject<object[]>();
            long oid = 0;

            IObservable<Unit> SetupLogic() {
                var commandExecuted = application.WhenProviderObject<ActionOperationWorkflowCommand>()
                    .SelectMany(cmd => cmd.WhenExecuted().Take(1))
                    .Do(output => {
                        capturedOutput.OnNext(output);
                        capturedOutput.OnCompleted();
                    })
                    .ToUnit();

                var setupDatabase = application.UseProviderObjectSpace(space => {
                    var wf = space.CreateObject<WF>();
                    oid = wf.Oid;
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ActionOperationWorkflowCommand>();
                    command.Action = command.Actions.First(s => s.Name==nameof(Projects_SelectedObjects_With_OutputProperty));
                    command.Emission = ActionOperationWorkflowCommandEmission.ViewObjects;
                    command.OutputProperty = nameof(WF.Oid);
                    command.CommandSuite = suite;
                    return suite.Commit();
                });

                return setupDatabase.IgnoreElements().ConcatToUnit(commandExecuted);
            }

            IObservable<Unit> ExecutionLogic(Frame frame) 
                => frame.Application.Navigate(typeof(WF)).Take(1)
                    .Do(frame1 => frame1.SimpleAction(nameof(Projects_SelectedObjects_With_OutputProperty)).DoExecute())
                    .ToUnit();

            await SetupLogic().IgnoreElements()
                .MergeToUnit(application.StartWinTest(ExecutionLogic)).FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.Length.ShouldBe(1);
            result.Single().ShouldBe(oid);
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Handles_Invalid_OutputProperty_Gracefully() {
            await using var application = NewApplication();
            using var _=application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Handles_Invalid_OutputProperty_Gracefully)))
                .Subscribe();
            WorkflowModule(application);

            var capturedOutput = new ReplaySubject<object[]>();

            IObservable<Unit> SetupLogic() {
                var commandExecuted = application.WhenProviderObject<ActionOperationWorkflowCommand>()
                    .SelectMany(cmd => cmd.WhenExecuted().Take(1))
                    .Do(output => {
                        capturedOutput.OnNext(output);
                        capturedOutput.OnCompleted();
                    })
                    .ToUnit();

                var setupDatabase = application.UseProviderObjectSpace(space => {
                    space.CreateObject<WF>();
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ActionOperationWorkflowCommand>();
                    command.Action = command.Actions.First(s => s.Name==nameof(Handles_Invalid_OutputProperty_Gracefully));
                    command.Emission = ActionOperationWorkflowCommandEmission.ViewObjects;
                    command.OutputProperty = "InvalidPropertyName";
                    command.CommandSuite = suite;
                    return suite.Commit();
                });

                return setupDatabase.IgnoreElements().ConcatToUnit(commandExecuted);
            }

            IObservable<Unit> ExecutionLogic(Frame frame) 
                => frame.Application.Navigate(typeof(WF)).Take(1)
                    .Do(frame1 => frame1.SimpleAction(nameof(Handles_Invalid_OutputProperty_Gracefully)).DoExecute())
                    .ToUnit();

            await SetupLogic().IgnoreElements()
                .MergeToUnit(application.StartWinTest(ExecutionLogic)).FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.ShouldBeEmpty();
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Output_Is_Distinct() {
            await using var application = NewApplication();
            using var _=application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Output_Is_Distinct)))
                .Subscribe();
            WorkflowModule(application);

            var capturedOutput = new ReplaySubject<object[]>();

            IObservable<Unit> SetupLogic() {
                var commandExecuted = application.WhenProviderObject<ActionOperationWorkflowCommand>()
                    .SelectMany(cmd => cmd.WhenExecuted().Take(1))
                    .Do(output => {
                        capturedOutput.OnNext(output);
                        capturedOutput.OnCompleted();
                    })
                    .ToUnit();

                var setupDatabase = application.UseProviderObjectSpace(space => {
                    space.CreateObject<WF>().Status = "Active";
                    space.CreateObject<WF>().Status = "Active";
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ActionOperationWorkflowCommand>();
                    command.Action=command.Actions.First(s => s.Name==nameof(Output_Is_Distinct));
                    command.Emission = ActionOperationWorkflowCommandEmission.ViewObjects;
                    command.OutputProperty = nameof(WF.Status);
                    command.CommandSuite = suite;
                    return suite.Commit();
                });

                return setupDatabase.IgnoreElements().ConcatToUnit(commandExecuted);
            }

            IObservable<Unit> ExecutionLogic(Frame frame) 
                => frame.Application.Navigate(typeof(WF)).Take(1).IgnoreElements().DoNotComplete()
                    .Do(frame1 => frame1.SimpleAction(nameof(Output_Is_Distinct)).DoExecute())
                    .ToUnit();

            await SetupLogic().IgnoreElements()
                .MergeToUnit(application.StartWinTest(ExecutionLogic,100.ToSeconds())).DoNotComplete().FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.Length.ShouldBe(1);
            result.Single().ShouldBe("Active");
        }
    }
 }
