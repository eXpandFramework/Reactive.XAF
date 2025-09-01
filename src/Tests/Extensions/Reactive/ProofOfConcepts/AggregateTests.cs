using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.Reactive.ProofOfConcepts{
    public class AggregateTests {
        [Test]
        public async Task Aggregate_Preserves_Accumulator_State_On_Sequential_Failures() {
            var seed = Observable.Return((Results: new List<string>(), Failures: new List<Exception>()));
            var steps = new[] {
                () => Observable.Throw<string>(new InvalidOperationException("Failure 1")),
                () => Observable.Throw<string>(new InvalidOperationException("Failure 2"))
            };

            var finalState = await steps.Aggregate(seed, (accObservable, step) => {
                return accObservable.SelectMany(acc => {
                    return step()
                        .Materialize()
                        .ToList()
                        .Select(notifications => {
                            var stepErrors = notifications.Where(n => n.Exception != null).Select(n => n.Exception).ToList();
                            var updatedFailures = acc.Failures.Concat(stepErrors).ToList();
                            return acc with{ Failures = updatedFailures };
                        });
                });
            }).SingleAsync();

            finalState.Failures.Count.ShouldBe(2);
        }
        
    }
}