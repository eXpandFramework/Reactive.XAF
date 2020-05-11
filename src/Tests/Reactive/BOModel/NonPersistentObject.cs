using System.Collections.Generic;
using DevExpress.ExpressApp.DC;

namespace Xpand.XAF.Modules.Reactive.Tests.BOModel{
    [DomainComponent]
    public class NonPersistentObject{
        public List<NonPersistentObject> Childs{ get; }=new List<NonPersistentObject>();    
    }
}