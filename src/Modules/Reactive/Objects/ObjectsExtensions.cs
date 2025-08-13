using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.NonPersistentObjects;


namespace Xpand.XAF.Modules.Reactive.Objects {
    public static class ObjectsExtensions {
        public static IObservable<KeyValuePair<object, string>> WhenCheckedListBoxItems(this ObjectString objectString, IMemberInfo member, object o) 
            => objectString.WhenCheckedListBoxItems()
                .SelectManyItemResilient(e => member.FindAttributes<DataSourcePropertyAttribute>()
                    .SelectMany(attribute => ((IEnumerable) member.Owner.FindMember(attribute.DataSourceProperty).GetValue(o)).Cast<object>())
                    .ToDictionary(o1 => o1, o1 => $"{o1}").ToObservable(Transform.ImmediateScheduler)
                    .SwitchIfEmpty(Observable.Defer(() => ((IEnumerable) member.GetValue(o)).Cast<object>()
                        .ToDictionary(o1 => (object)$"{o1}", o1 => $"{o1}").ToObservable(Transform.ImmediateScheduler)))
                    .Where(dict=>!e.Objects.ContainsKey(dict.Key))
                    .Do(pair => e.Objects.Add(pair.Key,pair.Value)));

        public static IObservable<CheckListboxItemsProviderArgs> WhenCheckedListBoxItems(this ObjectString objectString) 
            => objectString.ProcessEvent<CheckListboxItemsProviderArgs>(nameof(ObjectString.CheckedListBoxItems));
        
        public static IObservable<T> WhenObjectSpaceChanged<T>(this T baseObject) where T:NonPersistentBaseObject
            => baseObject.ProcessEvent(nameof(NonPersistentBaseObject.ObjectSpaceChanged));
    }
}
