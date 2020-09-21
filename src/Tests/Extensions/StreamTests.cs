using System.IO;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.StreamExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Tests{
    public class StreamTests{
        [Test]
        public void Zip_Unizip_stream(){
            var memoryStream = new MemoryStream("test".Bytes());

            var bytes = memoryStream.GZip();
			
            bytes.Unzip().ShouldBe("test");
        }
		


    }
}