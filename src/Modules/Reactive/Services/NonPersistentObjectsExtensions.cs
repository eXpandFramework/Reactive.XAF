using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.NonPersistentObjects;

namespace Xpand.XAF.Modules.Reactive.Services {
    public static class NonPersistentObjectsExtensions {
        internal static IObservable<Unit> ConnectObjectString(this ApplicationModulesManager manager) {
            return manager.WhenGeneratingModelNodes<IModelViews>().SelectMany().OfType<IModelDetailView>()
                .SelectMany(view => view.MemberViewItems()).Where(item =>
                    item.ModelMember.MemberInfo.Owner.Type == typeof(ObjectString) &&
                    item.PropertyName == nameof(ObjectString.Caption))
                // .Do(item => item.PropertyEditorType = item.PropertyEditorTypes.FirstOrDefault(type =>
                    // type.FullName == "DevExpress.ExpressApp.Blazor.Editors.CheckedLookupStringPropertyEditor"))
                .ToUnit();
            // .Merge(manager.WhenApplication(application => {
            //     var detailViewCreated = application.WhenDetailViewCreated().Where(t => t.e.View.ObjectTypeInfo.Type!=typeof(ObjectString))
            //             .SelectMany(t => application.WhenFrameViewChanged().WhenFrame(typeof(ObjectString)).WhenFrame(ViewType.DetailView)
            //                 .SelectMany(frame => ((ObjectString) frame.View.AsDetailView().CurrentObject).Datasource
            //                     .Do(e => {
            //                         e.Instance.Objects
            //                     })))
            //         ;
            //     
            //     return datasource.CombineLatest(detailViewCreated);
            // }));
        }

        public static BindingList<ObjectString> ToObjectString(this IEnumerable<object> source, IObjectSpace objectSpace)
            => source.Select(o1 => {
                var isModified =objectSpace.IsDisposed || objectSpace.ModifiedObjects.Contains(o1);
                var name = $"{o1}";
                var objectString =objectSpace.IsDisposed?new ObjectString(name): objectSpace.CreateObject<ObjectString>();
                objectString.Name = name;
                objectString.Caption = name;
                if (!isModified) {
                    objectSpace.RemoveFromModifiedObjects(objectString);
                }
                return objectString;
            }).ToList().ToBindingList();

    }
}
