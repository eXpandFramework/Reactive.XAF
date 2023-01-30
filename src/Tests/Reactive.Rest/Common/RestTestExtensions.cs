using System;
using System.Linq;
using System.Net.Http;
using DevExpress.ExpressApp;
using Moq;
using Xpand.Extensions.JsonExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Rest.Tests.BO;

namespace Xpand.XAF.Modules.Reactive.Rest.Tests.Common {
    public static class RestTestExtensions {
        public static void SetupRestPropertyObject(this Mock<HttpMessageHandler> handlerMock,IObjectSpace objectSpace,Action<RestPropertyObject> configureRestPropertyObject=null) {
            handlerMock.SetupSend(message => {
                var requestUrl = $"{message.RequestMessage?.RequestUri}";
                if (requestUrl.Contains($"Get{nameof(RestPropertyObject)}")) {
                    var restPropertyObject = objectSpace.CreateObject<RestPropertyObject>();
                    restPropertyObject.RestOperationObjectName = "test";
                    restPropertyObject.Name = "test";
                    configureRestPropertyObject?.Invoke(restPropertyObject);
                    message.Content=message.StringContent($"Get{nameof(RestPropertyObject)}",restPropertyObject);
                }
                else if (requestUrl.Contains($"Get{nameof(RestOperationObject)}")) {
                    message.Content=message.StringContent($"Get{nameof(RestOperationObject)}",new RestOperationObject() {Name = "test"});
                }
                else if (requestUrl.Contains($"Get{nameof(RestObjectStats)}")) {
                    message.Content = message.StringContent($"Get{nameof(RestObjectStats)}",
                        new[] {
                            new RestObjectStats() {Name = Guid.NewGuid().ToString()},
                            new RestObjectStats() {Name = "test"}
                        });
                }
                else if (requestUrl.Contains(nameof(RestPropertyObject.ActiveObjects))) {
                    message.Content = message.StringContent(nameof(RestPropertyObject.ActiveObjects),new[]{new RestActiveObject()});
                }
                else {
                    throw new NotImplementedException();
                }
            }).Verifiable();
        }

        public static void SetupRestOperationObject<T>(this Mock<HttpMessageHandler> handlerMock,params T[] objects) where T:IObjectSpaceLink
            => handlerMock.SetupSend(message => {
                var requestUrl = $"{message.RequestMessage?.RequestUri}";
                if (requestUrl.Contains($"Get{typeof(T).Name}")) {
                    message.Content = message.StringContent($"Get{typeof(T).Name}",
                        objects.Where(link =>link.ObjectSpace==null|| !link.ObjectSpace.IsDeletedObject(link)).ToArray());
                }
                else if (new[]{$"Create{typeof(T).Name}",$"Update{typeof(T).Name}",$"Delete{typeof(T).Name}","enable","disable","Act"}.Any(s => requestUrl.Contains(s))) {
                    message.Content = new StringContent("[]");
                }
                else {
                    throw new NotImplementedException();
                }
                
            });

        

        static StringContent StringContent(this HttpResponseMessage message, string value,object content) {
            StringContent stringContent = null;
            if (message.RequestMessage?.RequestUri is not null && message.RequestMessage != null &&
                message.RequestMessage.RequestUri.AbsoluteUri.Contains(value)) {
                stringContent = new StringContent(content.Serialize());
            }
            return stringContent;
        }


    }
}