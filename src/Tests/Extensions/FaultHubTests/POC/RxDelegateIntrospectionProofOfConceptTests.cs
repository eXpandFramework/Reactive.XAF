using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests.POC {
    [TestFixture]
    public class RxDelegateIntrospectionProofOfConceptTests {
        private string MyBusinessLogicMethod() => "Business Logic Result";

        
        private string FrameworkHelperSync(Func<string> businessLogic) => businessLogic.Method.Name;

        [Test]
        public void Can_Extract_Method_Name_From_Delegate_Synchronously() {
            var result = FrameworkHelperSync(MyBusinessLogicMethod);
            
            result.ShouldBe(nameof(MyBusinessLogicMethod));
        }
        
        private IObservable<string> FrameworkHelperAsyncWithTimer(Func<string> businessLogic) 
            => Observable.Timer(TimeSpan.FromMilliseconds(20))
                .Select(_ => businessLogic.Method.Name);

        [Test]
        public async Task Can_Extract_Method_Name_From_Delegate_On_Timer_Thread() {
            var result = await FrameworkHelperAsyncWithTimer(MyBusinessLogicMethod);
            
            result.ShouldBe(nameof(MyBusinessLogicMethod));
        }
    }
}