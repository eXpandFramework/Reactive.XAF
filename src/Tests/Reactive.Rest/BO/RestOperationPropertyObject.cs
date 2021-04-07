using System.Net.Http;
using DevExpress.ExpressApp.DC;
using Newtonsoft.Json;

namespace Xpand.XAF.Modules.Reactive.Rest.Tests.BO {
    [DomainComponent]
    // [RestOperation(Operation.Update, "UpdateObject")]
    [RestOperation(Operation.Get, "Get"+nameof(RestOperationPropertyObject) )]
    public class RestOperationPropertyObject:NonPersistentBaseObject {
        private const string BaseUrl = "/ver1/";
        private const string InstanceUrl = BaseUrl + ("{" + nameof(Oid) + "}/");
        bool _isEnabled;
        [JsonProperty("is_enabled")]
        [RestOperation(nameof(HttpMethod.Post), InstanceUrl + "enable", Criteria = nameof(IsEnabled) + "=true")]
        [RestOperation(nameof(HttpMethod.Post), InstanceUrl + "disable", Criteria = nameof(IsEnabled) + "=false")]
        public bool IsEnabled {
            get => _isEnabled;
            set => SetPropertyValue(ref _isEnabled, value);
        }

    }
}