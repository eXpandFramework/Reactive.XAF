using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Utils;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;


namespace Xpand.XAF.Modules.Reactive.Services{
    public static class BoolListExtensions{
        public static IObservable<(BoolList boolList, BoolValueChangedEventArgs e)> WhenResultValueChanged(this BoolList source,bool? newValue=null) 
            => source.ProcessEvent<BoolValueChangedEventArgs>(nameof(BoolList.ResultValueChanged),
                e => e.Observe().Where(_ => !newValue.HasValue || e.NewValue == newValue))
                .InversePair(source);

        public static IObservable<(BoolList boolList, BoolValueChangedEventArgs e)> ResultValueChanged(this IObservable<BoolList> source,bool? newValue=null) 
            => source.SelectMany(item => item.WhenResultValueChanged(newValue));
    }
}