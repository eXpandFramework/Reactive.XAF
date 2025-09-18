using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Relay.Transaction;

namespace Xpand.Extensions.Tests.FaultHubTests.ProofOfConcepts{
    public class AggregateStepChainTests {
        [Test]
        public async Task Aggregate_Correctly_Aggregates_All_TransactionAbortedExceptions() {
            var seed = Observable.Return((Results: new List<string>(), Failures: new List<Exception>()));
            var steps = new[] { () => {
                    var innerFault1 = new FaultHubException("Inner 1", new InvalidOperationException("Failure 1"), new AmbientFaultContext());
                    return Observable.Throw<string>(new TransactionAbortedException("Aborted 1", innerFault1, new AmbientFaultContext()));
                }, () => {
                    var innerFault2 = new FaultHubException("Inner 2", new InvalidOperationException("Failure 2"), new AmbientFaultContext());
                    return Observable.Throw<string>(new TransactionAbortedException("Aborted 2", innerFault2, new AmbientFaultContext()));
                }
            };

            var finalState = await steps.Aggregate(seed, (accObservable, step) => {
                return accObservable.Select(acc => {
                    return step()
                        .Materialize()
                        .ToList()
                        .Select(notifications => {
                            var stepErrors = notifications.Where(n => n.Exception != null).Select(n => n.Exception).ToList();
                            var updatedFailures = acc.Failures.Concat(stepErrors).ToList();
                            return acc with{ Failures = updatedFailures };
                        });
                }).Concat();
            }).SingleAsync();

            finalState.Failures.Count.ShouldBe(2);
            
        }

    }
}