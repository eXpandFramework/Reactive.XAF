using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Utils;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class BoolListExtensions{
        public static IObservable<(BoolList boolList, BoolValueChangedEventArgs e)> WhenResultValueChanged(this BoolList source,bool? newValue=null) 
            => source.ReturnObservable().ResultValueChanged(newValue);

        public static IObservable<(BoolList boolList, BoolValueChangedEventArgs e)> ResultValueChanged(
            this IObservable<BoolList> source,bool? newValue=null) 
            => source
                .SelectMany(item => Observable.FromEventPattern<EventHandler<BoolValueChangedEventArgs>, BoolValueChangedEventArgs>(
                    h => item.ResultValueChanged += h, h => item.ResultValueChanged -= h, ImmediateScheduler.Instance))
                .Where(pattern => !newValue.HasValue||pattern.EventArgs.NewValue==newValue)
                .TransformPattern<BoolValueChangedEventArgs, BoolList>();
    }
}