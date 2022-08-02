using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Tests{
    public class StringTests{
        [Test]
        public void string_to_bytes(){
            var bytes = "test".Bytes();
			
            Encoding.UTF8.GetString(bytes).ShouldBe("test");
        }

        [Test]
        public void Protect() {
            var s = "test";
            var convertToBytes = s.Protect();

            var path = $"{AppDomain.CurrentDomain.ApplicationPath()}\\{DateTime.Now.Ticks}.txt";
            File.WriteAllBytes(path,convertToBytes);
            
            var secureString = File.ReadAllBytes(path).UnProtect().First();
            
            secureString.GetString().ShouldBe(s);
        }


    }
    
}