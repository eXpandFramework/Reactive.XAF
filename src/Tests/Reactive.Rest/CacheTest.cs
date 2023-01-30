using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Rest.Tests.BO;
using Xpand.XAF.Modules.Reactive.Rest.Tests.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive.Rest.Tests {
    public class CacheTest:RestCommonAppTest {
        [TestCase(1)]
        [TestCase(2)]
        public async Task Cache_Get_Requests(int times) {
            var typeInfo = Application.TypesInfo.FindTypeInfo(typeof(RestPropertyObject));
            var operationAttribute = typeInfo.FindAttributes<RestOperationAttribute>().First(attribute => attribute.Operation==Operation.Get);
            if (times == 2) {
                operationAttribute.PollInterval = 0;
            }
            HandlerMock.SetupRestPropertyObject(Application.CreateObjectSpace(typeof(RestPropertyObject)));
            await Application.CreateObjectSpace(typeof(RestPropertyObject))
                .Request(typeof(RestPropertyObject)).FirstAsync().Timeout(Timeout);
            
            await Application.CreateObjectSpace(typeof(RestPropertyObject))
                .Request(typeof(RestPropertyObject)).Timeout(Timeout);
        
            HandlerMock.VerifySend(Times.Exactly(times),message => $"{message.RequestUri}".Contains($"Get{nameof(RestPropertyObject)}") );
        
            operationAttribute.PollInterval = RestOperationAttribute.DefaultPollingInterval;
        }

        [Test]
        public async Task Do_not_Cache_Post_Requests() {
            using var objectSpace = Application.CreateObjectSpace(typeof(RestOperationObject));
            var restOperationObject = objectSpace.CreateObject<RestOperationObject>();
            var objects = new[] {restOperationObject};
            HandlerMock.SetupRestOperationObject(objects);
            var restObject = await objectSpace.Request<RestOperationObject>().Timeout(Timeout);
            restObject.Name = "1";
            objectSpace.CommitChanges();
            restObject.Name = "2";
            objectSpace.CommitChanges();
            await RestService.Object.FirstAsync().ToTaskWithoutConfigureAwait();

            HandlerMock.VerifySend(Times.Exactly(2),message => $"{message.RequestUri}".Contains($"Update{nameof(RestOperationObject)}") );
        }

    }
}