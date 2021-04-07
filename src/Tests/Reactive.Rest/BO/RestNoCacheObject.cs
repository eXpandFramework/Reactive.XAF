using DevExpress.ExpressApp.DC;

namespace Xpand.XAF.Modules.Reactive.Rest.Tests.BO {
    [DomainComponent]
    [RestOperation(Operation.Get, "Get"+nameof(RestNoCacheObject),PollInterval = 0)]
    public class RestNoCacheObject:NonPersistentBaseObject {
        
    }
}