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
            var tcs1 = new TaskCompletionSource<string>();
            var tcs3 = new TaskCompletionSource<string>();
            var res = new ReplaySubject<string>();

            source.ExhaustMap(i => i switch {
                1 => Observable.FromAsync(() => tcs1.Task),
                3 => Observable.FromAsync(() => tcs3.Task),
                _ => Observable.Return($"{i}")
            }).Subscribe(res);

            source.OnNext(1);
            source.OnNext(2); // Dropped because tcs1 is incomplete
            tcs1.SetResult("1");
            (await res.FirstAsync()).ShouldBe("1");

            source.OnNext(3);
            source.OnNext(4); // Dropped because tcs3 is incomplete
            tcs3.SetResult("3");
            (await res.Skip(1).FirstAsync()).ShouldBe("3");
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