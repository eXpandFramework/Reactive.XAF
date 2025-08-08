using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp.Utils;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class BoolListExtensions{
        public static IObservable<(BoolList boolList, BoolValueChangedEventArgs e)> WhenResultValueChanged(this BoolList source,bool? newValue=null) 
            => source.Observe().ResultValueChanged(newValue);

        public static IObservable<(BoolList boolList, BoolValueChangedEventArgs e)> ResultValueChanged(
            this IObservable<BoolList> source,bool? newValue=null,[CallerMemberName]string caller="") 
            => source.SelectMany(item => item.ProcessEvent<BoolValueChangedEventArgs>(nameof(BoolList.ResultValueChanged),caller:caller)
                    .Where(eventArgs => !newValue.HasValue || eventArgs.NewValue == newValue).InversePair(item));
    }
}