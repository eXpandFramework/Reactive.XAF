using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Rest.Tests.BO;
using Xpand.XAF.Modules.Reactive.Rest.Tests.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive.Rest.Tests {
    public class  RestOperationTests:RestCommonAppTest {
        private RestOperationObject[] _restObjects;

        public override void Setup() {
            base.Setup();
            _restObjects = new[] {new RestOperationObject() {Name = "test"}, new RestOperationObject() {Name = "test2"}};
        }

        [Test]
        public async Task Request_object() {
            HandlerMock.SetupRestOperationObject(_restObjects);
            
            var restObject = await Application.CreateObjectSpace(typeof(RestOperationObject))
                .Request<RestOperationObject>();
            
            restObject.Name.ShouldBe(_restObjects.Last().Name);
        }
        
        [Test]
        public async Task Create_object() {
            HandlerMock.SetupRestOperationObject(_restObjects);
            var objectSpace = Application.CreateObjectSpace(typeof(RestOperationObject));
            objectSpace.CreateObject<RestOperationObject>();
            
            objectSpace.CommitChanges();
            await RestService.Object.FirstAsync();
        }

        [Test]
        public async Task Update_object() {
            var objects = new[]{new RestOperationObject()};
            HandlerMock.SetupRestOperationObject(objects);
            var objectSpace = Application.CreateObjectSpace(typeof(RestOperationObject));
            var restObject = await objectSpace.Request<RestOperationObject>();
            restObject.Name = "update";
            
            objectSpace.CommitChanges();
            await RestService.Object.FirstAsync();
        }

        [Test]
        public async Task Delete_object() {
            var objectSpace = Application.CreateObjectSpace(typeof(RestOperationObject));
            var objects = new[]{objectSpace.CreateObject<RestOperationObject>()};
            HandlerMock.SetupRestOperationObject(objects);
            var restObject = await objectSpace.Request<RestOperationObject>();
            
            objectSpace.Delete(restObject);
            objectSpace.CommitChanges();
            await RestService.Object.FirstAsync();
        }

        [TestCase(false,"disable")]
        [TestCase(true,"enable")]
        public async Task Property_Operation_on_existing_object(bool isEnable,string name) {
            var objects = new[]{new RestOperationPropertyObject(){IsEnabled = !isEnable}};
            HandlerMock.SetupRestOperationObject(objects);
            var objectSpace = Application.CreateObjectSpace(typeof(RestOperationPropertyObject));
            var restObject = await objectSpace.Request<RestOperationPropertyObject>();
            restObject.IsEnabled = isEnable;
            objectSpace.CommitChanges();

            await RestService.Object.FirstAsync(t =>
                t.message.RequestMessage?.RequestUri is not null && t.message.RequestMessage != null &&
                t.message.RequestMessage.RequestUri.AbsoluteUri.Contains(name));

        }

        [Test]
        public void RestOperation_Action() {
            HandlerMock.SetupRestOperationObject(_restObjects);
            var window = Application.CreateViewWindow();
            var detailView = Application.NewDetailView(typeof(RestOperationObject));
            window.SetView(detailView);

            var action = window.Action("Act");
            action.ShouldNotBeNull();
            action.Active.ResultValue.ShouldBeTrue();
            action.DoExecute(_ => new[]{detailView.CurrentObject});

            HandlerMock.VerifySend(Times.Exactly(1),message => $"{message.RequestUri}".Contains("Act") );

        }


    }
}