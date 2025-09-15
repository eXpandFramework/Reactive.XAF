// FILE: TransactionalApiContextPoCTests.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.Reactive.ProofOfConcepts {
    
    // --- PoC: Core Ambient Context Infrastructure ---
    public class PoCTransactionContext {
        public List<object> AllResults { get; } = new();
    }

    public static class PoCTransaction {
        public static readonly AsyncLocal<PoCTransactionContext> Current = new();
    }
    
    // --- PoC: Fluent API Simulation ---
    public class PoCStep {
        public Func<object[], IObservable<object[]>> Selector { get; init; }
    }

    public interface IPoCTransactionBuilder {
        IPoCTransactionBuilder Then(Func<object[], IObservable<object[]>> selector);
    }

    public class PoCTransactionBuilder : IPoCTransactionBuilder {
        internal readonly List<PoCStep> Steps = new();
        internal IObservable<object[]> InitialSource { get; init; }

        public IPoCTransactionBuilder Then(Func<object[], IObservable<object[]>> selector) {
            Steps.Add(new PoCStep { Selector = selector });
            return this;
        }
    }
    
    public static class PoCExtensions {
        public static IPoCTransactionBuilder BeginPoCWorkflow<T>(this IObservable<T[]> source) 
            => new PoCTransactionBuilder { InitialSource = source.Select(arr => arr.Cast<object>().ToArray()) };

        public static IObservable<object[]> RunPoC(this IPoCTransactionBuilder builder) {
            return Observable.Defer(() => {
                PoCTransaction.Current.Value = new PoCTransactionContext();
                var concreteBuilder = (PoCTransactionBuilder)builder;

                var initialAccumulator = concreteBuilder.InitialSource.Select(results =>
                    (Results: results, Failures: new List<Exception>()));

                var stepRunner = concreteBuilder.Steps.Aggregate(
                    initialAccumulator,
                    (accObservable, step) => accObservable.SelectMany(acc => {
                        //MODIFICATION: START
                        var stepIndex = concreteBuilder.Steps.IndexOf(step);
                        Console.WriteLine($"[PoC Runner] Preparing Step {stepIndex + 1}...");
                        
                        var inputForThisStep = acc.Results;
                        Console.WriteLine($"[PoC Runner] > Input for this step has {inputForThisStep.Length} items. Prior failures: {acc.Failures.Count}.");

                        return step.Selector(inputForThisStep)
                            .Materialize()
                            .ToList()
                            .Select(notifications => {
                                var errors = notifications.Where(n => n.Kind == System.Reactive.NotificationKind.OnError).Select(n => n.Exception).ToList();
                                var onNextValues = notifications.Where(n => n.Kind == System.Reactive.NotificationKind.OnNext).SelectMany(n => n.Value).ToArray();
                                
                                PoCTransaction.Current.Value.AllResults.AddRange(onNextValues);
                                
                                var resultsForNextStep = errors.Any() ? new object[0] : onNextValues;
                                var allFailures = acc.Failures.Concat(errors).ToList();

                                Console.WriteLine($"[PoC Runner] < Step {stepIndex + 1} finished. Errors: {errors.Count}. Results emitted: {onNextValues.Length}. Total accumulated results: {PoCTransaction.Current.Value.AllResults.Count}");
                                
                                return (Results: resultsForNextStep, Failures: allFailures);
                            });
                        //MODIFICATION: END
                    })
                );

                return stepRunner
                    .SelectMany(finalAcc => {
                        Console.WriteLine($"[PoC Runner] Finalizing transaction. Total failures: {finalAcc.Failures.Count}. Total results: {PoCTransaction.Current.Value.AllResults.Count}.");
                        if (finalAcc.Failures.Any()) {
                            return Observable.Throw<object[]>(new AggregateException(finalAcc.Failures));
                        }
                        return Observable.Return(finalAcc.Results);
                    })
                    .Finally(() => PoCTransaction.Current.Value = null);
            });
        }
    }

    // --- PoC: Test Fixture ---
    [TestFixture]
    public class TransactionalApiContextPoCTests {
        
        private IObservable<object[]> InnerFailingTransactionWithMixedTypes() {
            return Observable.Create<object[]>(observer => {
                // This simulates a failing RunToEnd that produces partial results.
                // In the real API, these would be collected and put on the context.
                // Here, we simulate that by emitting them before the error.
                observer.OnNext(new object[] { 123 });
                observer.OnNext(new object[] { "some_string" });
                observer.OnError(new InvalidOperationException("Inner Failure"));
                return System.Reactive.Disposables.Disposable.Empty;
            });
        }

        [Test]
        public async Task ContextSideChannel_Delivers_PartialResults_While_MainChannel_Is_TypeSafe() {
            var finalStepWasCalled = false;
            var receivedPartialsFromContext = new List<object>();

            var outerWorkflow = Observable.Return(new object[0])
                .BeginPoCWorkflow()
                .Then(initialEmptyInput => {
                    initialEmptyInput.ShouldBeEmpty();
                    return InnerFailingTransactionWithMixedTypes();
                })
                .Then(inputFromFailedStep => {
                    finalStepWasCalled = true;
                    inputFromFailedStep.ShouldBeEmpty("Input from a failed step must be an empty array.");
                    //MODIFICATION: START
                    receivedPartialsFromContext.AddRange(PoCTransaction.Current.Value.AllResults);
                    //MODIFICATION: END
                    return Observable.Return(new object[0]);
                });

            var result = await outerWorkflow.RunPoC().Capture();
            
            finalStepWasCalled.ShouldBe(true);
            result.Error.ShouldNotBeNull();
            result.Error.ShouldBeOfType<AggregateException>().InnerExceptions.Single()
                .ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Inner Failure");

            receivedPartialsFromContext.Count.ShouldBe(2);
            receivedPartialsFromContext.ShouldContain(123);
            receivedPartialsFromContext.ShouldContain("some_string");
        }
    }
}