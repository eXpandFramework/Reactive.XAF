using System;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests.POC;
[TestFixture]
public class AmbientContextProofOfConceptTests {
    private static readonly AsyncLocal<MethodInfo> AmbientLogicMethod = new();

    private string MyBusinessLogicMethod() => CreateContext();

    private IObservable<string> MyBusinessLogicObservable() => Observable.Return(CreateContext());

    private string CreateContext() => AmbientLogicMethod.Value?.Name;

    private T FrameworkHelper<T>(Func<T> businessLogic) {
        var parentContext = AmbientLogicMethod.Value;
        try {
            AmbientLogicMethod.Value = businessLogic.Method;
            return businessLogic();
        }
        finally {
            AmbientLogicMethod.Value = parentContext;
        }
    }

    private IObservable<T> FrameworkHelperObservable<T>(Func<IObservable<T>> businessLogic) {
        return Observable.Create<T>(observer => {
            var parentContext = AmbientLogicMethod.Value;
            try {
                AmbientLogicMethod.Value = businessLogic.Method;
                return businessLogic().Subscribe(observer);
            }
            finally {
                AmbientLogicMethod.Value = parentContext;
            }
        });
    }


    [Test]
    public void Ambient_Context_Flows_Synchronously() {
        var result = FrameworkHelper(MyBusinessLogicMethod);

        result.ShouldBe(nameof(MyBusinessLogicMethod));

        AmbientLogicMethod.Value.ShouldBeNull();
    }

    [Test]
    public async Task Ambient_Context_Flows_Across_Rx_Timer_Thread() {
        var result = await FrameworkHelperObservable(MyBusinessLogicObservable)
            .SelectMany(s => Observable.Timer(TimeSpan.FromMilliseconds(20)).Select(_ => s));


        result.ShouldBe(nameof(MyBusinessLogicObservable));

        AmbientLogicMethod.Value.ShouldBeNull();
    }
}