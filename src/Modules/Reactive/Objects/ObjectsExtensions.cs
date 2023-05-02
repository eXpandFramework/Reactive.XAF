using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.NonPersistentObjects;

namespace Xpand.XAF.Modules.Reactive.Objects {
    public static class ObjectsExtensions {
        public static IObservable<KeyValuePair<object, string>> WhenCheckedListBoxItems(this ObjectString objectString, IMemberInfo member, object o) 
            => objectString.WhenCheckedListBoxItems()
                .SelectMany(t2 => member.FindAttributes<DataSourcePropertyAttribute>()
                    .SelectMany(attribute => ((IEnumerable) member.Owner.FindMember(attribute.DataSourceProperty).GetValue(o)).Cast<object>())
                    .ToDictionary(o1 => o1, o1 => $"{o1}").ToObservable(Transform.ImmediateScheduler)
                    .SwitchIfEmpty(Observable.Defer(() => ((IEnumerable) member.GetValue(o)).Cast<object>()
                        .ToDictionary(o1 => (object)$"{o1}", o1 => $"{o1}").ToObservable(Transform.ImmediateScheduler)))
                    .Where(dict=>!t2.e.Objects.ContainsKey(dict.Key))
                    .Do(pair => t2.e.Objects.Add(pair.Key,pair.Value)));

        public static IObservable<(ObjectString sender, CheckListboxItemsProviderArgs e)> WhenCheckedListBoxItems(this ObjectString objectString) 
            => Observable.FromEventPattern<EventHandler<CheckListboxItemsProviderArgs>, CheckListboxItemsProviderArgs>(
                    h => objectString.CheckedListBoxItems += h, h => objectString.CheckedListBoxItems -= h,Transform.ImmediateScheduler)
                .TransformPattern<CheckListboxItemsProviderArgs,ObjectString>();
        
        public static IObservable<T> WhenObjectSpaceChanged<T>(this T baseObject) where T:NonPersistentBaseObject
            => Observable.FromEventPattern<EventHandler<EventArgs>, EventArgs>(
                    h => baseObject.ObjectSpaceChanged += h, h => baseObject.ObjectSpaceChanged -= h,Transform.ImmediateScheduler)
                .TransformPattern<EventArgs,T>()
                .Select(t => t.sender);
    }
}
