using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;

namespace Xpand.Extensions.Tests.FaultHubTests.TransactionalApi {
    [TestFixture]
    public class TransactionalApiNameInferenceTests  {
        public static IEnumerable<TestCaseData> GetStepName_Source() {
            yield return new TestCaseData("ignored", "ExplicitName", "ExplicitName")
                .SetName("Explicit Name");
            yield return new TestCaseData("SimpleMethod()", null, "SimpleMethod")
                .SetName("Simple Method Call");
            yield return new TestCaseData("SimpleMethod(a, b)", null, "SimpleMethod")
                .SetName("Simple Method With Parameters");
            yield return new TestCaseData("MyModule.MyClass.MyMethod(int arg)", null, "MyModule.MyClass.MyMethod")
                .SetName("Qualified Method With Parameters");
            yield return new TestCaseData("MyMethod()[0]", null, "MyMethod")
                .SetName("Method With Indexer");
            yield return new TestCaseData("webScraper.ExtractLinks(new Uri(url)))[0]", null, "webScraper.ExtractLinks")
                .SetName("Qualified Method With Parameters and Indexer");
            yield return new TestCaseData("_ => MyMethod()", null, "MyMethod")
                .SetName("Lambda Expression");
            yield return new TestCaseData("builder.UpComing)", null, "builder.UpComing")
                .SetName("Orphan Parenthesis");
        }

        [Test, TestCaseSource(nameof(GetStepName_Source))]
        public void Should_Correctly_Infer_Step_Name(string expression, string explicitName, string expected) {
            var result = Transaction.GetStepName(expression, explicitName);
            result.ShouldBe(expected);
        }
    }}