using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.Tests {
    public class ObjectExtensionsTests {
        [TestCase(0,true)]
        public void DefaultValue(object value,bool isDefault) {
            value.IsDefaultValue().ShouldBe(isDefault);
        }

    }
}