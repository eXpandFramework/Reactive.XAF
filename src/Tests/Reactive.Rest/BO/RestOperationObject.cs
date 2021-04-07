using DevExpress.ExpressApp.DC;

namespace Xpand.XAF.Modules.Reactive.Rest.Tests.BO {
    [DomainComponent]
    [RestOperation(Operation.Delete, "Delete"+nameof(RestOperationObject) )]
    [RestOperation(Operation.Create, "Create"+nameof(RestOperationObject) )]
    [RestOperation(Operation.Get,GetRequest )]
    [RestOperation(Operation.Update, "Update"+nameof(RestOperationObject))]
    [RestActionOperation("Act")]
    public class RestOperationObject : NonPersistentBaseObject {
        public const string GetRequest = "Get" + nameof(RestOperationObject);

        string _name;
        public string Name {
            get => _name;
            set => SetPropertyValue(ref _name, value);
        }

    }
} 