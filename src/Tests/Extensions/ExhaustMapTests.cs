using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Tests{
    [TestFixture]
    public class ExhaustMapTests {

        [Test][SuppressMessage("ReSharper", "StringLiteralTypo")]
        public async Task ExhaustMap_ShouldProcessFirstAvailableItemAfterPreviousObservableCompletes()
        {
            var source = new Subject<int>();
            var results = new List<string>();
            var completionFlags = new List<bool>();

            var exhaustMapped = source.ExhaustMap(i => Observable.Create<string>(observer => {
                Task.Run(async () =>
                {
                    await Task.Delay(i * 100);  
                    observer.OnNext($"Completed {i}");
                    observer.OnCompleted();
                });

                return () => completionFlags.Add(true);  
            }));
            
            var subscription = exhaustMapped.Subscribe(results.Add);
            source.OnNext(1);  
            await Task.Delay(50);  

            source.OnNext(2);  
            await Task.Delay(50);  

            source.OnNext(3);  
            await Task.Delay(50);  

            source.OnNext(4);  
            await Task.Delay(200);  

            await Task.Delay(500);  

            subscription.Dispose();

            
            results.ShouldBe(new[] { "Completed 1", "Completed 3" });
            completionFlags.Count.ShouldBe(2);  
        }
        [Test]
        public async Task ExhaustMap_Should_Ignore_Emissions_While_Previous_Observable_Not_Completed() {
            
            var source = new Subject<int>();
            var results = new List<int>();
        
            
            IObservable<int> DelayedFunction(int item) => Observable.Return(item).Delay(TimeSpan.FromMilliseconds(100));

            
            source.ExhaustMap(DelayedFunction).Subscribe(results.Add);
        
            
            source.OnNext(1);

            
            source.OnNext(2);
        
            
            await Task.Delay(150);

            
            source.OnNext(3);

            
            await Task.Delay(150);

            source.OnCompleted();

            
            results.ShouldBe(new[] { 1, 3 });
        }
    }
}