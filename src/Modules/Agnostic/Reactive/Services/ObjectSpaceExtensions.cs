using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ObjectSpaceExtensions{
        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectsGettingEventArgs e)> ObjectsGetting(this IObservable<NonPersistentObjectSpace> source) {
            return source.SelectMany(item => item.WhenObjectsGetting());
        }
        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectsGettingEventArgs e)> WhenObjectsGetting(this NonPersistentObjectSpace item) {
            return Observable.FromEventPattern<EventHandler<ObjectsGettingEventArgs>, ObjectsGettingEventArgs>(h => item.ObjectsGetting += h, h => item.ObjectsGetting -= h)
                    .TakeUntil(item.WhenDisposed()).TransformPattern<ObjectsGettingEventArgs, NonPersistentObjectSpace>();
        }
        
        public static IObservable<(IObjectSpace objectSpace,EventArgs e)> Commited(this IObservable<IObjectSpace> source) {
            return source.SelectMany(item => item.WhenCommited());
        }

        public static IObservable<(IObjectSpace objectSpace,EventArgs e)> WhenCommited(this IObjectSpace item) {
            return Observable.FromEventPattern<EventHandler, EventArgs>(h => item.Committed += h, h => item.Committed -= h)
                    .TakeUntil(item.WhenDisposed()).TransformPattern<EventArgs, IObjectSpace>();
        }
        
        public static IObservable<(IObjectSpace objectSpace,ObjectsManipulatingEventArgs e)> ObjectDeleted(this IObservable<IObjectSpace> source) {
            return source.SelectMany(item => item.WhenObjectDeleted());
        }

        public static IObservable<(IObjectSpace objectSpace,ObjectsManipulatingEventArgs e)> WhenObjectDeleted(this IObjectSpace item) {
            return Observable.FromEventPattern<EventHandler<ObjectsManipulatingEventArgs>, ObjectsManipulatingEventArgs>(h => item.ObjectDeleted += h, h => item.ObjectDeleted -= h)
                    .TakeUntil(item.WhenDisposed()).TransformPattern<ObjectsManipulatingEventArgs, IObjectSpace>();
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

        public static IObservable<IObjectSpace> WhenModifyChanged(this IObjectSpace source){
            return source.AsObservable().ModifyChanged();
        }

        public static IObservable<IObjectSpace> ModifyChanged(this IObservable<IObjectSpace> source) {
            return source
                .SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(h => item.ModifiedChanged += h, h => item.ModifiedChanged -= h)
                    .Select(pattern => (IObjectSpace) pattern.Sender).TakeUntil(item.WhenDisposed()));
        }

    }
}