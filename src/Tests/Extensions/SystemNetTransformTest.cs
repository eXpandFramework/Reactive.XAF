using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform.System.Net;
using Xpand.Extensions.Reactive.Utility;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;


namespace Xpand.Extensions.Tests{
	public class SystemNetTransformTest:BaseTest{
        [Test]
        [XpandTest]
        public async System.Threading.Tasks.Task Signal_When_In_Listening(){
            var portInUse = Enumerable.Range(10000,2).Select(port => new IPEndPoint(IPAddress.Loopback, port)).ToArray().Listening().SubscribeReplay();
            var tcpListener = new TcpListener(IPAddress.Loopback,10001);
            tcpListener.Start();
            await portInUse.FirstAsync(endPoint => endPoint.Port == 10001);
            tcpListener.Stop();
            
            portInUse.Test().ItemCount.ShouldBe(1);
        }

        [Test]
        [XpandTest(tryCount:1)]
        public async System.Threading.Tasks.Task Signal_When_Listening_Subsequent(){
            var portInUse = Enumerable.Range(10000,2).Select(port => new IPEndPoint(IPAddress.Loopback, port)).ToArray().Listening().SubscribeReplay();
            var tcpListener = new TcpListener(IPAddress.Loopback,10000);
            tcpListener.Start();
            
            await portInUse.FirstAsync(endPoint => endPoint.Port == 10000);
            
            tcpListener.Stop();
            portInUse.Test().ItemCount.ShouldBe(1);
            
            tcpListener = new TcpListener(IPAddress.Loopback,10001);
            tcpListener.Start();
            await System.Threading.Tasks.Task.Delay(500);
            await portInUse.FirstAsync(endPoint => endPoint.Port == 10001);
            portInUse.Test().ItemCount.ShouldBe(2);

            tcpListener = new TcpListener(IPAddress.Loopback,10000);
            tcpListener.Start();
            await portInUse.Skip(2).FirstAsync(endPoint => endPoint.Port == 10000);
            tcpListener.Stop();
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(1));
            portInUse.Test().ItemCount.ShouldBe(3);
        }


    }
}