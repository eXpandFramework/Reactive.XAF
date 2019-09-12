using System.Reactive.Subjects;
using akarnokd.reactive_extensions;
using Shouldly;
using TestsLib;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xunit;

namespace Xpand.XAF.Modules.Reactive.Tests{
    [Collection(nameof(ReactiveModule))]
    public class CommonExtensionsTests:BaseTest{
        [Fact]
        public void CountSubSequent(){
            var subject = new ReplaySubject<int>();
            subject.OnNext(0);
            subject.OnNext(0);
            subject.OnNext(1);
            subject.OnNext(0);

            var count = subject.CountSubsequent(i => i).SubscribeReplay();


            var items = count.Test().Items;
            items.Count.ShouldBe(2);
            items[0].length.ShouldBe(2);
            items[0].item.ShouldBe(0);
            items[1].length.ShouldBe(1);
            items[1].item.ShouldBe(1);
        }

    }
}