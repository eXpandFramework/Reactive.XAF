using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib;

namespace Xpand.Extensions.Tests{
	public class RoundRobinTests:BaseTest{
		[Test]
		public void Observe_Unseen_values(){
			var source = Observable.Range(1, 100).Publish();
			var dist = source.RoundRobin();

			var testObserver1 = dist.Test();
			var testObserver2 = dist.Test();

			testObserver2.Items.Intersect(testObserver1.Items).Count().ShouldBe(0);
		}

	}
}