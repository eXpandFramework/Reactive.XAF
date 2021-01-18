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
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;
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
        public static IObservable<(IObjectSpace objectSpace, object[] objects)> WhenModifiedObjects(
            this IObjectSpace objectSpace, bool emitAfterCommit, params Type[] objectTypes) 
            => objectSpace.WhenCommiting(ObjectModification.All, emitAfterCommit, objectTypes);

        [PublicAPI]
        public static IObservable<(IObjectSpace objectSpace, object[] objects)> WhenModifiedObjects(this IObjectSpace objectSpace,
            bool emitAfterCommit,ObjectModification objectModification = ObjectModification.All, params Type[] objectTypes) 
            => objectSpace.WhenCommiting(objectModification, emitAfterCommit, objectTypes);
        
        public static IObservable<(IObjectSpace objectSpace, object[] objects)> WhenModifiedObjects(this IObjectSpace objectSpace,
            ObjectModification objectModification = ObjectModification.All, params Type[] objectTypes) 
            => objectSpace.WhenCommiting(objectModification, false, objectTypes);

        public static IObservable<(IObjectSpace objectSpace, object[] objects)> WhenCommiting(this IObjectSpace objectSpace, ObjectModification objectModification, 
            bool emitAfterCommit , params Type[] objectTypes){
            if (!objectTypes.Any()){
                objectTypes = objectTypes.Add(typeof(object));
            }
            return objectSpace.WhenCommiting().SelectMany(_ => {
                var modifiedObjects = objectSpace.ModifiedObjects(objectModification).Where(o => objectTypes.Any(type => type.IsInstanceOfType(o))).ToArray();
                return modifiedObjects.Any() ? emitAfterCommit ? objectSpace.WhenCommitted().FirstAsync().Select(space => (space, modifiedObjects))
                    : (objectSpace, modifiedObjects).ReturnObservable() : Observable.Empty<(IObjectSpace objectSpace, object[] objects)>();
            });
        }

        [PublicAPI]
        public static IObservable<(IObjectSpace objectSpace, object[] objects)> WhenModifiedObjects(this IObjectSpace objectSpace, params Type[] objectTypes) 
            => objectSpace.WhenCommiting(ObjectModification.All, false, objectTypes);

        [PublicAPI]
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommiting<T>(this IObjectSpace objectSpace, 
            ObjectModification objectModification = ObjectModification.All,bool emitAfterCommit = false) 
            => objectSpace.WhenCommiting(objectModification, emitAfterCommit, typeof(T)).Select(t => (t.objectSpace,t.objects.Cast<T>()));

        static bool IsUpdated<T>(this IObjectSpace objectSpace, T t) 
            => !objectSpace.IsNewObject(t)&&!objectSpace.IsDeletedObject(t);

        [PublicAPI]
        public static IObservable<(IObjectSpace objectSpace, object[] objects)> WhenDeletedObjects<T>(this IObjectSpace objectSpace,bool emitAfterCommit=false) 
            => emitAfterCommit ? objectSpace.WhenCommiting( ObjectModification.Deleted,true,typeof(T))
                : objectSpace.WhenObjectDeleted()
                    .Select(pattern => (pattern.objectSpace,pattern.e.Objects.Cast<object>().ToArray()))
                    .TakeUntil(objectSpace.WhenDisposed());


        [PublicAPI]
        public static IEnumerable<object> ModifiedObjects(this IObjectSpace objectSpace,
            ObjectModification objectModification)
            => objectSpace.ModifiedObjects.Cast<object>().Where(_ => {
                return objectModification switch{
                    ObjectModification.Deleted => objectSpace.IsDeletedObject(_),
                    ObjectModification.New => objectSpace.IsNewObject(_),
                    ObjectModification.Updated => objectSpace.IsUpdated(_),
                    ObjectModification.NewOrDeleted => objectSpace.IsNewObject(_) || objectSpace.IsDeletedObject(_),
                    ObjectModification.NewOrUpdated => objectSpace.IsNewObject(_) || objectSpace.IsUpdated(_),
                    ObjectModification.DeletedOrUpdated => objectSpace.IsUpdated(_) || objectSpace.IsDeletedObject(_),
                    _ => true
                };
            });

        [PublicAPI]
        public static IEnumerable<T> ModifiedObjects<T>(this IObjectSpace objectSpace, ObjectModification objectModification) 
            => objectSpace.ModifiedObjects(objectModification).Cast<T>();

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

        public static IObservable<(IObjectSpace objectSpace, object[] objects)> DeletedObjects<T>(this XafApplication application) 
            => application.WhenObjectSpaceCreated().SelectMany(t => t.e.ObjectSpace.WhenDeletedObjects<T>());

        public static IObservable<T> AllModifiedObjects<T>(this XafApplication application,Func<(IObjectSpace objectSpace,ObjectChangedEventArgs e),bool> filter=null ) 
            => application.WhenObjectSpaceCreated()
                .SelectMany(_ => _.e.ObjectSpace.WhenObjectChanged()
                    .Where(tuple => filter == null || filter(tuple))
                    .SelectMany(tuple => tuple.objectSpace.ModifiedObjects.OfType<T>()));

        [PublicAPI]
        public static IObservable<TResult> NewObjectSpace<TResult>(this XafApplication application,Func<IObjectSpace, IObservable<TResult>> factory) 
            => Observable.Using(application.CreateObjectSpace, factory);

        [PublicAPI]
        public static T GetObject<T>(this XafApplication application,T value) 
            => application.CreateObjectSpace().GetObject(value);

        public static IObservable<(T theObject, IObjectSpace objectSpace)> FindObject<T>(this XafApplication application,Func<IQueryable<T>,IQueryable<T>> query=null) 
            => Observable.Using(application.CreateObjectSpace, space => space.ExistingObject(query));


        [PublicAPI]
        public static IObservable<T> WhenObjectCommitted<T>(this IObservable<T> source) where T:IObjectSpaceLink 
            => source.SelectMany(_ => _.ObjectSpace.WhenCommitted().FirstAsync().Select(tuple => _));

        public static IObservable<T> WhenNewObjectCommiting<T>(this IObjectSpace objectSpace) 
            => objectSpace.WhenCommiting()
                .SelectMany(t => objectSpace.ModifiedObjects.OfType<T>().Where(r => t.objectSpace.IsNewObject(r)))
                .TraceRX();

        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommiting<T>(
            this XafApplication application, ObjectModification objectModification = ObjectModification.All)
            => application.WhenObjectSpaceCreated()
                .SelectMany(_ => _.e.ObjectSpace.WhenCommiting<T>(objectModification));
        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommitted<T>(
            this XafApplication application, ObjectModification objectModification = ObjectModification.All)
            => application.WhenObjectSpaceCreated()
                .SelectMany(_ => _.e.ObjectSpace.WhenModifiedObjects(true,objectModification,typeof(T)).Select(t => (t.objectSpace,t.objects.Cast<T>())));

        public static IObservable<T> Objects<T>(this IObservable<(IObjectSpace, IEnumerable<T> objects)> source)
            => source.SelectMany(t => t.objects);
        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenExistingObjectCommiting<T>(this XafApplication application) 
            => application.WhenObjectSpaceCreated().SelectMany(_ => _.e.ObjectSpace.WhenCommiting<T>(ObjectModification.Updated));

        public static IObservable<(T theObject, IObjectSpace objectSpace)> ExistingObject<T>(this IObjectSpace objectSpace,Func<IQueryable<T>,IQueryable<T>> query=null){
            var objectsQuery = objectSpace.GetObjectsQuery<T>();
            if (query != null){
                objectsQuery = objectsQuery.Concat(query(objectsQuery));
            }
            return objectsQuery.ToObservable().Pair(objectSpace).TraceRX();
        }

        [PublicAPI]
        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectsGettingEventArgs e)> ObjectsGetting(this IObservable<NonPersistentObjectSpace> source) 
            => source.SelectMany(item => item.WhenObjectsGetting());

        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectsGettingEventArgs e)> WhenObjectsGetting(this NonPersistentObjectSpace item) 
            => Observable.FromEventPattern<EventHandler<ObjectsGettingEventArgs>, ObjectsGettingEventArgs>(h => item.ObjectsGetting += h, h => item.ObjectsGetting -= h,ImmediateScheduler.Instance)
                .TransformPattern<ObjectsGettingEventArgs, NonPersistentObjectSpace>();

        public static IObservable<IObjectSpace> Committed(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => Observable
                .FromEventPattern<EventHandler, EventArgs>(h => item.Committed += h, h => item.Committed -= h,ImmediateScheduler.Instance)
                .TransformPattern<IObjectSpace>());

        public static IObservable<(IObjectSpace objectSpace,CancelEventArgs e)> Commiting(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => Observable
                .FromEventPattern<EventHandler<CancelEventArgs>, CancelEventArgs>(h => item.Committing += h, h => item.Committing -= h,ImmediateScheduler.Instance)
                .TransformPattern<CancelEventArgs, IObjectSpace>()
                .TraceRX());

        public static IObservable<IObjectSpace> WhenCommitted(this IObjectSpace item) 
            => item.ReturnObservable().Committed();

        public static IObservable<(IObjectSpace objectSpace, CancelEventArgs e)> WhenCommiting(this IObjectSpace item) 
            => item.ReturnObservable().Commiting();

        [PublicAPI]
        public static IObservable<(IObjectSpace objectSpace,ObjectsManipulatingEventArgs e)> ObjectDeleted(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => item.WhenObjectDeleted());

        public static IObservable<(IObjectSpace objectSpace,ObjectsManipulatingEventArgs e)> WhenObjectDeleted(this IObjectSpace item) 
            => Observable.FromEventPattern<EventHandler<ObjectsManipulatingEventArgs>, ObjectsManipulatingEventArgs>(h => item.ObjectDeleted += h, h => item.ObjectDeleted -= h,ImmediateScheduler.Instance)
                .TransformPattern<ObjectsManipulatingEventArgs, IObjectSpace>();

        [PublicAPI]
        public static IObservable<(IObjectSpace objectSpace,ObjectChangedEventArgs e)> ObjectChanged(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => item.WhenObjectChanged());

        public static IObservable<(IObjectSpace objectSpace,ObjectChangedEventArgs e)> WhenObjectChanged(this IObjectSpace item) 
            => Observable.FromEventPattern<EventHandler<ObjectChangedEventArgs>, ObjectChangedEventArgs>(h => item.ObjectChanged += h, h => item.ObjectChanged -= h,ImmediateScheduler.Instance)
                .TransformPattern<ObjectChangedEventArgs, IObjectSpace>();

        public static IObservable<Unit> Disposed(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => Observable.FromEventPattern<EventHandler,EventArgs>(h => item.Disposed += h, h => item.Disposed -= h,ImmediateScheduler.Instance))
                .ToUnit();

        public static IObservable<Unit> WhenDisposed(this IObjectSpace source)
            => source.ReturnObservable().Disposed();

        [PublicAPI]
        public static IObservable<IObjectSpace> WhenModifyChanged(this IObjectSpace source) 
            => source.ReturnObservable().WhenModifyChanged();

        public static IObservable<IObjectSpace> WhenModifyChanged(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(h => item.ModifiedChanged += h, h => item.ModifiedChanged -= h,ImmediateScheduler.Instance)
                .Select(pattern => (IObjectSpace) pattern.Sender)
            );
        
        public static IObservable<IObjectSpace> WhenReloaded(this IObjectSpace source) 
            => source.ReturnObservable().WhenReloaded();

        public static IObservable<IObjectSpace> WhenReloaded(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(h => item.Reloaded += h, h => item.Reloaded -= h,ImmediateScheduler.Instance)
                .Select(pattern => (IObjectSpace) pattern.Sender)
            );

        internal static IObservable<Unit> ShowPersistentObjectsInNonPersistentView(this XafApplication application)
            => application.WhenObjectViewCreating()
                .SelectMany(t => t.e.ObjectSpace is NonPersistentObjectSpace nonPersistentObjectSpace
                    ? t.application.Model.Views[t.e.ViewID].AsObjectView.ModelClass.TypeInfo.Members
                        .Where(info => info.MemberTypeInfo.IsPersistent)
                        .Where(info => nonPersistentObjectSpace.AdditionalObjectSpaces.All(space => !space.IsKnownType(info.MemberType)))
                        .GroupBy(info => t.application.ObjectSpaceProviders(info.MemberType))
                        .ToObservable(ImmediateScheduler.Instance)
                        .SelectMany(infos => {
                            var objectSpace = application.CreateObjectSpace(infos.First().MemberType);
                            nonPersistentObjectSpace.AdditionalObjectSpaces.Add(objectSpace);
                            return nonPersistentObjectSpace.WhenDisposed().Do(_ => objectSpace.Dispose()).ToUnit();
                        })
                    : Observable.Empty<Unit>());

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