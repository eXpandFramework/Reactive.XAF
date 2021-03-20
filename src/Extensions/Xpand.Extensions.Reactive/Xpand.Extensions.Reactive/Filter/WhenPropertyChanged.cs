using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xpand.Extensions.ExpressionExtensions;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Filter{
    public static partial class Filter{
        public static IObservable<(TObject sender, PropertyChangedEventArgs e)> WhenPropertyChanged<TObject>(this TObject o,
            Expression<Func<TObject, object>> memberSelector=null, IScheduler scheduler = null) where TObject : INotifyPropertyChanged{
            scheduler ??= ImmediateScheduler.Instance;
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    h => o.PropertyChanged += h,
                    h => o.PropertyChanged -= h, scheduler)
                .TransformPattern<PropertyChangedEventArgs, TObject>()
                .Where(_ =>memberSelector==null|| _.e.PropertyName == memberSelector.MemberExpressionName());
        }
        public static IObservable<(TObject sender, PropertyChangedEventArgs e)> WhenPropertyChanged<TObject>(this IObservable<TObject> source,
            Expression<Func<TObject, object>> memberSelector, IScheduler scheduler = null) where TObject : INotifyPropertyChanged 
            => source.SelectMany(o => o.WhenPropertyChanged(memberSelector, scheduler));
    }
}