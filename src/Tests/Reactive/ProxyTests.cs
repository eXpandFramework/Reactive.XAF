using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;

using NUnit.Framework;
using Shouldly;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;

namespace Xpand.XAF.Modules.Reactive.Tests{
	public class ProxyTests:BaseTest{
		
		public virtual object MethodInvocation(IObjectSpace objectSpace) => 0;

		[Test]
        [XpandTest()]
        public  void Skip_Method_Invocation(){
	        var tests = this.ToProxy(proxyTests => proxyTests.MethodInvocation(A.Is<IObjectSpace>()));
	        tests.WhenInvocation()
		        .Do(invocation => invocation.ReturnValue=1)
		        .Test();

	        var skipMethodInvocation = ((ProxyTests) tests).MethodInvocation(null);

			skipMethodInvocation.ShouldBe(1);
        }
		[Test]
        [XpandTest()]
        public  void Procceed_Method_Invocation(){
	        var tests = this.ToProxy(proxyTests => proxyTests.MethodInvocation(A.Is<IObjectSpace>()));
	        tests.WhenInvocation()
		        .Do(invocation => invocation.Proceed())
		        .Test();

	        var skipMethodInvocation = ((ProxyTests) tests).MethodInvocation(null);

			skipMethodInvocation.ShouldBe(0);
        }


	}

}