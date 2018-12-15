using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.XAF.Modules.Reactive.Extensions;

namespace DevExpress.XAF.Modules.Reactive.Services{
    public static class ObjectSpaceExtensions{
        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectsGettingEventArgs e)> ObjectsGetting(this IObservable<NonPersistentObjectSpace> source) {
            return source
                .SelectMany(item => Observable.FromEventPattern<EventHandler<ObjectsGettingEventArgs>, ObjectsGettingEventArgs>(h => item.ObjectsGetting += h, h => item.ObjectsGetting -= h))
                .Select(pattern => pattern)
                .TransformPattern<ObjectsGettingEventArgs,NonPersistentObjectSpace>();
        }
        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectsGettingEventArgs e)> WhenObjectsGetting(this NonPersistentObjectSpace source) {
            return Observable.Return(source).ObjectsGetting();
        }
        
        public static IObservable<(IObjectSpace objectSpace,EventArgs e)> Disposed(this IObservable<IObjectSpace> source) {
            return source
                .SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(h => item.Disposed += h, h => item.Disposed -= h))
                .Select(pattern => pattern)
                .TransformPattern<EventArgs,IObjectSpace>();
        }
        public static IObservable<(IObjectSpace objectSpace,EventArgs e)> WhenDisposed(this IObjectSpace source) {
            return Observable.Return(source).Disposed();
        }

    }
}