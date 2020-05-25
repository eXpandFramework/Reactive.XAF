using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using JetBrains.Annotations;
using Xpand.Extensions.Linq;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ObjectSpaceExtensions{
        [PublicAPI]
        public static IObservable<T> WhenModifiedObjects<T>(this IObjectSpace objectSpace,Expression<Func<T,object>> memberSelector){
            var memberName = ((MemberExpression) memberSelector.Body).Member.Name;
            return objectSpace.WhenObjectChanged().Where(_ => _.e.Object is T && _.e.MemberInfo.Name == memberName)
                .Select(_ => _.e.Object).Cast<T>();
        }

        [PublicAPI]
        public static IObservable<(IObjectSpace objectSpace, object[] objects)> WhenModifiedObjects(this IObjectSpace objectSpace, bool emitAfterCommit,
            params Type[] objectTypes){
            return objectSpace.WhenModifiedObjects(ObjectModification.All, emitAfterCommit, objectTypes);
        }

        [PublicAPI]
        public static IObservable<(IObjectSpace objectSpace, object[] objects)> WhenModifiedObjects(this IObjectSpace objectSpace,
            ObjectModification objectModification = ObjectModification.All, params Type[] objectTypes){
            return objectSpace.WhenModifiedObjects(objectModification, false, objectTypes);
        }

        public static IObservable<(IObjectSpace objectSpace, object[] objects)> WhenModifiedObjects(this IObjectSpace objectSpace, ObjectModification objectModification, 
            bool emitAfterCommit , params Type[] objectTypes){
            if (!objectTypes.Any()){
                objectTypes = objectTypes.Add(typeof(object));
            }
            return objectSpace.WhenCommiting().SelectMany(_ => {
                var modifiedObjects = objectSpace.ModifiedObjects(objectModification).Where(o => objectTypes.Any(type => type.IsInstanceOfType(o))).ToArray();
                if (modifiedObjects.Any()){
                    return emitAfterCommit ? objectSpace.WhenCommited().FirstAsync().Select(pattern => (pattern.objectSpace, modifiedObjects))
                        : (objectSpace, modifiedObjects).ReturnObservable();    
                }
                return Observable.Empty<(IObjectSpace objectSpace, object[] objects)>();
            });
        }

        [PublicAPI]
        public static IObservable<(IObjectSpace objectSpace,object[] objects)> WhenModifiedObjects(this IObjectSpace objectSpace,params Type[] objectTypes){
            return objectSpace.WhenModifiedObjects(ObjectModification.All, false, objectTypes);
        }

        [PublicAPI]
        public static IObservable<(IObjectSpace objectSpace, object[] objects)> WhenModifiedObjects<T>(this IObjectSpace objectSpace,bool emitAfterCommit=false,ObjectModification objectModification=ObjectModification.All ){
            return objectSpace.WhenModifiedObjects(  objectModification,emitAfterCommit,typeof(T));
        }
        static bool IsUpdated<T>(this IObjectSpace objectSpace, T t){
            return !objectSpace.IsNewObject(t)&&!objectSpace.IsDeletedObject(t);
        }

        [PublicAPI]
        public static IObservable<(IObjectSpace objectSpace, object[] objects)> WhenDeletedObjects<T>(this IObjectSpace objectSpace,bool emitAfterCommit=false){
            return emitAfterCommit ? objectSpace.WhenModifiedObjects( ObjectModification.Deleted,true,typeof(T))
                : objectSpace.WhenObjectDeleted()
                    .Select(pattern => (pattern.objectSpace,pattern.e.Objects.Cast<object>().ToArray()))
                    .TakeUntil(objectSpace.WhenDisposed());
        }


        [PublicAPI]
        public static IEnumerable<object> ModifiedObjects(this IObjectSpace objectSpace, ObjectModification objectModification){
            return objectSpace.ModifiedObjects.Cast<object>().Where(_ => {
                if (objectModification == ObjectModification.Deleted){
                    return objectSpace.IsDeletedObject(_);
                }
                if (objectModification == ObjectModification.New){
                    return objectSpace.IsNewObject(_);
                }
                if (objectModification == ObjectModification.Updated){
                    return objectSpace.IsUpdated(_);
                }
                if (objectModification == ObjectModification.NewOrDeleted){
                    return objectSpace.IsNewObject(_) || objectSpace.IsDeletedObject(_);
                }
                if (objectModification == ObjectModification.NewOrUpdated){
                    return objectSpace.IsNewObject(_) || objectSpace.IsUpdated(_);
                }
                if (objectModification == ObjectModification.DeletedOrUpdated){
                    return objectSpace.IsUpdated(_) || objectSpace.IsDeletedObject(_);
                }

                return true;

            });

        }

        [PublicAPI]
        public static IEnumerable<T> ModifiedObjects<T>(this IObjectSpace objectSpace, ObjectModification objectModification){
            return objectSpace.ModifiedObjects(objectModification).Cast<T>();
        }
        [PublicAPI]
        public static IObservable<T> ModifiedExistingObject<T>(this XafApplication application,
            Func<(IObjectSpace objectSpace,ObjectChangedEventArgs e),bool> filter = null){
            filter ??= (_ => true);
            return application.AllModifiedObjects<T>(_ => filter(_) && !_.objectSpace.IsNewObject(_.e.Object));
        }
        [PublicAPI]
        public static IObservable<T> ModifiedNewObject<T>(this XafApplication application,
            Func<(IObjectSpace objectSpace,ObjectChangedEventArgs e),bool> filter = null){
            filter ??= (_ => true);
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
            return provider == null ? Observable.Empty<IObjectSpace>() : Observable.Using(provider.CreateObjectSpace, space => space.ReturnObservable());
        }
        [PublicAPI]
        public static IObservable<IObjectSpace> ToObjectSpace(this XafApplication application){
            return Observable.Using(application.CreateObjectSpace, space => space.ReturnObservable());
        }

        [PublicAPI]
        public static T GetObject<T>(this XafApplication application,T value){
            return application.CreateObjectSpace().GetObject(value);
        }

        public static IObservable<T> ExistingObject<T>(this XafApplication application,Func<IQueryable<T>,IQueryable<T>> query=null){
            return Observable.Using(application.CreateObjectSpace, space => space.ExistingObject(query));
        }

        [PublicAPI]
        public static IObservable<T> AnyObject<T>(this XafApplication application){
            return application.NewObject<T>().Merge(application.ExistingObject<T>()).DistinctUntilChanged()
                .TraceRX();
        }

        [PublicAPI]
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

        [PublicAPI]
        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectsGettingEventArgs e)> ObjectsGetting(this IObservable<NonPersistentObjectSpace> source) {
            return source.SelectMany(item => item.WhenObjectsGetting());
        }
        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectsGettingEventArgs e)> WhenObjectsGetting(this NonPersistentObjectSpace item) {
            return Observable.FromEventPattern<EventHandler<ObjectsGettingEventArgs>, ObjectsGettingEventArgs>(h => item.ObjectsGetting += h, h => item.ObjectsGetting -= h,ImmediateScheduler.Instance)
                .TransformPattern<ObjectsGettingEventArgs, NonPersistentObjectSpace>();
        }
        
        public static IObservable<(IObjectSpace objectSpace,EventArgs e)> Commited(this IObservable<IObjectSpace> source) {
            return source.SelectMany(item => {
                return Observable
                    .FromEventPattern<EventHandler, EventArgs>(h => item.Committed += h, h => item.Committed -= h,ImmediateScheduler.Instance)
                    .TransformPattern<EventArgs, IObjectSpace>();
            });
        }

        public static IObservable<(IObjectSpace objectSpace,CancelEventArgs e)> Commiting(this IObservable<IObjectSpace> source) {
            return source.SelectMany(item => {
                return Observable
                    .FromEventPattern<EventHandler<CancelEventArgs>, CancelEventArgs>(h => item.Committing += h, h => item.Committing -= h,ImmediateScheduler.Instance)
                    .TransformPattern<CancelEventArgs, IObjectSpace>()
                    .TraceRX();
            });
        }

        public static IObservable<(IObjectSpace objectSpace,EventArgs e)> WhenCommited(this IObjectSpace item) {
            return item.ReturnObservable().Commited();
        }

        public static IObservable<(IObjectSpace objectSpace, CancelEventArgs e)> WhenCommiting(this IObjectSpace item){
            return item.ReturnObservable().Commiting();
        }
        
        [PublicAPI]
        public static IObservable<(IObjectSpace objectSpace,ObjectsManipulatingEventArgs e)> ObjectDeleted(this IObservable<IObjectSpace> source) {
            return source.SelectMany(item => item.WhenObjectDeleted());
        }

        public static IObservable<(IObjectSpace objectSpace,ObjectsManipulatingEventArgs e)> WhenObjectDeleted(this IObjectSpace item) {
            return Observable.FromEventPattern<EventHandler<ObjectsManipulatingEventArgs>, ObjectsManipulatingEventArgs>(h => item.ObjectDeleted += h, h => item.ObjectDeleted -= h,ImmediateScheduler.Instance)
                .TransformPattern<ObjectsManipulatingEventArgs, IObjectSpace>();
        }
        
        [PublicAPI]
        public static IObservable<(IObjectSpace objectSpace,ObjectChangedEventArgs e)> ObjectChanged(this IObservable<IObjectSpace> source) {
            return source.SelectMany(item => item.WhenObjectChanged());
        }

        public static IObservable<(IObjectSpace objectSpace,ObjectChangedEventArgs e)> WhenObjectChanged(this IObjectSpace item) {
            return Observable.FromEventPattern<EventHandler<ObjectChangedEventArgs>, ObjectChangedEventArgs>(h => item.ObjectChanged += h, h => item.ObjectChanged -= h,ImmediateScheduler.Instance)
                .TransformPattern<ObjectChangedEventArgs, IObjectSpace>();
        }
        
        public static IObservable<Unit> Disposed(this IObservable<IObjectSpace> source){
            return source
                .SelectMany(item => Observable.FromEventPattern<EventHandler,EventArgs>(h => item.Disposed += h, h => item.Disposed -= h,ImmediateScheduler.Instance))
                .ToUnit();
        }

        public static IObservable<Unit> WhenDisposed(this IObjectSpace source) {
            return source.ReturnObservable().Disposed();
        }

        [PublicAPI]
        public static IObservable<IObjectSpace> WhenModifyChanged(this IObjectSpace source){
            return source.ReturnObservable().ModifyChanged();
        }

        public static IObservable<IObjectSpace> ModifyChanged(this IObservable<IObjectSpace> source) {
            return source
                .SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(h => item.ModifiedChanged += h, h => item.ModifiedChanged -= h,ImmediateScheduler.Instance)
                    .Select(pattern => (IObjectSpace) pattern.Sender)
                );
        }

    }
    public enum ObjectModification{
        All,
        New,
        Deleted,
        Updated,
        NewOrDeleted,
        NewOrUpdated,
        DeletedOrUpdated
    }

}