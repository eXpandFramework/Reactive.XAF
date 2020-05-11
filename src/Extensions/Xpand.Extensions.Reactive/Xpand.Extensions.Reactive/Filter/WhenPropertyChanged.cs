using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Filter{
    public static partial class Filter{
        public static IObservable<TObject> WhenPropertyChanged<TObject>(this TObject o,
            Expression<Func<TObject, object>> memberSelector, IScheduler scheduler = null)
            where TObject : INotifyPropertyChanged{
            scheduler ??= ImmediateScheduler.Instance;
            var memberName = ((MemberExpression) memberSelector.Body).Member.Name;
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    h => o.PropertyChanged += h,
                    h => o.PropertyChanged -= h, scheduler)
                .TransformPattern<PropertyChangedEventArgs, TObject>()
                .Where(_ => _.e.PropertyName == memberName).Select(_ => _.sender);
        }
    }
}