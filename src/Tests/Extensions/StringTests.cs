using System.Reactive;
using System.Text;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Tests{
    public class StringTests{
        [Test]
        public void string_to_bytes(){
            var bytes = "test".Bytes();
			
            Encoding.UTF8.GetString(bytes).ShouldBe("test");
        }

    }
}