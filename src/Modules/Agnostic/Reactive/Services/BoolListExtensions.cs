using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Utils;
using DevExpress.XAF.Modules.Reactive.Extensions;

namespace DevExpress.XAF.Modules.Reactive.Services{
    public static class BoolListExtensions{

        public static IObservable<(BoolList boolList, BoolValueChangedEventArgs e)> WhenResultValueChanged(
            this BoolList source){
            return Observable.Return(source).ResultValueChanged();
        }

        public static IObservable<(BoolList boolList,BoolValueChangedEventArgs e)> ResultValueChanged(this IObservable<BoolList> source) {
            return source
                .SelectMany(item => {
                    return Observable.FromEventPattern<EventHandler<BoolValueChangedEventArgs>, BoolValueChangedEventArgs>(h => item.ResultValueChanged += h, h => item.ResultValueChanged -= h);
                })
                .Select(pattern => pattern)
                .TransformPattern<BoolValueChangedEventArgs,BoolList>();
        }

    }
}