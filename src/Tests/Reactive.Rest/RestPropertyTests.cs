using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform.Collections;
using Xpand.XAF.Modules.Reactive.Rest.Tests.BO;
using Xpand.XAF.Modules.Reactive.Rest.Tests.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive.Rest.Tests {
    public class RestPropertyTests:RestCommonAppTest {
        [Test]
        public async Task Arrays_BindingList() {
            HandlerMock.SetupRestPropertyObject(Application.CreateObjectSpace(typeof(RestPropertyObject)),o => o.StringArray=new []{"a"});
            
            var restObject = await Application.CreateObjectSpace(typeof(RestPropertyObject))
                .Request<RestPropertyObject>();
            
            restObject.StringArrayList.Count.ShouldBe(1);
            restObject.StringArrayList.First().Name.ShouldBe("a");
        }

        [Test]
        public async Task Writable_DomainComponent_Dependency() {
            HandlerMock.SetupRestPropertyObject(Application.CreateObjectSpace(typeof(RestPropertyObject)));
            
            var restObject = await Application.CreateObjectSpace(typeof(RestPropertyObject))
                .Request<RestPropertyObject>();
            
            restObject.RestOperationObject.ShouldNotBeNull();
            restObject.RestOperationObject.Name.ShouldBe(restObject.RestOperationObjectName);
        }

        [Test]
        public async Task ReadOnly_DomainComponent_Dependency() {
            HandlerMock.SetupRestPropertyObject(Application.CreateObjectSpace(typeof(RestPropertyObject)));
            
            var restObject = await Application.CreateObjectSpace(typeof(RestPropertyObject))
                .Request<RestPropertyObject>();
            
            restObject.RestObjectStats.ShouldNotBeNull();
            restObject.RestObjectStats.Name.ShouldBe(restObject.RestOperationObjectName);
        }

        [Test]
        public async Task ReactiveCollection_Fetch() {
            HandlerMock.SetupRestPropertyObject(Application.CreateObjectSpace(typeof(RestPropertyObject)));
            
            var restObject = await Application.CreateObjectSpace(typeof(RestPropertyObject))
                .Request<RestPropertyObject>();

            var whenListChanged = restObject.ActiveObjects.WhenListChanged().FirstAsync();
            var activeObjectsCount = restObject.ActiveObjects.Count;
            activeObjectsCount.ShouldBe(0);
            await whenListChanged;
            restObject.ActiveObjects.Count.ShouldBeGreaterThan(activeObjectsCount);
        }

        
    }
}