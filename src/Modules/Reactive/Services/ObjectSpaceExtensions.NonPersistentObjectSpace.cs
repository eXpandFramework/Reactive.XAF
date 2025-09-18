using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.NonPersistentObjects;
using NonPersistentBaseObject = DevExpress.ExpressApp.NonPersistentBaseObject;

namespace Xpand.XAF.Modules.Reactive.Services {
    public static class NonPersistentObjectsExtensions {
        #region High-Level Logical Operations
        internal static IObservable<Unit> ConnectObjectString(this ApplicationModulesManager manager) {
            return manager.WhenGeneratingModelNodes<IModelViews>().SelectMany().OfType<IModelDetailView>()
                .SelectMany(view => view.MemberViewItems()).Where(item =>
                    item.ModelMember.MemberInfo.Owner.Type == typeof(ObjectString) &&
                    item.PropertyName == nameof(ObjectString.Caption))
                .ToUnit().PushStackFrame();
        }
        #endregion

        #region Low-Level Plumbing
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

        public static void MarkChanged(this NonPersistentBaseObject baseObject,params string[] memberNames) 
            => memberNames.ForEach(s => baseObject.CallMethod("OnPropertyChanged", s));
        #endregion
    }
}