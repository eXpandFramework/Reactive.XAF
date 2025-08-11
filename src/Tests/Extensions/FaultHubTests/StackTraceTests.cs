using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;

namespace Xpand.Extensions.Tests.FaultHubTests {
    [TestFixture]
    public class StackTraceTests : FaultHubTestBase {
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Level1_Helper(IObservable<Unit> source) 
            => Level2_Helper(source).PushStackFrame();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Level2_Helper(IObservable<Unit> source)
            => source.PushStackFrame();
        
        [Test]
        public void Chained_Helpers_Build_Correct_Logical_Stack() {
            var source = Observable.Throw<Unit>(new InvalidOperationException("Test Failure"));
            
            var testStream = Level1_Helper(source)
                .ContinueOnFault(context: ["Chained_Helpers_Test"]);

            using var testObserver = testStream.Test();
            
            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            
            var logicalStack = fault.GetLogicalStackTrace().ToList();
            
            logicalStack.ShouldNotBeNull();
            logicalStack.Count.ShouldBe(3);
            
            logicalStack[0].MemberName.ShouldBe(nameof(Level2_Helper));
            logicalStack[1].MemberName.ShouldBe(nameof(Level1_Helper));
            logicalStack[2].MemberName.ShouldBe(nameof(Chained_Helpers_Build_Correct_Logical_Stack));
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Level3_Operation_That_Fails()
            => Observable.Throw<Unit>(new InvalidOperationException("Failure at Level 3"));

        // --- Test for SelectManyItemResilient ---

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Level1_Outer_Operation()
            => Level2_Intermediate_ItemResilient_Operation();
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Level2_Intermediate_ItemResilient_Operation()
            => Observable.Return(Unit.Default)
                .SelectManyItemResilient(_ => Level3_Operation_That_Fails(), context: ["Level2"]);

        [Test]
        public void Nested_ItemResilience_Captures_Trace_From_Handling_Site() {
            var testStream = Level1_Outer_Operation();
            using var testObserver = testStream.Test();
            
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            
            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.GetLogicalStackTrace().ToList();
            
            logicalStack.ShouldNotBeEmpty();
            logicalStack.ShouldContain(frame => frame.MemberName == nameof(Level2_Intermediate_ItemResilient_Operation));
            logicalStack.ShouldNotContain(frame => frame.MemberName == nameof(Level1_Outer_Operation));
        }

        // --- Test for SelectManySequentialItemResilient ---

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Seq_Level1_Outer_Operation()
            => Seq_Level2_Intermediate_ItemResilient_Operation();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Seq_Level2_Intermediate_ItemResilient_Operation()
            => Observable.Return(Unit.Default)
                .SelectManySequentialItemResilient(_ => Level3_Operation_That_Fails(), context: ["Level2"]);

        [Test]
        public void Nested_Sequential_ItemResilience_Captures_Trace_From_Handling_Site() {
            var testStream = Seq_Level1_Outer_Operation();
            using var testObserver = testStream.Test();
            
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            
            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.GetLogicalStackTrace().ToList();

            logicalStack.ShouldNotBeEmpty();
            logicalStack.ShouldContain(frame => frame.MemberName == nameof(Seq_Level2_Intermediate_ItemResilient_Operation));
            logicalStack.ShouldNotContain(frame => frame.MemberName == nameof(Seq_Level1_Outer_Operation));
        }

        // --- New Tests for Remaining ItemResilient Operators ---

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<int> ResilientSelect_Operation()
            => Observable.Range(1, 3).SelectItemResilient(i => {
                if (i == 2) throw new InvalidOperationException("Select Failure");
                return i;
            });
        
        [Test]
        public void SelectItemResilient_Captures_Trace_From_Handling_Site() {
            var testStream = ResilientSelect_Operation();
            using var testObserver = testStream.Test();

            testObserver.Items.ShouldBe(new[] { 1, 3 });
            testObserver.CompletionCount.ShouldBe(1);

            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.GetLogicalStackTrace().ToList();
            
            logicalStack.ShouldNotBeEmpty();
            logicalStack.ShouldContain(frame => frame.MemberName == nameof(ResilientSelect_Operation));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
        private IObservable<int> ResilientDo_Operation()
            => Observable.Range(1, 3).DoItemResilient(i => {
                if (i == 2) throw new InvalidOperationException("Do Failure");
            });

        [Test]
        public void DoItemResilient_Captures_Trace_From_Handling_Site() {
            var testStream = ResilientDo_Operation();
            using var testObserver = testStream.Test();

            testObserver.Items.ShouldBe(new[] { 1, 2, 3 });
            testObserver.CompletionCount.ShouldBe(1);

            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.GetLogicalStackTrace().ToList();
            
            logicalStack.ShouldNotBeEmpty();
            logicalStack.ShouldContain(frame => frame.MemberName == nameof(ResilientDo_Operation));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ResilientDefer_Operation()
            => this.DeferItemResilient(() => Observable.Throw<Unit>(new InvalidOperationException("Defer Failure")));

        [Test]
        public void DeferItemResilient_Captures_Trace_From_Handling_Site() {
            var testStream = ResilientDefer_Operation();
            using var testObserver = testStream.Test();

            testObserver.CompletionCount.ShouldBe(1);

            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.GetLogicalStackTrace().ToList();

            logicalStack.ShouldNotBeEmpty();
            logicalStack.ShouldContain(frame => frame.MemberName == nameof(ResilientDefer_Operation));
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ResilientUsing_Stream_Fails_Operation()
            => this.UsingItemResilient(
                () => new TestResource(),
                _ => Observable.Throw<Unit>(new InvalidOperationException("Using Stream Failure")));
        
        [Test]
        public void UsingItemResilient_Captures_Trace_When_Stream_Throws() {
            var testStream = ResilientUsing_Stream_Fails_Operation();
            using var testObserver = testStream.Test();

            testObserver.CompletionCount.ShouldBe(1);

            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.GetLogicalStackTrace().ToList();

            logicalStack.ShouldNotBeEmpty();
            logicalStack.ShouldContain(frame => frame.MemberName == nameof(ResilientUsing_Stream_Fails_Operation));
        }
    }
}