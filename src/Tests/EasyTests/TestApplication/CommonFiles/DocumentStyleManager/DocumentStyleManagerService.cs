using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;

// ReSharper disable CheckNamespace

namespace TestApplication.Office.DocumentStyleManager{
    public static class DocumentStyleManagerService{
        public static IObservable<Unit> ConnectDocumentStyleManager(this ApplicationModulesManager  manager){
            return Observable.Empty<Unit>();
            // return manager.WhenApplication(application => application.WhenListViewCreating(typeof(DocumentStyleLinkTemplate))
            //         .SelectMany(t => {
            //             return Observable.Empty<Unit>();
            //             // var nonPersistentObjectSpace = ((NonPersistentObjectSpace) t.e.ObjectSpace);
            //             // var objectSpaces = nonPersistentObjectSpace.AdditionalObjectSpaces;
            //             // t.e.CollectionSource.ObjectTypeInfo.Members.Where(info => info.MemberTypeInfo.IsPersistent)
            //             //     .GroupBy(info => info)
            //             // var needPersistentObjectSpace = objectSpaces.Any(space => space.IsKnownType(t.e.CollectionSource.ObjectTypeInfo.Type));
            //             // IObjectSpace objectSpace = null;
            //             // if (!needPersistentObjectSpace){
            //             //     objectSpace = application.CreateObjectSpace(typeof(DocumentStyleLinkTemplate));
            //             //     objectSpaces.Add(objectSpace);
            //             // }
            //             //
            //             // return nonPersistentObjectSpace.WhenDisposed().Do(_ => {
            //             //     if (needPersistentObjectSpace){
            //             //         objectSpace?.Dispose();
            //             //     }
            //             // });
            //         }))
            //     .Do(tuple => {})
            //     .ToUnit();
        }

    }

    // public class Class1:ObjectViewController<ListView,DocumentStyleLinkTemplate>{
    //     protected override void OnActivated(){
    //         base.OnActivated();
    //     }
    // }

}
