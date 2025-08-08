using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xpand.Extensions.ExpressionExtensions;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Filter{
    public static partial class Filter{
        public static IObservable<(TObject sender, PropertyChangedEventArgs e)> WhenPropertyChanged<TObject>(this TObject o, Expression<Func<TObject, object>> memberSelector) where TObject : INotifyPropertyChanged 
            => o.WhenPropertyChanged().Where(_ =>memberSelector==null|| _.e.PropertyName == memberSelector.MemberExpressionName());

        public static IObservable<(TObject sender, PropertyChangedEventArgs e)> WhenPropertyChanged<TObject>(this TObject o,params string[] names) where TObject : INotifyPropertyChanged 
            => o.ProcessEvent<PropertyChangedEventArgs>(nameof(INotifyPropertyChanged.PropertyChanged))
                .Where(eventArgs =>!names.Any()|| names.Any(s => s==eventArgs.PropertyName)).InversePair(o);

        public static IObservable<(TObject sender, PropertyChangedEventArgs e)> WhenPropertyChanged<TObject>(this IObservable<TObject> source,
            Expression<Func<TObject, object>> memberSelector, IScheduler scheduler = null) where TObject : INotifyPropertyChanged 
            => source.SelectMany(o => o.WhenPropertyChanged(memberSelector));
        
    }
}