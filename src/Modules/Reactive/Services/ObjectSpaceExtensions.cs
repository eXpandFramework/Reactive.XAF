using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ObjectSpaceExtensions{
        public static IObservable<T> ModifiedExistingObject<T>(this XafApplication application,
            Func<(IObjectSpace objectSpace,ObjectChangedEventArgs e),bool> filter = null){
            filter = filter ?? (_ => true);
            return application.AllModifiedObjects<T>(_ => filter(_) && !_.objectSpace.IsNewObject(_.e.Object));
        }

        public static IObservable<T> ModifiedNewObject<T>(this XafApplication application,
            Func<(IObjectSpace objectSpace,ObjectChangedEventArgs e),bool> filter = null){
            filter = filter ?? (_ => true);
            return application.AllModifiedObjects<T>(_ => filter(_) && _.objectSpace.IsNewObject(_.e.Object));
        }

        public static IObservable<T> AllModifiedObjects<T>(this XafApplication application,Func<(IObjectSpace objectSpace,ObjectChangedEventArgs e),bool> filter=null ){
            return application.WhenObjectSpaceCreated()
                .SelectMany(_ => _.e.ObjectSpace.WhenObjectChanged()
                    .Where(tuple => filter == null || filter(tuple))
                    .SelectMany(tuple => tuple.objectSpace.ModifiedObjects.OfType<T>()))
                ;
        }

        public static IObservable<IObjectSpace> ToObjectSpace(this IObjectSpaceProvider provider){
            return provider == null ? Observable.Empty<IObjectSpace>() : Observable.Using(provider.CreateObjectSpace, space => space.AsObservable());
        }

        public static IObservable<IObjectSpace> ToObjectSpace(this XafApplication application){
            return Observable.Using(application.CreateObjectSpace, space => space.AsObservable());
        }

        public static T GetObject<T>(this XafApplication application,T value){
            return application.CreateObjectSpace().GetObject(value);
        }

        public static IObservable<T> ExistingObject<T>(this XafApplication application,Func<IQueryable<T>,IQueryable<T>> query=null){
            return Observable.Using(application.CreateObjectSpace, space => space.ExistingObject(query));
        }

        public static IObservable<T> AnyObject<T>(this XafApplication application){
            return application.NewObject<T>().Merge(application.ExistingObject<T>()).DistinctUntilChanged()
                .TraceRX();
        }

        public static IObservable<T> WhenObjectCommited<T>(this IObservable<T> source) where T:IObjectSpaceLink{
            return source.SelectMany(_ => _.ObjectSpace.WhenCommited().FirstAsync().Select(tuple => _));
        }

        public static IObservable<T> NewObject<T>(this IObjectSpace objectSpace){
            return objectSpace.WhenCommiting()
                .SelectMany(t => objectSpace.ModifiedObjects.OfType<T>().Where(r => t.objectSpace.IsNewObject(r)))
                .TraceRX();
        }

        public static IObservable<T> NewObject<T>(this XafApplication application){
            return application.WhenObjectSpaceCreated().SelectMany(_ => _.e.ObjectSpace.NewObject<T>());
        }

        public static IObservable<T> ExistingObject<T>(this IObjectSpace objectSpace,Func<IQueryable<T>,IQueryable<T>> query=null){
            var objectsQuery = objectSpace.GetObjectsQuery<T>();
            if (query != null){
                objectsQuery = objectsQuery.Concat(query(objectsQuery));
            }
            return objectsQuery.ToObservable().TraceRX();
        }

        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectsGettingEventArgs e)> ObjectsGetting(this IObservable<NonPersistentObjectSpace> source) {
            return source.SelectMany(item => item.WhenObjectsGetting());
        }
        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectsGettingEventArgs e)> WhenObjectsGetting(this NonPersistentObjectSpace item) {
            return Observable.FromEventPattern<EventHandler<ObjectsGettingEventArgs>, ObjectsGettingEventArgs>(h => item.ObjectsGetting += h, h => item.ObjectsGetting -= h)
                .TransformPattern<ObjectsGettingEventArgs, NonPersistentObjectSpace>();
        }
        
        public static IObservable<(IObjectSpace objectSpace,EventArgs e)> Commited(this IObservable<IObjectSpace> source) {
            return source.SelectMany(item => {
                return Observable
                    .FromEventPattern<EventHandler, EventArgs>(h => item.Committed += h, h => item.Committed -= h)
                    .TransformPattern<EventArgs, IObjectSpace>();
            });
        }

        public static IObservable<(IObjectSpace objectSpace,CancelEventArgs e)> Commiting(this IObservable<IObjectSpace> source) {
            return source.SelectMany(item => {
                return Observable
                    .FromEventPattern<EventHandler<CancelEventArgs>, CancelEventArgs>(h => item.Committing += h, h => item.Committing -= h)
                    .TransformPattern<CancelEventArgs, IObjectSpace>()
                    .TraceRX();
            });
        }

        public static IObservable<(IObjectSpace objectSpace,EventArgs e)> WhenCommited(this IObjectSpace item) {
            return Observable.Return(item).Commited();
        }

        public static IObservable<(IObjectSpace objectSpace, CancelEventArgs e)> WhenCommiting(this IObjectSpace item){
            return Observable.Return(item).Commiting();
        }
        
        public static IObservable<(IObjectSpace objectSpace,ObjectsManipulatingEventArgs e)> ObjectDeleted(this IObservable<IObjectSpace> source) {
            return source.SelectMany(item => item.WhenObjectDeleted());
        }

        public static IObservable<(IObjectSpace objectSpace,ObjectsManipulatingEventArgs e)> WhenObjectDeleted(this IObjectSpace item) {
            return Observable.FromEventPattern<EventHandler<ObjectsManipulatingEventArgs>, ObjectsManipulatingEventArgs>(h => item.ObjectDeleted += h, h => item.ObjectDeleted -= h)
                .TransformPattern<ObjectsManipulatingEventArgs, IObjectSpace>();
        }
        
        public static IObservable<(IObjectSpace objectSpace,ObjectChangedEventArgs e)> ObjectChanged(this IObservable<IObjectSpace> source) {
            return source.SelectMany(item => item.WhenObjectChanged());
        }

        public static IObservable<(IObjectSpace objectSpace,ObjectChangedEventArgs e)> WhenObjectChanged(this IObjectSpace item) {
            return Observable.FromEventPattern<EventHandler<ObjectChangedEventArgs>, ObjectChangedEventArgs>(h => item.ObjectChanged += h, h => item.ObjectChanged -= h)
                .TransformPattern<ObjectChangedEventArgs, IObjectSpace>();
        }
        
        public static IObservable<Unit> Disposed(this IObservable<IObjectSpace> source){
            return source
                .SelectMany(item => Observable.FromEventPattern<EventHandler,EventArgs>(h => item.Disposed += h, h => item.Disposed -= h))
                .ToUnit();
        }

        public static IObservable<Unit> WhenDisposed(this IObjectSpace source) {
            return Observable.Return(source).Disposed();
        }

        public static IObservable<IObjectSpace> WhenModifyChanged(this IObjectSpace source){
            return source.AsObservable().ModifyChanged();
        }

        public static IObservable<IObjectSpace> ModifyChanged(this IObservable<IObjectSpace> source) {
            return source
                .SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(h => item.ModifiedChanged += h, h => item.ModifiedChanged -= h)
                    .Select(pattern => (IObjectSpace) pattern.Sender)
                );
        }

    }
}