using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
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
using Xpand.XAF.Modules.Workflow.Tests.Common;

namespace Xpand.XAF.Modules.Workflow.Tests{
    public class TimeIntervalWorkflowCommandTests : BaseWorkflowTest {
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Startup_Interval_With_EmitNow_True_Executes_Immediately_And_Then_On_Interval() {
            await using var application = NewApplication();
            WorkflowModule(application);

            var stopwatch = new Stopwatch();
            var executionTimes = new List<long>();
            const int intervalMilliseconds = 200;

            IObservable<Unit> TestLogic(Frame frame) => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<TimeIntervalWorkflowCommand>();
                    command.Interval = intervalMilliseconds.Milliseconds();
                    command.EmitNow = true;
                    command.Mode = TriggerAction.Startup;
                    command.CommandSuite = suite;
                    space.CommitChanges();
                    return command.Observe();
                })
                .DoOnSubscribe(stopwatch.Start)
                .SelectMany(command => command.WhenExecuted().Do(_ => executionTimes.Add(stopwatch.ElapsedMilliseconds)))
                .Take(2)
                .ToUnit();

            await application.StartWinTest(TestLogic)
                ;

            executionTimes.Count.ShouldBe(2);
            executionTimes[0].ShouldBeLessThan(500, "First execution should be near-immediate.");
            var interval = executionTimes[1] - executionTimes[0];
            interval.ShouldBeInRange((long)intervalMilliseconds - 50, (long)intervalMilliseconds + 500, "The second execution should occur after the specified interval.");
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Startup_Interval_With_EmitNow_False_Waits_For_Interval_Before_First_Emission() {
            await using var application = NewApplication();
            WorkflowModule(application);

            var stopwatch = new Stopwatch();
            var executionTimes = new List<long>();
            const int intervalMilliseconds = 300;

            IObservable<Unit> TestLogic(Frame frame) => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<TimeIntervalWorkflowCommand>();
                    command.Interval = intervalMilliseconds.Milliseconds();
                    command.EmitNow = false;
                    command.Mode = TriggerAction.Startup;
                    command.CommandSuite = suite;
                    space.CommitChanges();
                    return command.Observe();
                })
                .DoOnSubscribe(stopwatch.Start)
                .SelectMany(command => command.WhenExecuted().Do(_ => executionTimes.Add(stopwatch.ElapsedMilliseconds)))
                .Take(1)
                .ToUnit();

            await application.StartWinTest(TestLogic)
                ;

            executionTimes.Count.ShouldBe(1);
            executionTimes[0].ShouldBeInRange((long)intervalMilliseconds - 50, (long)intervalMilliseconds + 500,
                "The first emission should occur only after the interval has elapsed.");
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Persistent_Resumes_Execution_Based_On_Last_Execution_Time() {
            await using var application = NewApplication();
            WorkflowModule(application);

            var stopwatch = new Stopwatch();
            var executionTimes = new List<long>();
            const int intervalMilliseconds = 500;
            const int pastExecutionMilliseconds = 200;

            IObservable<Unit> TestLogic(Frame frame) => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<TimeIntervalWorkflowCommand>();
                    command.Interval = intervalMilliseconds.Milliseconds();
                    command.EmitNow = false;
                    command.Mode = TriggerAction.Persistent;
                    command.CommandSuite = suite;

                    var lastExecution = space.CreateObject<CommandExecution>();
                    lastExecution.WorkflowCommand = command;
                    lastExecution.Created = DateTime.Now.AddMilliseconds(-pastExecutionMilliseconds);
            
                    space.CommitChanges();
                    return command.Observe();
                })
                .DoOnSubscribe(stopwatch.Start)
                .SelectMany(command => command.WhenExecuted().Do(_ => executionTimes.Add(stopwatch.ElapsedMilliseconds)))
                .Take(1)
                .ToUnit();

            await application.StartWinTest(TestLogic)
                ;

            executionTimes.Count.ShouldBe(1);
            long expectedDelay = intervalMilliseconds - pastExecutionMilliseconds;
            executionTimes[0].ShouldBeInRange(expectedDelay - 50, expectedDelay + 500,
                "The first execution should be scheduled based on the last execution's timestamp.");
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Random_Mode_Waits_A_Random_Interval_Before_First_Execution() {
            await using var application = NewApplication();
            WorkflowModule(application);

            var stopwatch = new Stopwatch();
            var executionTimes = new List<long>();
            const int maxIntervalMilliseconds = 400;

            IObservable<Unit> TestLogic(Frame frame) => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<TimeIntervalWorkflowCommand>();
                    command.Interval = maxIntervalMilliseconds.Milliseconds();
                    command.Mode = TriggerAction.Random;
                    command.CommandSuite = suite;
                    space.CommitChanges();
                    return command.Observe();
                })
                .DoOnSubscribe(stopwatch.Start)
                .SelectMany(command => command.WhenExecuted().Do(_ => executionTimes.Add(stopwatch.ElapsedMilliseconds)))
                .Take(2)
                .ToUnit();

            await application.StartWinTest(TestLogic)
                ;

            executionTimes.Count.ShouldBe(2);
            executionTimes[0].ShouldBeInRange((long)maxIntervalMilliseconds - 50, (long)maxIntervalMilliseconds * 2 + 500,
                "The first execution should occur after a random delay plus one full interval.");

            var subsequentInterval = executionTimes[1] - executionTimes[0];
            subsequentInterval.ShouldBeInRange((long)maxIntervalMilliseconds - 50, (long)maxIntervalMilliseconds + 500,
                "The second execution should occur after the regular, non-random interval.");
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task HourlyIntervals_Executes_After_Specified_Fractional_Hour_Duration() {
            await using var application = NewApplication();
            WorkflowModule(application);

            var stopwatch = new Stopwatch();
            var executionTimes = new List<long>();
            const double shortIntervalHours = 0.0001;
            var intervalString = shortIntervalHours.ToString("F8");
            var expectedDelay = (long)TimeSpan.FromHours(shortIntervalHours).TotalMilliseconds;

            IObservable<Unit> TestLogic(Frame frame) => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<TimeIntervalWorkflowCommand>();
                    command.HourlyIntervals = intervalString;
                    command.Interval = 1.Seconds();
                    command.CommandSuite = suite;
                    space.CommitChanges();
                    return command.Observe();
                })
                .DoOnSubscribe(stopwatch.Start)
                .SelectMany(command => command.WhenExecuted().Do(_ => executionTimes.Add(stopwatch.ElapsedMilliseconds)))
                .Take(1)
                .ToUnit();

            await application.StartWinTest(TestLogic)
                ;

            executionTimes.Count.ShouldBe(1);
            executionTimes[0].ShouldBeInRange(expectedDelay - 100, expectedDelay + 500, "The execution should occur after the duration specified by the fractional hour.");
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task HourlyIntervals_With_Decimal_Executes_At_Correct_Minute() {
            await using var application = NewApplication();
            WorkflowModule(application);

            var stopwatch = new Stopwatch();
            var executionTimes = new List<long>();
            const double fractionalHour = 0.0005;
            var intervalString = fractionalHour.ToString("F4");
            var expectedDelay = (long)TimeSpan.FromHours(fractionalHour).TotalMilliseconds;

            IObservable<Unit> TestLogic(Frame frame) => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<TimeIntervalWorkflowCommand>();
                    command.HourlyIntervals = intervalString;
                    command.Interval = 1.Seconds();
                    command.CommandSuite = suite;
                    space.CommitChanges();
                    return command.Observe();
                })
                .DoOnSubscribe(stopwatch.Start)
                .SelectMany(command => command.WhenExecuted().Do(_ => executionTimes.Add(stopwatch.ElapsedMilliseconds)))
                .Take(1)
                .ToUnit();

            await application.StartWinTest(TestLogic)
                ;

            executionTimes.Count.ShouldBe(1);
            executionTimes[0].ShouldBeInRange(expectedDelay - 100, expectedDelay + 500,
                "The execution should be delayed by the duration represented by the fractional hour.");
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task HourlyIntervals_Execution_Uses_Only_The_Last_Value_Due_To_Switch_Operator() {
            await using var application = NewApplication();
            WorkflowModule(application);

            var stopwatch = new Stopwatch();
            var executionTimes = new List<long>();
    
            const double ignoredIntervalHours = 0.0001;
            const double activeIntervalHours = 0.0002;
            var intervalString = $"{ignoredIntervalHours:F8},{activeIntervalHours:F8}";
            var expectedDelay = (long)TimeSpan.FromHours(activeIntervalHours).TotalMilliseconds;

            IObservable<Unit> TestLogic(Frame frame) => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<TimeIntervalWorkflowCommand>();
                    command.HourlyIntervals = intervalString;
                    command.Interval = 1.Seconds();
                    command.CommandSuite = suite;
                    space.CommitChanges();
                    return command.Observe();
                })
                .DoOnSubscribe(stopwatch.Start)
                .SelectMany(command => command.WhenExecuted().Do(_ => executionTimes.Add(stopwatch.ElapsedMilliseconds)))
                .Take(1)
                .ToUnit();

            await application.StartWinTest(TestLogic)
                ;

            executionTimes.Count.ShouldBe(1);
            executionTimes[0].ShouldBeInRange(expectedDelay - 100, expectedDelay + 500,
                "The execution should be scheduled based only on the last value in the comma-separated list.");
        }
        
        [Test]
        public async Task NextEmission_For_Startup_Mode_Calculates_Correctly() {
            await using var application = NewApplication();
            WorkflowModule(application);
            DateTime? nextEmissionWithDelay = null;
            DateTime? nextEmissionImmediate = null;

            await application.UseProviderObjectSpace(space => {
                var command = space.CreateObject<TimeIntervalWorkflowCommand>();
                command.Mode = TriggerAction.Startup;
                command.Interval = TimeSpan.FromMinutes(15);

                command.EmitNow = false;
                nextEmissionWithDelay = command.NextEmission;

                command.EmitNow = true;
                nextEmissionImmediate = command.NextEmission;
        
                return command.Commit();
            });

            nextEmissionWithDelay.ShouldNotBeNull();
            (nextEmissionWithDelay.Value - DateTime.Now.AddMinutes(15)).TotalSeconds.ShouldBeLessThan(1,
                "With EmitNow=false, NextEmission should be the current time plus the interval.");

            nextEmissionImmediate.ShouldNotBeNull();
            (nextEmissionImmediate.Value - DateTime.Now).TotalSeconds.ShouldBeLessThan(1,
                "With EmitNow=true, NextEmission should be the current time.");
        }
        
        [Test]
        public async Task NextEmission_For_Persistent_Mode_With_Prior_Execution_Calculates_Correctly() {
            await using var application = NewApplication();
            WorkflowModule(application);
            DateTime? nextEmission = null;
            var fiveMinutesAgo = DateTime.Now.AddMinutes(-5);
            var tenMinuteInterval = TimeSpan.FromMinutes(10);

            await application.UseProviderObjectSpace(space => {
                var commandSuite = space.CreateObject<CommandSuite>();
                var command = space.CreateObject<TimeIntervalWorkflowCommand>();
                command.CommandSuite = commandSuite;
                command.Mode = TriggerAction.Persistent;
                command.Interval = tenMinuteInterval;

                var execution = space.CreateObject<CommandExecution>();
                execution.WorkflowCommand = command;
                execution.Created = fiveMinutesAgo;
        
                nextEmission = command.NextEmission;
                return command.Commit();
            });

            nextEmission.ShouldNotBeNull();
            var expectedTime = fiveMinutesAgo.Add(tenMinuteInterval);
            (nextEmission.Value - expectedTime).TotalSeconds.ShouldBeLessThan(1,
                "NextEmission should be calculated as the last execution time plus the interval.");
        }
        
        [Test]
        public async Task NextEmission_Is_Null_For_Random_Mode() {
            await using var application = NewApplication();
            WorkflowModule(application);
            DateTime? nextEmission = null;

            await application.UseProviderObjectSpace(space => {
                var command = space.CreateObject<TimeIntervalWorkflowCommand>();
                command.Mode = TriggerAction.Random;
                command.Interval = TimeSpan.FromMinutes(10);
        
                nextEmission = command.NextEmission;
                return command.Commit();
            });

            nextEmission.ShouldBeNull("NextEmission should be null when the mode is Random, as the start time is unpredictable.");
        }
        
        [Test]
        public async Task NextEmission_For_HourlyIntervals_Finds_Next_Time_Today() {
            await using var application = NewApplication();
            WorkflowModule(application);
            DateTime? nextEmission = null;

            var nowForTest = DateTime.Now;
            var futureTime = nowForTest.AddMinutes(5);
            var fractionalHour = futureTime.TimeOfDay.TotalHours;
            var intervalString = $"{fractionalHour:F8}";

            await application.UseProviderObjectSpace(space => {
                var command = space.CreateObject<TimeIntervalWorkflowCommand>();
                command.HourlyIntervals = intervalString;
                nextEmission = command.NextEmission;
                return command.Commit();
            });

            nextEmission.ShouldNotBeNull();
            (nextEmission.Value - futureTime).TotalSeconds.ShouldBeLessThan(1,
                "NextEmission should accurately calculate the time based on the fractional hour provided.");
        }

        [Test]
        public async Task NextEmission_For_HourlyIntervals_Finds_Next_Time_Tomorrow() {
            await using var application = NewApplication();
            WorkflowModule(application);
            DateTime? nextEmission = null;

            var nowForTest = DateTime.Now;
            var pastTime = nowForTest.AddMinutes(-5);
            var fractionalHour = pastTime.TimeOfDay.TotalHours;
            var intervalString = $"{fractionalHour:F8}";
            var expectedTime = pastTime.AddDays(1);

            await application.UseProviderObjectSpace(space => {
                var command = space.CreateObject<TimeIntervalWorkflowCommand>();
                command.HourlyIntervals = intervalString;
                nextEmission = command.NextEmission;
                return command.Commit();
            });

            nextEmission.ShouldNotBeNull();
            (nextEmission.Value - expectedTime).TotalSeconds.ShouldBeLessThan(1,
                "NextEmission should be scheduled for the same time on the next calendar day.");
        }
        
        
    }
}