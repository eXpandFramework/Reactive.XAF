using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Persistent.Validation;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Workflow.BusinessObjects;
using Xpand.XAF.Modules.Workflow.BusinessObjects.Commands;
using Xpand.XAF.Modules.Workflow.Services;
using Xpand.XAF.Modules.Workflow.Tests.BOModel;
using Xpand.XAF.Modules.Workflow.Tests.Common;

namespace Xpand.XAF.Modules.Workflow.Tests {
    
    public class WorkflowServiceTests : BaseWorkflowTest {
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_Root_Command_Execution() {
            await using var application = NewApplication();
            var replaySubject = new ReplaySubject<Unit>();
            await application.WhenSetupComplete(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();
                    commandSuite.Commands.Add(space.CreateObject<TestCommand>());
                    return commandSuite.Commit().Cast<CommandSuite>()
                        .SelectMany(suite => suite.Commands)
                        .SelectMany(command => command.WhenExecuted()
                            .Do(_ => {
                                replaySubject.OnNext(Unit.Default);
                                replaySubject.OnCompleted();
                            })
                            .To(command));
                })).Take(1).IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => replaySubject.Take(1)))
                .Timeout(30.ToSeconds());
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_Dependency_Chain_Execution() {
            await using var application = NewApplication();
            var executionOrder = new ReplaySubject<string>();

            var setupAndListen = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();
                    var commandA = space.CreateObject<TestCommand>();
                    commandA.Id = "CommandA";
                    commandA.CommandSuite = commandSuite;
                    var commandB = space.CreateObject<TestCommand>();
                    commandB.Id = "CommandB";
                    commandB.CommandSuite = commandSuite;
                    commandB.StartAction = commandA;

                    return commandSuite.Commit()
                        .SelectMany(_ => commandA.WhenExecuted().Do(_ => executionOrder.OnNext(commandA.Id)).ToUnit()
                            .Merge(commandB.WhenExecuted().Do(_ => {
                                executionOrder.OnNext(commandB.Id);
                                executionOrder.OnCompleted();
                            }).ToUnit()))
                        .To(commandSuite);
                }))
                .Take(2);

            await setupAndListen
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => executionOrder.Buffer(2).Select(_ => Unit.Default)))
                .Timeout(30.ToSeconds());

            var results = await executionOrder.Buffer(2).FirstAsync();
            results.Count.ShouldBe(2);
            results[0].ShouldBe("CommandA");
            results[1].ShouldBe("CommandB");
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_StartCommands_Fan_In_Execution() {
            await using var application = NewApplication();
            var executionCount = 0;
            var completionSignal = new ReplaySubject<Unit>();

            var setupAndListen = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();

                    var commandA = space.CreateObject<TestCommand>();
                    commandA.Id = "CommandA";
                    commandA.CommandSuite = commandSuite;

                    var commandB = space.CreateObject<TestCommand>();
                    commandB.Id = "CommandB";
                    commandB.CommandSuite = commandSuite;

                    var commandC = space.CreateObject<TestCommand>();
                    commandC.Id = "CommandC";
                    commandC.CommandSuite = commandSuite;
                    commandC.StartCommands.Add(commandA);
                    commandC.StartCommands.Add(commandB);

                    return commandSuite.Commit()
                        .SelectMany(_ => commandC.WhenExecuted())
                        .Do(_ => {
                            executionCount++;
                            if (executionCount == 2) {
                                completionSignal.OnNext(Unit.Default);
                                completionSignal.OnCompleted();
                            }
                        })
                        .To(commandSuite);
                }))
                .ToUnit();

            await setupAndListen.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => completionSignal))
                .Timeout(TimeSpan.FromSeconds(30))
                .Take(1);

            executionCount.ShouldBe(2);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_ExecuteOnce_Behavior() {
            await using var application = NewApplication();

            var executionCount = 0;
            Guid targetCommandOid = Guid.Empty;

            var setupAndListen = application.WhenSetupComplete().Take(1)
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();

                    var ticker = space.CreateObject<TimeIntervalWorkflowCommand>();
                    ticker.Interval = TimeSpan.FromMilliseconds(100);
                    ticker.CommandSuite = commandSuite;

                    var target = space.CreateObject<TestCommand>();
                    target.ExecuteOnce = true;
                    target.CommandSuite = commandSuite;
                    target.StartAction = ticker;

                    targetCommandOid = target.Oid;

                    return commandSuite.Commit()
                        .SelectMany(_ => target.WhenExecuted()
                            .TakeUntil(ticker.WhenExecuted().Skip(4).Take(1)))
                        .Do(_ => executionCount++)
                        .To(commandSuite);
                }))
                .ToUnit()
                .Finally(() => { });

            var observationWindow = 1.ToSeconds().Timer().ToUnit();

            await setupAndListen.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)).IgnoreElements())
                .MergeToUnit(application.StartWinTest(_ => observationWindow))
                .Take(1)
                .Timeout(TimeSpan.FromSeconds(30));

            executionCount.ShouldBe(1);

            var testCommand = await application.UseProviderObjectSpace(space
                => space.GetObjectByKey<TestCommand>(targetCommandOid).Observe());
            testCommand.ShouldNotBeNull();
            testCommand.Active.ShouldBeFalse();
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_LogExecutions_Behavior() {
            await using var application = NewApplication();
            Guid withLoggingOid = Guid.Empty;
            Guid withoutLoggingOid = Guid.Empty;
            var completionSignal = new ReplaySubject<Unit>();

            var setupAndListen = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();

                    var commandWithLogging = space.CreateObject<TestCommand>();
                    commandWithLogging.Id = "WithLogging";
                    commandWithLogging.LogExecutions = true;
                    commandWithLogging.CommandSuite = commandSuite;
                    withLoggingOid = commandWithLogging.Oid;

                    var commandWithoutLogging = space.CreateObject<TestCommand>();
                    commandWithoutLogging.Id = "WithoutLogging";
                    commandWithoutLogging.CommandSuite = commandSuite;
                    withoutLoggingOid = commandWithoutLogging.Oid;

                    return commandSuite.Commit()
                        .SelectMany(_ => commandWithLogging.WhenExecuted()
                            .Zip(commandWithoutLogging.WhenExecuted(), (_, _) => Unit.Default))
                        .Do(_ => {
                            completionSignal.OnNext(Unit.Default);
                            completionSignal.OnCompleted();
                        })
                        .To(commandSuite);
                }))
                .ToUnit();

            await setupAndListen.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => completionSignal))
                .Timeout(TimeSpan.FromSeconds(30))
                .Take(1);

            await application.UseProviderObjectSpace(space => {
                var cmdWith = space.GetObjectByKey<TestCommand>(withLoggingOid);
                cmdWith.Executions.Count.ShouldBe(1);

                var cmdWithout = space.GetObjectByKey<TestCommand>(withoutLoggingOid);
                cmdWithout.Executions.Count.ShouldBe(0);
                return cmdWithout.Observe();
            });
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_NeedSubscription_Validation() {
            await using var application = NewApplication();

            var setup = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<TestCommand>();
                    command.Subscription = true;
                    command.CommandSuite = commandSuite;
                    return commandSuite.Commit();
                }))
                .ToUnit();

            await setup.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => Observable.Empty<Unit>()))
                .Timeout(TimeSpan.FromSeconds(30)).FirstOrDefaultAsync();

            BusEvents.ShouldHaveSingleItem();
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var validationException = fault.InnerException.ShouldBeOfType<ValidationException>();
            validationException.Result.Results
                .Any(item => item.ErrorMessage.Contains(nameof(WorkflowCommand.StartAction).CompoundName()))
                .ShouldBeTrue();

        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_Inactive_Root_Command_Is_Ignored() {
            await using var application = NewApplication();
            var executionCount = 0;
            var observationWindow = TimeSpan.FromMilliseconds(250).Timer().ToUnit().Publish().AutoConnect();

            var setupAndListen = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<TestCommand>();
                    command.Active = false;
                    command.CommandSuite = commandSuite;

                    return commandSuite.Commit()
                        .SelectMany(_ => command.WhenExecuted().TakeUntil(observationWindow))
                        .Do(_ => executionCount++)
                        .To(commandSuite);
                }))
                .ToUnit();

            await setupAndListen.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => observationWindow))
                .Timeout(TimeSpan.FromSeconds(30)).FirstOrDefaultAsync();

            executionCount.ShouldBe(0);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_Inactive_Trigger_Breaks_Dependency_Chain() {
            await using var application = NewApplication();
            var executionCountA = 0;
            var executionCountB = 0;
            var observationWindow = TimeSpan.FromMilliseconds(250).Timer().ToUnit().Publish().AutoConnect();

            var setupAndListen = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();
                    var commandA = space.CreateObject<TestCommand>();
                    commandA.Active = false;
                    commandA.Id = "A";
                    commandA.CommandSuite = commandSuite;

                    var commandB = space.CreateObject<TestCommand>();
                    commandB.StartAction = commandA;
                    commandB.CommandSuite = commandSuite;
                    commandB.Id = "B";
                    return commandSuite.Commit()
                        .SelectMany(_ =>
                            commandA.WhenExecuted().TakeUntil(observationWindow).Do(_ => executionCountA++).ToUnit()
                                .Merge(commandB.WhenExecuted().TakeUntil(observationWindow).Do(_ => executionCountB++)
                                    .ToUnit())
                        )
                        .To(commandSuite);
                }))
                .ToUnit();

            await setupAndListen.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => observationWindow))
                .Timeout(TimeSpan.FromSeconds(30)).FirstOrDefaultAsync();

            executionCountA.ShouldBe(0);
            executionCountB.ShouldBe(1);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_Inactive_Dependent_Command_Is_Ignored() {
            await using var application = NewApplication();
            var executionCountA = 0;
            var executionCountB = 0;


            var setupAndListen = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();
                    var commandA = space.CreateObject<TestCommand>();
                    commandA.CommandSuite = commandSuite;

                    var commandB = space.CreateObject<TestCommand>();
                    commandB.Active = false;
                    commandB.StartAction = commandA;
                    commandB.CommandSuite = commandSuite;

                    return commandSuite.Commit()
                        .SelectMany(_ =>
                            commandA.WhenExecuted().Take(1).Do(_ => executionCountA++).ToUnit()
                                .Merge(commandB.WhenExecuted().Take(1).Do(_ => executionCountB++).ToUnit())
                        )
                        .To(commandSuite);
                }))
                .ToUnit();

            await setupAndListen.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => 250.Milliseconds().Timer()))
                .Timeout(TimeSpan.FromSeconds(30)).FirstOrDefaultAsync();

            executionCountA.ShouldBe(1);
            executionCountB.ShouldBe(0);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_Inactive_Middle_Command_Breaks_Chain() {
            await using var application = NewApplication();
            var executionCountA = 0;
            var executionCountB = 0;
            var executionCountC = 0;

            var setupAndListen = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();

                    var commandA = space.CreateObject<TestCommand>();
                    commandA.Id = "CommandA";
                    commandA.CommandSuite = commandSuite;

                    var commandB = space.CreateObject<TestCommand>();
                    commandB.Id = "CommandB";
                    commandB.Active = false;
                    commandB.StartAction = commandA;
                    commandB.CommandSuite = commandSuite;

                    var commandC = space.CreateObject<TestCommand>();
                    commandC.Id = "CommandC";
                    commandC.StartAction = commandB;
                    commandC.CommandSuite = commandSuite;

                    return commandSuite.Commit()
                        .SelectMany(_ =>
                            commandA.WhenExecuted().Do(_ => executionCountA++).ToUnit()
                                .Merge(commandB.WhenExecuted().Do(_ => executionCountB++).ToUnit())
                                .Merge(commandC.WhenExecuted().Do(_ => executionCountC++).ToUnit())
                        )
                        .To(commandSuite);
                }))
                .ToUnit();

            await setupAndListen.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => 250.Milliseconds().Timer()))
                .Timeout(TimeSpan.FromSeconds(30)).Take(1);

            executionCountA.ShouldBe(1);
            executionCountB.ShouldBe(0);
            executionCountC.ShouldBe(1);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_Inactive_Suite_Is_Ignored() {
            await using var application = NewApplication();
            var executionCount = 0;

            var setupAndListen = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();
                    commandSuite.Active = false;

                    var command = space.CreateObject<TestCommand>();
                    command.Id = "TestCommand";
                    command.CommandSuite = commandSuite;

                    return commandSuite.Commit()
                        .SelectMany(_ => command.WhenExecuted())
                        .Do(_ => executionCount++)
                        .To(commandSuite);
                }))
                .ToUnit();

            await setupAndListen.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => 250.Milliseconds().Timer()))
                .Timeout(TimeSpan.FromSeconds(30)).FirstOrDefaultAsync();

            executionCount.ShouldBe(0);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_Suite_Activated_Post_Startup_Executes_Immediately() {
            await using var application = NewApplication();
            var executionCount = 0;
            Guid suiteOid = Guid.Empty;
            var completionSignal = new ReplaySubject<Unit>();

            var setupAndListen = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();
                    commandSuite.Active = false;
                    commandSuite.Name = "suite";
                    suiteOid = commandSuite.Oid;

                    var command = space.CreateObject<TestCommand>();
                    command.Id = "TestCommand";
                    command.CommandSuite = commandSuite;
                    return commandSuite.Commit()
                        .SelectMany(_ => command.WhenExecuted())
                        .Do(_ => {
                            executionCount++;
                            completionSignal.OnNext(Unit.Default);
                            completionSignal.OnCompleted();
                        })
                        .To(commandSuite);
                }))
                .ToUnit();

            var triggerAndObserve = Observable.Timer(TimeSpan.FromMilliseconds(100))
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var suite = space.GetObjectByKey<CommandSuite>(suiteOid);
                    suite.Active = true;
                    return suite.Commit();
                }))
                .ToUnit()
                .Merge(completionSignal);

            await setupAndListen.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => triggerAndObserve))
                .Timeout(TimeSpan.FromSeconds(30000)).FirstOrDefaultAsync();

            executionCount.ShouldBe(1);
        }


        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_DisableOnError_Behavior() {
            await using var application = NewApplication();
            Guid commandOid = Guid.Empty;

            var setup = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<TestCommand>();
                    command.Id = "FailingCommand";
                    command.DisableOnError = true;
                    command.ShouldFail = true;
                    command.CommandSuite = commandSuite;
                    commandOid = command.Oid;

                    return commandSuite.Commit();
                }));

            await setup.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => BusEvents.ToNowObservable()))
                .Timeout(TimeSpan.FromSeconds(30)).Take(1);

            BusEvents.ShouldHaveSingleItem();
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Execution Failure");

            await application.UseProviderObjectSpace(space => {
                var command = space.GetObjectByKey<TestCommand>(commandOid);
                command.ShouldNotBeNull();
                command.Active.ShouldBeFalse();
                return command.Observe();
            });
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_Failure_With_DisableOnError_False_Keeps_Command_Active() {
            await using var application = NewApplication();
            Guid commandOid = Guid.Empty;

            var setup = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<TestCommand>();
                    command.Id = "FailingCommand";
                    command.DisableOnError = false;
                    command.ShouldFail = true;
                    command.CommandSuite = commandSuite;
                    commandOid = command.Oid;

                    return commandSuite.Commit();
                }));

            await setup.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => BusEvents.ToNowObservable()))
                .Timeout(TimeSpan.FromSeconds(30)).Take(1);

            BusEvents.ShouldHaveSingleItem();
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Execution Failure");

            await application.UseProviderObjectSpace(space => {
                var command = space.GetObjectByKey<TestCommand>(commandOid);
                command.ShouldNotBeNull();
                command.Active.ShouldBeTrue();
                return command.Observe();
            });
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_Suite_Level_Resilience() {
            await using var application = NewApplication();
            var executionCountA = 0;
            var executionCountB = 0;

            var setup = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var suiteA = space.CreateObject<CommandSuite>();
                    suiteA.Name = "SuiteA";
                    var commandA = space.CreateObject<TestCommand>();
                    commandA.Id = "CommandA";
                    commandA.CommandSuite = suiteA;
                    commandA.Subscription = true;

                    var suiteB = space.CreateObject<CommandSuite>();
                    suiteB.Name = "SuiteB";
                    var commandB1 = space.CreateObject<TestCommand>();
                    commandB1.Id = "CommandB1";
                    commandB1.CommandSuite = suiteB;

                    return suiteA.Commit()
                        .SelectMany(_ => commandA.WhenExecuted().Do(_ => executionCountA++)
                            .Merge(commandB1.WhenExecuted().Do(_ => executionCountB++)))
                        .To(suiteA);
                }))
                .ToUnit();

            await setup.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => BusEvents.ToNowObservable()))
                .Timeout(TimeSpan.FromSeconds(30)).Take(1);

            executionCountA.ShouldBe(0);
            executionCountB.ShouldBe(1);
            BusEvents.ShouldHaveSingleItem();
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_Circular_Dependency_Is_Caught_And_Published() {
            await using var application = NewApplication();

            var setup = application.WhenSetupComplete().Delay(100.Milliseconds())
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();
                    commandSuite.Name = "SuiteWithCycle";

                    var command1 = space.CreateObject<TestCommand>();
                    command1.Id = "Command1";
                    command1.CommandSuite = commandSuite;

                    var command2 = space.CreateObject<TestCommand>();
                    command2.Id = "Command2";
                    command2.CommandSuite = commandSuite;

                    command1.StartAction = command2;
                    command2.StartAction = command1;

                    var triggerCommand = space.CreateObject<TestCommand>();
                    triggerCommand.Id = "Trigger";
                    triggerCommand.StartAction = command1;
                    triggerCommand.CommandSuite = commandSuite;
                    return commandSuite.Commit();
                }));

            var listenForFault = FaultHub.Bus.Take(1);

            await setup.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => listenForFault.Merge(BusEvents.ToNowObservable()).Take(1)))
                .Timeout(TimeSpan.FromSeconds(30)).Take(1);

            BusEvents.ShouldHaveSingleItem();
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.FindRootCauses().Any(e => e is ValidationException).ShouldBeTrue();
        }


        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_Data_Flow_Between_Commands() {
            await using var application = NewApplication();
            var completionSignal = new ReplaySubject<object[]>();

            var setupAndListen = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();

                    var commandA = space.CreateObject<TestCommand>();
                    commandA.Id = "CommandA";
                    commandA.CommandSuite = commandSuite;

                    var commandB = space.CreateObject<TestCommand>();
                    commandB.Id = "CommandB";
                    commandB.StartAction = commandA;
                    commandB.CommandSuite = commandSuite;

                    return commandSuite.Commit()
                        .SelectMany(_ => commandB.WhenExecuted().Take(1))
                        .Do(objects => {
                            completionSignal.OnNext(objects);
                            completionSignal.OnCompleted();
                        })
                        .To(commandSuite);
                }))
                .ToUnit();

            await setupAndListen.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => completionSignal))
                .Timeout(TimeSpan.FromSeconds(30)).FirstOrDefaultAsync();

            var signal = await completionSignal;
            signal.ShouldHaveSingleItem().ShouldBe("42;test-string");
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Test_Dynamic_Deactivation_Of_Running_Suite() {
            await using var application = NewApplication();
            var executionCount = 0;
            Guid suiteOid = Guid.Empty;

            var setupAndListen = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();
                    commandSuite.Active = false;
                    suiteOid = commandSuite.Oid;

                    var ticker = space.CreateObject<TimeIntervalWorkflowCommand>();
                    ticker.Interval = TimeSpan.FromMilliseconds(100);
                    ticker.CommandSuite = commandSuite;
                    ticker.LogExecutions = true;
                    

                    return commandSuite.Commit()
                        .SelectMany(_ => ticker.WhenExecuted())
                        .Do(_ => executionCount++)
                        .To(commandSuite);
                }))
                .ToUnit();

            var orchestrator = Observable.Defer(() => application.UseProviderObjectSpace(space => {
                    var suite = space.GetObjectByKey<CommandSuite>(suiteOid);
                    suite.Active = true;
                    return suite.Commit()
                        .SelectMany(commandSuite => suite.Commands.ToNowObservable()
                            .SelectMany(command => command.WhenExecuted())
                            .SelectMany(_ => {
                                commandSuite.Active = false;
                                return commandSuite.Commit();
                            }));
                }) )
                .SelectMany(_ => Observable.Timer(TimeSpan.FromMilliseconds(350)))
                .ToUnit();

            await setupAndListen.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => orchestrator.Take(1)))
                .Timeout(TimeSpan.FromSeconds(30)).FirstOrDefaultAsync();

            executionCount.ShouldBe(1);
        }
        
        [Test][Apartment(ApartmentState.STA)]
        public async Task Test_Resilience_Of_Auditing() {
            await using var application = NewApplication();
            var executionCountA = 0;
            var executionCountB = 0;

            application.WhenSetupComplete()
                .SelectMany(_ => application.WhenProviderCommitting<CommandExecution>()
                    .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Simulated Commit Failure"))))
                .Subscribe();
            

            var setupAndListen = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();
                    
                    var commandA = space.CreateObject<TestCommand>();
                    commandA.Id = "CommandA";
                    commandA.LogExecutions = true;
                    commandA.CommandSuite = commandSuite;

                    var commandB = space.CreateObject<TestCommand>();
                    commandB.Id = "CommandB";
                    commandB.StartAction = commandA;
                    commandB.CommandSuite = commandSuite;

                    return commandSuite.Commit()
                        .SelectMany(_ => 
                            commandA.WhenExecuted().Do(_ => executionCountA++).ToUnit()
                            .Merge(commandB.WhenExecuted().Do(_ => executionCountB++).ToUnit())
                        )
                        .To(commandSuite);
                }))
                .ToUnit();

            await setupAndListen.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => FaultHub.Bus.Merge(BusEvents.ToNowObservable()).Take(1)))
                .Timeout(TimeSpan.FromSeconds(30)).FirstOrDefaultAsync();

            executionCountA.ShouldBe(1);
            executionCountB.ShouldBe(1);
            BusEvents.ShouldHaveSingleItem();
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Simulated Commit Failure");
            application.Count<CommandExecution>().ShouldBe(0);
        }
        [Test][Apartment(ApartmentState.STA)]
        public async Task Test_CleanupExecutions_Logic() {
            await using var application = NewApplication();
            Guid commandOid = Guid.Empty;

            var setup = application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var commandSuite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<TestCommand>();
                    command.Id = "AuditedCommand";
                    command.LogExecutions = true;
                    command.CommandSuite = commandSuite;
                    commandOid = command.Oid;

                    for (int i = 0; i < 11; i++) {
                        var execution = space.CreateObject<CommandExecution>();
                        execution.WorkflowCommand = command;
                        execution.Created = DateTime.Now.Subtract(TimeSpan.FromMinutes(i + 1));
                    }
                    return commandSuite.Commit();
                }));

            var listenForCleanup = application.WhenProviderCommitted<CommandExecution>(ObjectModification.Deleted).Take(1);

            await setup.IgnoreElements()
                .MergeToUnit(application.DeferAction(_ => WorkflowModule(application)))
                .MergeToUnit(application.StartWinTest(_ => Observable.Empty<Unit>()))
                .MergeToUnit(listenForCleanup)
                .Timeout(TimeSpan.FromSeconds(30)).FirstOrDefaultAsync();

            await application.UseProviderObjectSpace(space => {
                var cmd = space.GetObjectByKey<TestCommand>(commandOid);
                cmd.Executions.Count.ShouldBe(10);
                return cmd.Observe();
            });
        }
        
    }
}