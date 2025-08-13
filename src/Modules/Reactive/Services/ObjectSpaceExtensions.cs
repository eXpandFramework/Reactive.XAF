using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static partial class ObjectSpaceExtensions {
        private static readonly Subject<(IObjectSpace objectSpace,object instance)> ObjectsSubject = new();

        public static IObservable<TObject> WhenNewObjectCreated<TObjectSpace, TObject>(
            this IObservable<TObjectSpace> source) where TObjectSpace : class,IObjectSpace where TObject : IObjectSpaceLink 
            => source.SelectMany(objectSpace => objectSpace.WhenNewObjectCreated<TObject>()).PushStackFrame();

        public static IObservable<T> RefreshObjectSpace<T>(this IObservable<T> source,Func<IObjectSpace> objectSpaceSelector) 
            => source.Select(arg => (arg,space:objectSpaceSelector())).TakeUntil(t => t.space.IsDisposed)
                .If(t => !t.space.IsModified,t => t.arg.Observe().Do(_ => t.space.Refresh()),t =>
                    t.space.WhenCommitted()
                        .TakeUntil(t.space.WhenRollingBack().MergeToUnit(t.space.WhenDisposed())).Take(1)
                        .TakeUntil(t.space.WhenDisposed())
                        .Do(space => space.Refresh()).To(t.arg))
                .CompleteOnError().PushStackFrame();

        public static IObservable<T> WhenObjects<T>(this NonPersistentObjectSpace objectSpace,Func<(NonPersistentObjectSpace objectSpace, ObjectsGettingEventArgs e), IObservable<T>> source,Type objectType=null) where T:class{
            objectType ??= typeof(T);
            objectSpace.AutoSetModifiedOnObjectChange = true;
            objectSpace.NonPersistentChangesEnabled = true;
            return objectSpace.WhenObjectsGetting()
                    .Where(t => objectType.IsAssignableFrom(t.e.ObjectType))
                .SelectMany(t => {
                    var objects = new DynamicCollection(objectSpace, t.e.ObjectType);
                    t.e.Objects = objects;
                    return objects.WhenFetchObjects()
                        .TakeWhile(_ => !objectSpace.IsDisposed)
                        .SelectMany(e => source(t)
                            .ObserveOn(Scheduler.CurrentThread)
                            .Where(buffer => objectSpace.IsObjectFitForCriteria(e.Criteria,buffer))
                            .TakeWhile(_ => !objectSpace.IsDisposed)
                            .Take(e.TopReturnedObjects,true)
                            .ObserveOn(Scheduler.CurrentThread)
                            .BufferUntilCompleted()
                            .Do(items => objects.AddObjects(items))
                            .SelectMany()
                            .TakeWhile(_ => !objectSpace.IsDisposed)
                            .Do(item => {
                                objectSpace.AcceptObject(item);
                                if (objectType.IsInstanceOfType(item)) {
                                    ObjectsSubject.OnNext((objectSpace, item));
                                }
                            })
                            .Finally(() => {
                                objects.CallMethod("RaiseLoaded");
                                objects.CallMethod("RaiseListChangedEvent", new ListChangedEventArgs((ListChangedType) (-10000), 0));
                            })).IgnoreElements().Merge(ObjectsSubject.Where(t2 => t2.objectSpace==objectSpace).Select(t2 => t2.instance).Cast<T>()
                        );
                }).PushStackFrame();
        }

        public static IObservable<Unit> WhenCommitingObjects(this NonPersistentObjectSpace objectSpace,Func<object,IObservable<object>> sourceSelector)
            => objectSpace.WhenCommiting()
                .SelectMany(_ => objectSpace.ModifiedObjects.Cast<object>().ToObservable(Transform.ImmediateScheduler)
                    .SelectMany(sourceSelector)
                    .ToUnit().IgnoreElements()
                    .Merge(objectSpace.WhenModifyChanged().Where(space => !space.IsModified).Take(1).Do(space => space.SetIsModified(true)).ToUnit().IgnoreElements())
                    .Concat(Observable.Return(Unit.Default).Do(_ => objectSpace.SetIsModified(false))))
                .ToUnit().PushStackFrame();

        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenModifiedObjectsDetailed<T>(
            this IObjectSpace objectSpace, bool emitAfterCommit) 
            => objectSpace.WhenCommitingDetailed<T>(ObjectModification.All, emitAfterCommit).PushStackFrame();

        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenModifiedObjectsDetailed<T>(this IObjectSpace objectSpace,
            ObjectModification objectModification,bool emitAfterCommit) 
            => objectSpace.WhenCommitingDetailed<T>( emitAfterCommit,objectModification).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenModifiedObjectsDetailed<T>(this IObjectSpace objectSpace,
            ObjectModification objectModification ) 
            => objectSpace.WhenCommitingDetailed<T>(objectModification, false).PushStackFrame();

        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenCommitingDetailed<T>(
                this IObjectSpace objectSpace, bool emitAfterCommit, ObjectModification objectModification,Func<T,bool> criteria=null) 
            => objectSpace.WhenCommiting().SelectMany(_ => {
                    var modifiedObjects = objectSpace.ModifiedObjects<T>(objectModification).Where(t => criteria==null|| criteria.Invoke(t.instance)).ToArray();
                    return modifiedObjects.Any() ? emitAfterCommit ? objectSpace.WhenCommitted().Take(1).Select(space => (space, modifiedObjects))
                            : (objectSpace, modifiedObjects).Observe() : Observable.Empty<(IObjectSpace, (T instance, ObjectModification modification)[])>();
                })
                .TraceRX(_ => typeof(T).Name).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenCommitingDetailed(
                this IObjectSpace objectSpace,Type objectType, bool emitAfterCommit, ObjectModification objectModification,Func<object,bool> criteria=null) 
            => objectSpace.WhenCommiting()
                .SelectMany(_ => {
                    var modifiedObjects = objectSpace.ModifiedObjects(objectType, objectModification)
                        .Where(t => criteria==null|| criteria.Invoke(t.instance)).ToArray();
                    return modifiedObjects.Any() ? emitAfterCommit ? objectSpace.WhenCommitted().Take(1).Select(space => (space, modifiedObjects))
                        : (objectSpace, modifiedObjects).Observe() : Observable.Empty<(IObjectSpace, (object instance, ObjectModification modification)[])>();
                }).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenCommitingDetailed(
                this IObjectSpace objectSpace,Type objectType, bool emitAfterCommit, ObjectModification objectModification,string[] modifiedProperties,Func<object,bool> criteria=null) 
            => objectSpace.WhenModifiedObjects(objectType,modifiedProperties).Take(1)
                .TakeUntil(objectSpace.WhenDisposed())
                .SelectMany(_ => objectSpace.WhenCommitingDetailed( objectType, emitAfterCommit, objectModification, criteria)).PushStackFrame();

        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)>
            WhenCommitingDetailed<T>(this IObjectSpace objectSpace, ObjectModification objectModification, bool emitAfterCommit,Func<T,bool> criteria=null) 
            => objectSpace.WhenCommitingDetailed(emitAfterCommit, objectModification,criteria).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)>
            WhenCommitingDetailed(this IObjectSpace objectSpace,Type objectType, ObjectModification objectModification, bool emitAfterCommit,Func<object,bool> criteria=null) 
            => objectSpace.WhenCommitingDetailed(objectType, emitAfterCommit, objectModification,criteria).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)>
            WhenCommitingDetailed(this IObjectSpace objectSpace,Type objectType, ObjectModification objectModification, bool emitAfterCommit,string[] modifiedProperties,Func<object,bool> criteria=null) 
            => objectSpace.WhenCommitingDetailed(objectType, emitAfterCommit, objectModification,modifiedProperties,criteria).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)>
            WhenCommittedDetailed<T>(this IObjectSpace objectSpace, ObjectModification objectModification) 
            => objectSpace.ParentObjectSpace().WhenCommitingDetailed<T>(true, objectModification).PushStackFrame();

        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)>
            WhenCommittedDetailed<T>(this IObjectSpace objectSpace, ObjectModification objectModification, Func<T, bool> criteria ) where T : class
            => objectSpace.WhenCommitingDetailed(true, objectModification, criteria, []).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)>
            WhenCommittedDetailed<T>(this IObjectSpace objectSpace, ObjectModification objectModification,string[] modifiedProperties,
                Func<T, bool> criteria = null) where T : class
            => objectSpace.WhenCommitingDetailed(true, objectModification, criteria, modifiedProperties).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)>
            WhenCommittedDetailed(this IObjectSpace objectSpace, Type objectType, ObjectModification objectModification,
                string[] modifiedProperties,Func<object, bool> criteria = null) 
            => (modifiedProperties.Any()?objectSpace.WhenModifiedObjects(objectType,modifiedProperties).Take(1)
                    .SelectMany(_ => objectSpace.WhenCommitingDetailed(objectType,objectModification, true,criteria)):
                objectSpace.WhenCommitingDetailed(objectType,objectModification, true,criteria)).PushStackFrame();

        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenCommitingDetailed<T>(
            this IObjectSpace objectSpace, bool emitAfterCommit, ObjectModification objectModification, Func<T, bool> criteria,string[] modifiedProperties) where T:class 
            => (!modifiedProperties.Any() ? objectSpace.WhenCommitingDetailed(objectModification, emitAfterCommit, criteria)
                : objectSpace.WhenModifiedObjects(typeof(T), modifiedProperties).Cast<T>().Where(criteria??(_ =>true) )
                    .Buffer(objectSpace.WhenCommitingDetailed(false, objectModification, criteria)).WhenNotEmpty()
                    .TakeUntil(objectSpace.WhenDisposed())
                    .SelectMany(modifiedObjects => {
                        var details = objectSpace.ModifiedObjects(objectModification, modifiedObjects).ToArray();
                        return emitAfterCommit ? objectSpace.WhenCommitted().Take(1)
                            .Select(_ => (objectSpace, details)) : (objectSpace, details).Observe();
                    }).Where(t => t.details.Any())).PushStackFrame();

        public static
            IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)>
            WhenModifiedObjectsDetailed<T>(this IObjectSpace objectSpace) where T : class 
            => objectSpace.WhenCommitingDetailed<T>(ObjectModification.All, false).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommiting<T>(this IObjectSpace objectSpace, 
            ObjectModification objectModification = ObjectModification.All,bool emitAfterCommit = false) => objectSpace.WhenCommitingDetailed<T>(objectModification, emitAfterCommit)
                .Select(t => (t.objectSpace,t.details.Select(t1 => t1.instance))).PushStackFrame();

        public static IObservable<(IObjectSpace objectSpace, T[] objects)> WhenDeletedObjects<T>(this IObjectSpace objectSpace,bool emitAfterCommit=false) => (emitAfterCommit ? objectSpace.WhenCommiting<T>(ObjectModification.Deleted, true)
                    .Select(t => (t.objectSpace, t.objects.Select(t1 => t1).ToArray())).Finally(() => { })
                : objectSpace.WhenObjectDeleted()
                    .Select(pattern => (pattern.objectSpace, pattern.e.Objects.OfType<T>().ToArray()))
                    .TakeUntil(objectSpace.WhenDisposed())).PushStackFrame();

        public static IObservable<T> ModifiedExistingObject<T>(this XafApplication application,
            Func<(IObjectSpace objectSpace,ObjectChangedEventArgs e),bool> filter = null)
            => application.AllModifiedObjects<T>(t => filter?.Invoke(t)??!t.objectSpace.IsNewObject(t.e.Object)).PushStackFrame();

        public static IObservable<T> ModifiedNewObject<T>(this XafApplication application,
            Func<(IObjectSpace objectSpace,ObjectChangedEventArgs e),bool> filter = null)
            => application.AllModifiedObjects<T>(t => filter?.Invoke(t)??t.objectSpace.IsNewObject(t.e.Object)).PushStackFrame();

        public static IObservable<(IObjectSpace objectSpace, T[] objects)> DeletedObjects<T>(this XafApplication application) where T : class 
            => application.WhenObjectSpaceCreated().SelectMany(objectSpace => objectSpace.WhenDeletedObjects<T>()).PushStackFrame();

        public static IObservable<T> AllModifiedObjects<T>(this XafApplication application,Func<(IObjectSpace objectSpace,ObjectChangedEventArgs e),bool> filter=null ) 
            => application.WhenObjectSpaceCreated()
                .SelectMany(objectSpace => objectSpace.WhenObjectChanged()
                    .Where(tuple => filter == null || filter(tuple))
                    .SelectMany(tuple => tuple.objectSpace.ModifiedObjects.OfType<T>())).PushStackFrame();

        public static IObservable<T> CommitAndValidate<T>(this IEnumerable<T> source, IObjectSpace objectSpace = null) where T : IObjectSpaceLink 
            => source.Commit(objectSpace, true).PushStackFrame();

        public static IObservable<T> Commit<T>(this IEnumerable<T> source,IObjectSpace objectSpace=null,bool validate=false,bool refresh=false) where T:IObjectSpaceLink {
            var links = source as T[] ?? source.ToArray();
            return (links.Length == 0 ? Observable.Empty<T>() : links.Finally(() => {
                var space = (objectSpace ?? links.First().ObjectSpace);
                space.CommitChanges(validate);
                if (refresh) {
                    space.Refresh();
                }
            }).ToNowObservable()).PushStackFrame();
        }
        public static IObservable<T> Commit<T>(this IEnumerable<T> source,IObjectSpaceLink objectSpace) where T:IObjectSpaceLink {
            var links = source as T[] ?? source.ToArray();
            return links.Finally(objectSpace.CommitChanges).ToNowObservable().PushStackFrame();
        }

        public static IObservable<T> Commit<T>(this T link) where T:IObjectSpaceLink
            => Observable.If(() => link!=null,link.Defer(() => link.ObjectSpace.CommitChangesAsync().ToObservable().To(link))).PushStackFrame();
        
        public static IObservable<T> Commit<T>(this IObservable<T> source,RXAction action=RXAction.OnCompleted) where T:IObjectSpaceLink {
            if (action == RXAction.OnCompleted) {
                return source.BufferUntilCompleted(true).SelectMany().Take(1).Do(link => link.CommitChanges()).PushStackFrame();    
            }

            if (action == RXAction.OnNext) {
                return source.Do(link => link.CommitChanges()).PushStackFrame();
            }

            throw new NotImplementedException(action.ToString());
        }
        
        public static IObservable<T> Reload<T>(this IObservable<T> source,XafApplication application) where T:IObjectSpaceLink 
            => source.Select(item => item.Reload(application)).PushStackFrame();
        public static IObservable<T> Reload<T>(this IObservable<IList<T>> source,XafApplication application) where T:IObjectSpaceLink 
            => source.WhenNotEmpty().SelectMany(items => {
                var firstItem = items.First().Reload(application);
                return items.Select(link => firstItem.ObjectSpace.GetObject(link)).StartWith(firstItem);
            }).PushStackFrame();

        public static IObservable<(T theObject, IObjectSpace objectSpace)> FindObject<T>(this XafApplication application,Func<IQueryable<T>,IQueryable<T>> query=null) 
            => Observable.Using(() => application.CreateObjectSpace(typeof(T)), space => space.ExistingObject(query).Select(arg => (arg,space))).PushStackFrame();

        public static IObservable<T> WhenObjectCommitted<T>(this IObservable<T> source) where T:IObjectSpaceLink 
            => source.SelectMany(link => link.ObjectSpace.WhenCommitted().Take(1).Select(_ => link)).PushStackFrame();

        public static IObservable<T> WhenNewObjectCommiting<T>(this IObjectSpace objectSpace) where T : class
            => objectSpace.WhenCommiting()
                .SelectMany(_ => objectSpace.ModifiedObjects.OfType<T>().Where(objectSpace.IsNewObject)).PushStackFrame();

        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommiting<T>(
            this XafApplication application, ObjectModification objectModification = ObjectModification.All) where T : class 
            => application.WhenObjectSpaceCreated().SelectMany(objectSpace => objectSpace.WhenCommiting<T>(objectModification)).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenProviderCommiting<T>(
            this XafApplication application, ObjectModification objectModification = ObjectModification.All) where T : class 
            => application.WhenProviderObjectSpaceCreated().SelectMany(objectSpace => objectSpace.WhenCommiting<T>(objectModification)).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenProviderCommitted<T>(
            this XafApplication application, ObjectModification objectModification = ObjectModification.All,bool emitUpdatingObjectSpace=false)
            => application.WhenProviderObjectSpaceCreated(emitUpdatingObjectSpace).WhenCommitted<T>(objectModification).PushStackFrame();

        public static IObservable<(IObjectSpace objectSpace, IEnumerable<object> objects)> WhenProviderCommitted(
            this XafApplication application,Type objectType, ObjectModification objectModification = ObjectModification.All)
            => application.WhenProviderCommitted(objectType,[],objectModification).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<object> objects)> WhenProviderCommitted(
            this XafApplication application,Type objectType,string[] modifiedProperties, ObjectModification objectModification = ObjectModification.All)
            => application.WhenProviderObjectSpaceCreated().WhenCommitted(objectType,modifiedProperties,objectModification).PushStackFrame();

        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenProviderCommitting<T>(
            this XafApplication application, ObjectModification objectModification = ObjectModification.All) where T : class 
            => application.WhenProviderObjectSpaceCreated().SelectMany(space => space.WhenCommiting<T>(objectModification)).PushStackFrame();

        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommitted<T>(
            this IObservable<IObjectSpace> source, ObjectModification objectModification = ObjectModification.All) 
            => source.SelectMany(objectSpace => objectSpace.WhenCommitted<T>(objectModification)).PushStackFrame();

        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommitted<T>(
            this IObjectSpace objectSpace) 
            => objectSpace.WhenCommitted<T>(ObjectModification.All).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommitted<T>(
            this IObjectSpace objectSpace, ObjectModification objectModification) 
            => objectSpace.WhenCommitingDetailed<T>(objectModification, true)
                .Select(t => (t.objectSpace,t.details.Select(t1 => t1.instance))).PushStackFrame();

        public static IObservable<(IObjectSpace objectSpace, IEnumerable<object> objects)> WhenCommitted(
            this IObservable<IObjectSpace> source,Type objectType, ObjectModification objectModification = ObjectModification.All) 
            => source.WhenCommitted(objectType,[],objectModification).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<object> objects)> WhenCommitted(
            this IObservable<IObjectSpace> source,Type objectType,string[] modifiedProperties , ObjectModification objectModification = ObjectModification.All) 
            => source.SelectMany(objectSpace => objectSpace.WhenCommitingDetailed(objectType, objectModification, true,modifiedProperties)
                .Select(t => (t.objectSpace,t.details.Select(t1 => t1.instance)))).PushStackFrame();

        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommitted<T>(
            this XafApplication application, ObjectModification objectModification,params T[] objects) 
            => application.WhenObjectSpaceCreated().WhenCommitted( objects,objectModification).PushStackFrame();
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommitted<T>(
            this XafApplication application, params T[] objects) 
            => application.WhenObjectSpaceCreated().WhenCommitted(objects).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenProviderCommitted<T>(
            this XafApplication application, ObjectModification objectModification,params T[] objects) 
            => application.WhenProviderObjectSpaceCreated().WhenCommitted( objects,objectModification).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenProviderCommitted<T>(
            this XafApplication application, params T[] objects) 
            => application.WhenProviderObjectSpaceCreated().WhenCommitted(objects).PushStackFrame();
        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenExistingObjectCommiting<T>(this XafApplication application) where T : class 
            => application.WhenObjectSpaceCreated().SelectMany(objectSpace => objectSpace.WhenCommiting<T>(ObjectModification.Updated)).PushStackFrame();

        public static IObservable<TLink> ReloadNotifyObject<TLink>(this TLink link) where  TLink:IObjectSpaceLink 
            => link.Defer(() => {
                    if (link.GetType().Implements(XPInvalidateableObjectType)) {

                        if (!(bool)link.GetPropertyValue("IsInvalidated") && !(bool)link.GetPropertyValue("Session").GetPropertyValue("IsObjectsLoading"))
                            return link.ReloadObject().Observe();
                        return link.GetPropertyValue("Session").ProcessEvent("ObjectLoaded").Take(1)
                            .Do(_ => link.ReloadObject()).To(link);
                    }
                    throw new NotImplementedException();
                })
                .Do(spaceLink => ReloadObjectSubject.OnNext((spaceLink.ObjectSpace, spaceLink))).PushStackFrame();

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
                    : Observable.Empty<Unit>()).PushStackFrame();


        
    }
}