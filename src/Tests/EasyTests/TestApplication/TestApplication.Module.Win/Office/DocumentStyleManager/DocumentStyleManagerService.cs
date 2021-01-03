using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Module.Win.Office.DocumentStyleManager{
    public static class DocumentStyleManagerService{
        public static IObservable<Unit> ConnectDocumentStyleManager(this ApplicationModulesManager  manager) 
            => manager.WhenApplication(application => application.WhenListViewCreating(typeof(DocumentStyleLinkTemplate))
                    .SelectMany(t => {
                        var nonPersistentObjectSpace = ((NonPersistentObjectSpace) t.e.ObjectSpace);
                        var objectSpaces = nonPersistentObjectSpace.AdditionalObjectSpaces;
                        var needPersistentObjectSpace = objectSpaces.Any(space => space.IsKnownType(t.e.CollectionSource.ObjectTypeInfo.Type));
                        IObjectSpace objectSpace = null;
                        if (!needPersistentObjectSpace){
                            objectSpace = application.CreateObjectSpace(typeof(DocumentStyleLinkTemplate));
                            objectSpaces.Add(objectSpace);
                        }
                        
                        return nonPersistentObjectSpace.WhenDisposed().Do(_ => {
                            if (needPersistentObjectSpace){
                                objectSpace?.Dispose();
                            }
                        });
                    }))
                .Do(tuple => {})
                .ToUnit();
    }
}
