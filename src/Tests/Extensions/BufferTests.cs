using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib;

namespace Xpand.Extensions.Tests; 
public class BufferTests : BaseTest {

    [Test]
    public void BufferUntilCompleted() {
    
        var subject = new Subject<Unit>();
        var testObserver = subject.BufferUntilCompleted().Test();
            
        subject.OnNext(Unit.Default);
        subject.OnNext(Unit.Default);
        testObserver.ItemCount.ShouldBe(0);
        subject.OnCompleted();
            
        testObserver.ItemCount.ShouldBe(1);
        testObserver.Items.First().Length.ShouldBe(2);
        testObserver.CompletionCount.ShouldBe(1);
        
            
    }

}