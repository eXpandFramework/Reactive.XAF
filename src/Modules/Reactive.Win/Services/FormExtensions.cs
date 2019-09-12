using System;
using System.Reactive.Linq;
using System.Windows.Forms;

namespace Xpand.XAF.Modules.Reactive.Win.Services{
    public static class FormExtensions{
        public static IObservable<Form> WhenLoad(this Form form){
            return Observable.FromEventPattern(h => form.Load += h, h => form.Load -= h)
                .Select(_ => form);
        }

        public static IObservable<Form> WhenVisibleChanged(this Form form){
            return Observable.FromEventPattern(h => form.VisibleChanged += h, h => form.VisibleChanged -= h)
                .Select(_ => form);
        }

        public static IObservable<Form> WhenActivated(this Form form){
            return Observable.FromEventPattern(h => form.Activated += h, h => form.Activated -= h)
                .Select(pattern => pattern.Sender).Cast<Form>();
        }

        public static IObservable<Form> WhenShown(this Form form){
            return Observable.FromEventPattern(h => form.Shown += h, h => form.Shown -= h)
                .Select(pattern => pattern.Sender).Cast<Form>();
        }
    }
}