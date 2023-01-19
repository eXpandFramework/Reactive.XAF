using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.Collections;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ObjectSpaceExtensions {
        private static readonly Subject<(IObjectSpace objectSpace,object instance)> ObjectsSubject = new();

        public static IObservable<TObject> WhenNewObjectCreated<TObjectSpace, TObject>(
            this IObservable<TObjectSpace> source) where TObjectSpace : class,IObjectSpace where TObject : IObjectSpaceLink 
            => source.SelectMany(objectSpace => objectSpace.WhenNewObjectCreated<TObject>());

        public static IObservable<T> Link<T>(this IObservable<T> source, IObjectSpace objectSpace) where T:class
            => source.Do(obj => {
                if (obj is IObjectSpaceLink link) {
                    link.ObjectSpace=objectSpace;
                }
            });

        public static IEnumerable<T> ShapeData<T>(this IObjectSpace objectSpace,Type objectType,CriteriaOperator criteria=null,IEnumerable<SortProperty> sorting=null,int topReturned=0,params T[] objects) where T:class{
            var filterEvaluator = objectSpace.GetExpressionEvaluator(objectType,criteria);
            var data = objects.Where(o => filterEvaluator.Fit(o));
            if (sorting!=null){
                foreach(var sortInfo in sorting) {
                    var sortingEvaluator = objectSpace.GetExpressionEvaluator(objectType, sortInfo.Property);
                    data = sortInfo.Direction == DevExpress.Xpo.DB.SortingDirection.Ascending ? data.OrderBy(o => sortingEvaluator.Evaluate(o)) : data.OrderByDescending(o => sortingEvaluator.Evaluate(o));
                }
            }
            if(topReturned > 0) {
                data = data.Take(topReturned);
            }

            return data;
        }

        public static IObservable<T> WhenObjects<T>(this NonPersistentObjectSpace objectSpace) => objectSpace.WhenObjects(typeof(T)).Cast<T>();
        public static IObservable<object> WhenObjects(this NonPersistentObjectSpace objectSpace,Type objectType=null) 
            => ObjectsSubject.Where(t => t.objectSpace==objectSpace)
                .Select(t => t.instance).Where(o =>objectType==null|| objectType.IsInstanceOfType(o));

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
                        .SelectMany(t1 => source(t)
                            .ObserveOn(Scheduler.CurrentThread)
                            .Where(buffer => objectSpace.IsObjectFitForCriteria(t1.e.Criteria,buffer))
                            .TakeWhile(_ => !objectSpace.IsDisposed)
                            .Take(t1.e.TopReturnedObjects,true)
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
                })
                ;
        }

        public static void AcceptObject(this NonPersistentObjectSpace objectSpace, object item) => objectSpace.CallMethod("AcceptObject", item);

        public static IObservable<Unit> WhenCommitingObjects(this NonPersistentObjectSpace objectSpace,Func<object,IObservable<object>> sourceSelector)
            => objectSpace.WhenCommiting()
                .SelectMany(t => t.objectSpace.ModifiedObjects.Cast<object>().ToObservable(Scheduler.Immediate)
                    .SelectMany(sourceSelector)
                    .ToUnit().IgnoreElements()
                    .Merge(t.objectSpace.WhenModifyChanged().FirstAsync(space => !space.IsModified).Do(space => space.SetIsModified(true)).ToUnit().IgnoreElements())
                    .Concat(Observable.Return(Unit.Default).Do(_ => t.objectSpace.SetIsModified(false))))
                .ToUnit();

        
        public static IObservable<T> Request<T>(this IObjectSpace objectSpace) 
            => objectSpace.Request(typeof(T)).Cast<T>();

        public static IObservable<T[]> RequestAll<T>(this IObjectSpace objectSpace) 
            => objectSpace.Request<T>().BufferUntilCompleted();
        
        public static IObservable<object[]> RequestAll(this IObjectSpace objectSpace,Type objectType) 
            => objectSpace.Request(objectType).BufferUntilCompleted();

        public static IObservable<object> Request(this IObjectSpace objectSpace,Type objectType) 
            => ((IBindingList) objectSpace.CreateCollection(objectType)).WhenObjects();

        public static IObservable<object> WhenObjects(this IBindingList bindingList,bool waitForTrigger=false) {
            var whenListChanged = bindingList.WhenListChanged().Publish().RefCount();
            var signalCompletion = whenListChanged.FirstAsync(t => t.e.ListChangedType == (ListChangedType) (-10000))
                .Select(t => t.sender);
            return waitForTrigger ? signalCompletion.SelectMany(list => list.Cast<object>()) : signalCompletion
                .Merge(bindingList.Cast<object>().TakeLast(1).ToObservable(Scheduler.Immediate).IgnoreElements().To(bindingList))
                .SelectMany(list => list.Cast<object>());
        }

        public static IObservable<T> WhenModifiedObjects<T>(this IObjectSpace objectSpace, params string[] properties) 
            => objectSpace.WhenModifiedObjects(typeof(T),properties).Cast<T>();

        public static IObservable<object> WhenModifiedObjects(this IObjectSpace objectSpace, Type objectType,
            params string[] properties)
            => Observable.Defer(() => objectSpace.WhenObjectChanged()
                .Where(t => objectType.IsInstanceOfType(t.e.Object) && properties.PropertiesMatch(t))
                .Select(_ => _.e.Object));
                // .RepeatWhen(observable => observable.SelectMany(_ => objectSpace.WhenModifyChanged().Where(space => !space.IsModified).Take(1)))
                // .TakeUntil(objectSpace.WhenDisposed());

        private static bool PropertiesMatch(this string[] properties, (IObjectSpace objectSpace, ObjectChangedEventArgs e) t) 
            => !properties.Any()||(t.e.MemberInfo != null && properties.Contains(t.e.MemberInfo.Name) ||
                t.e.PropertyName != null && properties.Contains(t.e.PropertyName));

        public static IObservable<T> WhenModifiedObjects<T>(this IObjectSpace objectSpace,Expression<Func<T,object>> memberSelector) 
            => objectSpace.WhenModifiedObjects(typeof(T),((MemberExpression) memberSelector.Body).Member.Name).Cast<T>();

        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenModifiedObjectsDetailed<T>(
            this IObjectSpace objectSpace, bool emitAfterCommit) 
            => objectSpace.WhenCommitingDetailed<T>(ObjectModification.All, emitAfterCommit);


        [Obsolete("use WhenCommitingDetailed",true)]
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)>
            WhenModifiedObjectsDetailed<T>(this IObjectSpace objectSpace, bool emitAfterCommit,ObjectModification objectModification ) 
            => objectSpace.WhenCommitingDetailed<T>(objectModification, emitAfterCommit);

        [Obsolete("use WhenCommitingDetailed",true)]
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)>
            WhenModifiedObjectsDetailed(this IObjectSpace objectSpace,Type objectType,
            bool emitAfterCommit,ObjectModification objectModification ) 
            => objectSpace.WhenCommitingDetailed(objectType, objectModification, emitAfterCommit);
        
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenModifiedObjectsDetailed<T>(this IObjectSpace objectSpace,
            ObjectModification objectModification,bool emitAfterCommit) 
            => objectSpace.WhenCommitingDetailed<T>( emitAfterCommit,objectModification);
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenModifiedObjectsDetailed<T>(this IObjectSpace objectSpace,
            ObjectModification objectModification ) 
            => objectSpace.WhenCommitingDetailed<T>(objectModification, false);

        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenCommitingDetailed<T>(
                this IObjectSpace objectSpace, bool emitAfterCommit, ObjectModification objectModification,Func<T,bool> criteria=null) 
            => objectSpace.WhenCommiting()
                .SelectMany(_ => {
                    var modifiedObjects = objectSpace.ModifiedObjects<T>(objectModification).Where(t => criteria==null|| criteria.Invoke(t.instance)).ToArray();
                    return modifiedObjects.Any() ? emitAfterCommit ? objectSpace.WhenCommitted().FirstAsync().Select(space => (space, modifiedObjects))
                            : (objectSpace, modifiedObjects).ReturnObservable() : Observable.Empty<(IObjectSpace, (T instance, ObjectModification modification)[])>();
                });
        
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenCommitingDetailed(
                this IObjectSpace objectSpace,Type objectType, bool emitAfterCommit, ObjectModification objectModification,Func<object,bool> criteria=null,
                params string[] modifiedProperties) 
            => objectSpace.WhenModifiedObjects(objectType,modifiedProperties)
                .Select(_ => objectSpace.WhenCommiting()
                    .SelectMany(_ => {
                        var modifiedObjects = objectSpace.ModifiedObjects(objectType, objectModification)
                            .Where(t => criteria==null|| criteria.Invoke(t.instance)).ToArray();
                        return modifiedObjects.Any() ? emitAfterCommit ? objectSpace.WhenCommitted().FirstAsync().Select(space => (space, modifiedObjects))
                            : (objectSpace, modifiedObjects).ReturnObservable() : Observable.Empty<(IObjectSpace, (object instance, ObjectModification modification)[])>();
                    })).Switch();

        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)>
            WhenCommitingDetailed<T>(this IObjectSpace objectSpace, ObjectModification objectModification, bool emitAfterCommit,Func<T,bool> criteria=null) 
            => objectSpace.WhenCommitingDetailed(emitAfterCommit, objectModification,criteria);
        
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)>
            WhenCommitingDetailed(this IObjectSpace objectSpace,Type objectType, ObjectModification objectModification, bool emitAfterCommit,Func<object,bool> criteria=null,
                params string[] modifiedProperties) 
            => objectSpace.WhenCommitingDetailed(objectType, emitAfterCommit, objectModification,criteria,modifiedProperties);
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)>
            WhenCommittedDetailed<T>(this IObjectSpace objectSpace, ObjectModification objectModification) 
            => objectSpace.ParentObjectSpace().WhenCommitingDetailed<T>(true, objectModification);

        public static IObjectSpace ParentObjectSpace(this IObjectSpace objectSpace) 
            => !objectSpace.IsNested() ? objectSpace : (IObjectSpace)objectSpace.GetPropertyValue("ParentObjectSpace");

        public static bool IsNested(this IObjectSpace objectSpace) 
            => objectSpace.GetType().InheritsFrom("DevExpress.ExpressApp.Xpo.XPNestedObjectSpace");

        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)>
            WhenCommittedDetailed<T>(this IObjectSpace objectSpace, ObjectModification objectModification,
                Func<T, bool> criteria = null, params string[] modifiedProperties) where T : class 
            => objectSpace.WhenCommitingDetailed(true, objectModification,criteria, modifiedProperties);

        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)>
            WhenCommittedDetailed(this IObjectSpace objectSpace, Type objectType, ObjectModification objectModification,
                Func<object, bool> criteria = null, params string[] modifiedProperties) 
            => modifiedProperties.Any()?objectSpace.WhenModifiedObjects(objectType,modifiedProperties).Take(1)
                    .SelectMany(_ => objectSpace.WhenCommitingDetailed(objectType,objectModification, true,criteria)):
                objectSpace.WhenCommitingDetailed(objectType,objectModification, true,criteria);

        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenCommitingDetailed<T>(
            this IObjectSpace objectSpace, bool emitAfterCommit, ObjectModification objectModification, Func<T, bool> criteria, params string[] modifiedProperties) where T:class 
            => !modifiedProperties.Any() ? objectSpace.WhenCommitingDetailed(objectModification, emitAfterCommit, criteria)
                : objectSpace.WhenModifiedObjects(typeof(T), modifiedProperties).Cast<T>().Where(criteria??(_ =>true) )
                    .Buffer(objectSpace.WhenCommitingDetailed(false, objectModification, criteria)).WhenNotEmpty()
                    .SelectMany(modifiedObjects => {
                        var objectSpaceModifiedObjects = objectSpace.ModifiedObjects(objectModification, modifiedObjects).ToArray();
                        if (emitAfterCommit) {
                            return objectSpace.WhenCommitted().Take(1)
                                .Select(_ => (objectSpace, details:objectSpaceModifiedObjects));
                        }
                        return (objectSpace, details: objectSpaceModifiedObjects).ReturnObservable();
                    }).Where(t => t.details.Any());

        public static void DeleteObject<T>(this T value, Expression<Func<T, bool>> criteria = null) where T:class,IObjectSpaceLink => value.ObjectSpace.Delete(value);

        public static void DeleteObject<T>(this IObjectSpace objectSpace, Expression<Func<T, bool>> criteria=null) {
            var query = objectSpace.GetObjectsQuery<T>();
            if (criteria != null) {
                query = query.Where(criteria);
            }
            objectSpace.Delete(query.ToArray());
        }

        public static
            IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)>
            WhenModifiedObjectsDetailed<T>(this IObjectSpace objectSpace) where T : class 
            => objectSpace.WhenCommitingDetailed<T>(ObjectModification.All, false);

        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommiting<T>(this IObjectSpace objectSpace, 
            ObjectModification objectModification = ObjectModification.All,bool emitAfterCommit = false) => objectSpace.WhenCommitingDetailed<T>(objectModification, emitAfterCommit)
                .Select(t => (t.objectSpace,t.details.Select(t1 => t1.instance)));

        public static bool IsUpdated<T>(this IObjectSpace objectSpace, T t) where T:class 
            => !objectSpace.IsNewObject(t)&&!objectSpace.IsDeletedObject(t);

        
        public static IObservable<(IObjectSpace objectSpace, T[] objects)> WhenDeletedObjects<T>(this IObjectSpace objectSpace,bool emitAfterCommit=false) => emitAfterCommit ? objectSpace.WhenCommiting<T>(ObjectModification.Deleted, true)
                    .Select(t => (t.objectSpace, t.objects.Select(t1 => t1).ToArray())).Finally(() => { })
                : objectSpace.WhenObjectDeleted()
                    .Select(pattern => (pattern.objectSpace, pattern.e.Objects.OfType<T>().ToArray()))
                    .TakeUntil(objectSpace.WhenDisposed());

        static bool HasAnyValue(this ObjectModification value, params ObjectModification[] values) => values.Any(@enum => value == @enum);

        public static IEnumerable<(object instance, ObjectModification modification)> ModifiedObjects(this IObjectSpace objectSpace,ObjectModification objectModification) 
            => objectSpace.ModifiedObjects( objectModification, objectSpace.YieldAll()
                .SelectMany(space => space.GetObjectsToDelete(true).Cast<object>().Concat(space.GetObjectsToSave(true).Cast<object>())).Distinct()).WhereNotDefault();

        public static IEnumerable<(T o, ObjectModification modification)> ModifiedObjects<T>(this IObjectSpace objectSpace, ObjectModification objectModification, IEnumerable<T> objects) where T:class 
            => objects.Select(o => {
                if (objectSpace.IsDeletedObject(o) && objectModification.HasAnyValue(ObjectModification.Deleted,
                        ObjectModification.All, ObjectModification.NewOrDeleted, ObjectModification.UpdatedOrDeleted)) {
                    return (o, ObjectModification.Deleted);
                }

                if (objectSpace.IsNewObject(o) && objectModification.HasAnyValue(ObjectModification.New,
                        ObjectModification.All, ObjectModification.NewOrDeleted, ObjectModification.NewOrUpdated)) {
                    return (o, ObjectModification.New);
                }

                if (objectSpace.IsUpdated(o) && objectModification.HasAnyValue(ObjectModification.Updated,
                        ObjectModification.All, ObjectModification.UpdatedOrDeleted, ObjectModification.NewOrUpdated)) {
                    return (o, ObjectModification.Updated);
                }

                return default;
            });


        public static IEnumerable<(T instance, ObjectModification modification)> ModifiedObjects<T>(this IObjectSpace objectSpace, ObjectModification objectModification) 
            => objectSpace.ModifiedObjects(objectModification).Where(t => t.instance is T).Select(t => ((T)t.instance,t.modification));
        public static IEnumerable<(object instance, ObjectModification modification)> ModifiedObjects(this IObjectSpace objectSpace,Type objectType, ObjectModification objectModification) 
            => objectSpace.ModifiedObjects(objectModification).Where(t => objectType.IsInstanceOfType(t.instance) ).Select(t => (t.instance,t.modification));
        
        public static IObservable<T> ModifiedExistingObject<T>(this XafApplication application,
            Func<(IObjectSpace objectSpace,ObjectChangedEventArgs e),bool> filter = null){
            filter ??= (_ => true);
            return application.AllModifiedObjects<T>(_ => filter(_) && !_.objectSpace.IsNewObject(_.e.Object));
        }

        public static IObservable<T> ModifiedNewObject<T>(this XafApplication application,
            Func<(IObjectSpace objectSpace,ObjectChangedEventArgs e),bool> filter = null){
            filter ??= (_ => true);
            return application.AllModifiedObjects<T>(_ => filter(_) && _.objectSpace.IsNewObject(_.e.Object));
        }

        public static IObservable<(IObjectSpace objectSpace, T[] objects)> DeletedObjects<T>(this XafApplication application) where T : class 
            => application.WhenObjectSpaceCreated().SelectMany(objectSpace => objectSpace.WhenDeletedObjects<T>());

        public static IObservable<T> AllModifiedObjects<T>(this XafApplication application,Func<(IObjectSpace objectSpace,ObjectChangedEventArgs e),bool> filter=null ) 
            => application.WhenObjectSpaceCreated()
                .SelectMany(objectSpace => objectSpace.WhenObjectChanged()
                    .Where(tuple => filter == null || filter(tuple))
                    .SelectMany(tuple => tuple.objectSpace.ModifiedObjects.OfType<T>()));

        public static void CommitChanges(this IObjectSpaceLink link)
            => link.ObjectSpace.CommitChanges();
        
        public static Task CommitChangesAsync(this IObjectSpaceLink link)
            => link.ObjectSpace.CommitChangesAsync();
        
        public static IObservable<T> Commit<T>(this IEnumerable<T> source,IObjectSpace objectSpace=null) where T:IObjectSpaceLink {
            var links = source as T[] ?? source.ToArray();
            return links.Finally((objectSpace??links.First().ObjectSpace).CommitChanges).ToNowObservable();
        }
        public static IObservable<T> Commit<T>(this IEnumerable<T> source,IObjectSpaceLink objectSpace) where T:IObjectSpaceLink {
            var links = source as T[] ?? source.ToArray();
            return links.Finally(objectSpace.CommitChanges).ToNowObservable();
        }

        public static IObservable<T> Commit<T>(this T link) where T:IObjectSpaceLink
            => link.ObjectSpace.CommitChangesAsync().ToObservable().To(link);
        public static IObservable<T> Commit<T>(this IObservable<T> source) where T:IObjectSpaceLink
            => source.BufferUntilCompleted(true).SelectMany(links => links.First().Commit());
        
        public static T CreateObject<T>(this IObjectSpaceLink link)
            => link.ObjectSpace.CreateObject<T>();
        
        public static Task CommitChangesAsync(this IObjectSpace objectSpace) {
            if (objectSpace is  NonPersistentObjectSpace nonPersistentObjectSpace) {
                return Task.WhenAll(nonPersistentObjectSpace.AdditionalObjectSpaces.OfType<IObjectSpaceAsync>().ToNowObservable()
                    .SelectMany(async => Observable.FromAsync(() => async.CommitChangesAsync()))
                    .ToTask());
            }
            return ((IObjectSpaceAsync)objectSpace).CommitChangesAsync();
        }

        public static IObservable<Unit> Commit(this IObjectSpace objectSpace) 
            => objectSpace.CommitChangesAsync().ToObservable();

        public static T Reload<T>(this T link) where T:class,IObjectSpaceLink
            => (T)link.ObjectSpace.ReloadObject(link);
        
        public static T Reload<T>(this T value,Func<IObjectSpace> objectSpaceSelector)where T:IObjectSpaceLink 
            => objectSpaceSelector().GetObject(value);
        
        public static T Reload<T>(this T value,XafApplication application) where T:IObjectSpaceLink
            => value.Reload(application.CreateObjectSpace);

        public static T Reload<T>(this IObjectSpace objectSpace, T value) where T:class {
            if (objectSpace is INestedObjectSpace nos) {  
                Reload(nos.ParentObjectSpace, value);  
                nos.Refresh();  
            }  
            else {  
                objectSpace.ReloadObject(value);  
            }

            return objectSpace.GetObject(value);
        }

        
        public static IObservable<(T theObject, IObjectSpace objectSpace)> FindObject<T>(this XafApplication application,Func<IQueryable<T>,IQueryable<T>> query=null) 
            => Observable.Using(() => application.CreateObjectSpace(typeof(T)), space => space.ExistingObject(query).Select(arg => (arg,space)));


        public static IObservable<T> WhenObjectCommitted<T>(this IObservable<T> source) where T:IObjectSpaceLink 
            => source.SelectMany(_ => _.ObjectSpace.WhenCommitted().FirstAsync().Select(tuple => _));

        public static IObservable<object> WhenNewObjectCreated(
            this IObjectSpace objectSpace, Type objectSpaceLinkType = null)
#if !XAF192
            => objectSpace.WhenModifiedChanging().Where(t => !t.e.Cancel && t.objectSpace.IsNewObject(t.e.Object) && t.e.MemberInfo == null)
                .Where(t => objectSpaceLinkType == null || objectSpaceLinkType.IsInstanceOfType(t.e.Object))
                .Select(t => t.e.Object);
#else
            => Observable.Throw<object>(new NotImplementedException());
#endif

        public static IObservable<T> WhenNewObjectCreated<T>(this IObjectSpace objectSpace) where T:IObjectSpaceLink
            => objectSpace.WhenNewObjectCreated(typeof(T)).OfType<T>();

        public static IObservable<T> WhenNewObjectCommiting<T>(this IObjectSpace objectSpace) where T:class 
            => objectSpace.WhenCommiting()
                .SelectMany(t => objectSpace.ModifiedObjects.OfType<T>().Where(r => t.objectSpace.IsNewObject(r)))
                .TraceRX();

        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommiting<T>(
            this XafApplication application, ObjectModification objectModification = ObjectModification.All) where T : class 
            => application.WhenObjectSpaceCreated().SelectMany(objectSpace => objectSpace.WhenCommiting<T>(objectModification));
        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenProviderCommiting<T>(
            this XafApplication application, ObjectModification objectModification = ObjectModification.All) where T : class 
            => application.WhenProviderObjectSpaceCreated().SelectMany(objectSpace => objectSpace.WhenCommiting<T>(objectModification));
        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenProviderCommitted<T>(
            this XafApplication application, ObjectModification objectModification = ObjectModification.All) {
            return application.WhenProviderObjectSpaceCreated().WhenCommitted<T>(objectModification);
        }
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<object> objects)> WhenProviderCommitted(
            this XafApplication application,Type objectType, ObjectModification objectModification = ObjectModification.All) {
            return application.WhenProviderObjectSpaceCreated().WhenCommitted(objectType,objectModification);
        }
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenProviderCommitting<T>(
            this XafApplication application, ObjectModification objectModification = ObjectModification.All) where T : class 
            => application.WhenProviderObjectSpaceCreated().SelectMany(space => space.WhenCommiting<T>(objectModification));

        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommitted<T>(
            this IObservable<IObjectSpace> source, ObjectModification objectModification = ObjectModification.All) 
            => source.SelectMany(objectSpace => objectSpace.WhenCommitted<T>(objectModification));

        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommitted<T>(
            this IObjectSpace objectSpace, ObjectModification objectModification = ObjectModification.All) 
            => objectSpace.WhenCommitingDetailed<T>(objectModification, true).Select(t => (t.objectSpace,t.details.Select(t1 => t1.instance)));

        public static IObservable<(IObjectSpace objectSpace, IEnumerable<object> objects)> WhenCommitted(
            this IObservable<IObjectSpace> source,Type objectType, ObjectModification objectModification = ObjectModification.All) 
            => source.SelectMany(objectSpace => objectSpace.WhenCommitingDetailed(objectType, objectModification, true)
                .Select(t => (t.objectSpace,t.details.Select(t1 => t1.instance))));

        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommitted<T>(
            this XafApplication application, ObjectModification objectModification,params T[] objects) 
            => application.WhenObjectSpaceCreated().WhenCommitted(objectModification, objects);
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenCommitted<T>(
            this XafApplication application, params T[] objects) 
            => application.WhenObjectSpaceCreated().WhenCommitted(ObjectModification.All,objects);
        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenProviderCommitted<T>(
            this XafApplication application, ObjectModification objectModification,params T[] objects) 
            => application.WhenProviderObjectSpaceCreated().WhenCommitted(objectModification, objects);
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenProviderCommitted<T>(
            this XafApplication application, params T[] objects) 
            => application.WhenProviderObjectSpaceCreated().WhenCommitted(ObjectModification.All,objects);

        private static IObservable<(IObjectSpace objectSpace, IEnumerable<T>)> WhenCommitted<T>(this IObservable<IObjectSpace> whenObjectSpaceCreated,ObjectModification objectModification=ObjectModification.All,params T[] instances) 
            => whenObjectSpaceCreated.WhenCommitted<T>(objectModification).Select(t => {
                if (instances.Any()) {
                    var keys = instances.Select(arg => t.objectSpace.GetKeyValue(arg)).ToArray();
                    return (t.objectSpace, t.objects.Where(arg => keys.Contains(t.objectSpace.GetKeyValue(arg))));
                }

                return t;
            });

        public static IObservable<T> Objects<T>(this IObservable<(IObjectSpace, IEnumerable<T> objects)> source)
            => source.SelectMany(t => t.objects);
        
        public static IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> WhenExistingObjectCommiting<T>(this XafApplication application) where T : class 
            => application.WhenObjectSpaceCreated().SelectMany(objectSpace => objectSpace.WhenCommiting<T>(ObjectModification.Updated));

        public static T FindObject<T>(this IObjectSpace objectSpace, Expression<Func<T,bool>> expression) 
            => objectSpace.FindObject<T>(CriteriaOperator.FromLambda(expression));

        public static IObservable<T> ExistingObject<T>(this IObjectSpace objectSpace,Func<IQueryable<T>,IQueryable<T>> query=null){
            var objectsQuery = objectSpace.GetObjectsQuery<T>();
            if (query != null){
                objectsQuery = objectsQuery.Concat(query(objectsQuery));
            }
            return objectsQuery.ToObservable().Pair(objectSpace).Select(t => t.source);
        }

        
        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectsGettingEventArgs e)> ObjectsGetting(this IObservable<NonPersistentObjectSpace> source) 
            => source.SelectMany(item => item.WhenObjectsGetting());

        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectsGettingEventArgs e)> WhenObjectsGetting(this NonPersistentObjectSpace item) 
            => Observable.FromEventPattern<EventHandler<ObjectsGettingEventArgs>, ObjectsGettingEventArgs>(h => item.ObjectsGetting += h, h => item.ObjectsGetting -= h,ImmediateScheduler.Instance)
                .TransformPattern<ObjectsGettingEventArgs, NonPersistentObjectSpace>();
        
        
        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectGettingEventArgs e)> ObjectGetting(this IObservable<NonPersistentObjectSpace> source) 
            => source.SelectMany(item => item.WhenObjectGetting());

        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectGettingEventArgs e)> WhenObjectGetting(this NonPersistentObjectSpace item) 
            => Observable.FromEventPattern<EventHandler<ObjectGettingEventArgs>, ObjectGettingEventArgs>(h => item.ObjectGetting += h, h => item.ObjectGetting -= h,ImmediateScheduler.Instance)
                .TransformPattern<ObjectGettingEventArgs, NonPersistentObjectSpace>();
        
        public static IObservable<(IObjectSpace objectSpace,CancelEventArgs e)> Commiting(this IObservable<IObjectSpace> source) 
            => source.SelectMany(space => space.WhenCommiting());
        
        public static IObservable<(IObjectSpace objectSpace,HandledEventArgs e)> WhenCustomCommitChanges(this IObjectSpace source) 
            => Observable.FromEventPattern<EventHandler<HandledEventArgs>, HandledEventArgs>(h => source.CustomCommitChanges += h, h => source.CustomCommitChanges -= h,ImmediateScheduler.Instance)
                .TransformPattern<HandledEventArgs, IObjectSpace>()
                .TraceRX();

        public static IObservable<IObjectSpace> Committed(this IObservable<IObjectSpace> source) 
            => source.SelectMany(objectSpace => objectSpace.WhenCommitted());
        
        public static IObservable<IObjectSpace> WhenCommitted(this IObjectSpace objectSpace) 
            => Observable.FromEventPattern<EventHandler, EventArgs>(h => objectSpace.Committed += h, h => objectSpace.Committed -= h,ImmediateScheduler.Instance)
                .TransformPattern<IObjectSpace>();

        public static IObservable<(IObjectSpace objectSpace, CancelEventArgs e)> WhenCommiting(this IObjectSpace item) 
            => Observable.FromEventPattern<EventHandler<CancelEventArgs>, CancelEventArgs>(h => item.Committing += h, h => item.Committing -= h,ImmediateScheduler.Instance)
                .TransformPattern<CancelEventArgs, IObjectSpace>()
                .TraceRX();

        
        public static IObservable<(IObjectSpace objectSpace,ObjectsManipulatingEventArgs e)> ObjectDeleted(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => item.WhenObjectDeleted());

        public static IObservable<(IObjectSpace objectSpace,ObjectsManipulatingEventArgs e)> WhenObjectDeleted(this IObjectSpace item) 
            => Observable.FromEventPattern<EventHandler<ObjectsManipulatingEventArgs>, ObjectsManipulatingEventArgs>(h => item.ObjectDeleted += h, h => item.ObjectDeleted -= h,ImmediateScheduler.Instance)
                .TransformPattern<ObjectsManipulatingEventArgs, IObjectSpace>();

        
        public static IObservable<(IObjectSpace objectSpace,ObjectChangedEventArgs e)> ObjectChanged(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => item.WhenObjectChanged());

        public static IObservable<(IObjectSpace objectSpace,ObjectChangedEventArgs e)> WhenObjectChanged(this IObjectSpace item,params Type[] objectTypes) 
            => Observable.FromEventPattern<EventHandler<ObjectChangedEventArgs>, ObjectChangedEventArgs>(h => item.ObjectChanged += h, h => item.ObjectChanged -= h,ImmediateScheduler.Instance)
                .TransformPattern<ObjectChangedEventArgs, IObjectSpace>().Where(t =>!objectTypes.Any() ||objectTypes.Any(type => type.IsInstanceOfType(t.e.Object)));
        public static IObservable<(IObjectSpace objectSpace,ObjectChangedEventArgs e)> WhenObjectChanged(this IObjectSpace item,Type objectType,params string[] properties) 
            => Observable.FromEventPattern<EventHandler<ObjectChangedEventArgs>, ObjectChangedEventArgs>(h => item.ObjectChanged += h, h => item.ObjectChanged -= h,ImmediateScheduler.Instance)
                .TransformPattern<ObjectChangedEventArgs, IObjectSpace>().Where(t =>objectType.IsInstanceOfType(t.e.Object)&&properties.Any(s => t.e.PropertyName==s));

        public static IObservable<Unit> Disposed(this IObservable<IObjectSpace> source) 
            => source.SelectMany(objectSpace => objectSpace.WhenDisposed());

        public static IObservable<Unit> WhenDisposed(this IObjectSpace objectSpace)
            => Observable.FromEventPattern<EventHandler,EventArgs>(h => objectSpace.Disposed += h, h => objectSpace.Disposed -= h,ImmediateScheduler.Instance)
                .ToUnit();

        
        public static IObservable<IObjectSpace> WhenModifyChanged(this IObjectSpace objectSpace) 
            => Observable.FromEventPattern<EventHandler, EventArgs>(h => objectSpace.ModifiedChanged += h, h => objectSpace.ModifiedChanged -= h,Scheduler.Immediate)
                .Select(pattern => (IObjectSpace) pattern.Sender);

        public static IObservable<IObjectSpace> WhenModifyChanged(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => item.WhenModifyChanged());

#if !XAF192
        
        public static IObservable<(IObjectSpace objectSpace, ObjectSpaceModificationEventArgs e)> WhenModifiedChanging(this IObjectSpace objectSpace) 
            => Observable.FromEventPattern<EventHandler<ObjectSpaceModificationEventArgs>, ObjectSpaceModificationEventArgs>(h => ((BaseObjectSpace) objectSpace).ModifiedChanging += h, h => ((BaseObjectSpace) objectSpace).ModifiedChanging -= h,ImmediateScheduler.Instance)
                .TransformPattern<ObjectSpaceModificationEventArgs,IObjectSpace>();

        public static IObservable<(IObjectSpace objectSpace, ObjectSpaceModificationEventArgs e)> WhenModifiedChanging(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => item.WhenModifiedChanging());
#endif

        static readonly ISubject<(IObjectSpace objectSpace,object obj)> ReloadObjectSubject=Subject.Synchronize(new Subject<(IObjectSpace objectSpace,object obj)>());

        private static readonly Type XPInvalidateableObjectType =
            AppDomain.CurrentDomain.GetAssemblyType("DevExpress.Xpo.IXPInvalidateableObject");
        public static IObservable<TLink> ReloadNotifyObject<TLink>(this TLink link) where  TLink:IObjectSpaceLink 
            => link.Defer(() => {
                    if (link.GetType().Implements(XPInvalidateableObjectType)) {

                        if (!(bool)link.GetPropertyValue("IsInvalidated") && !(bool)link.GetPropertyValue("Session").GetPropertyValue("IsObjectsLoading"))
                            return link.ReloadObject().ReturnObservable();
                        return link.GetPropertyValue("Session").WhenEvent("ObjectLoaded").FirstAsync()
                            .Do(_ => link.ReloadObject()).To(link);
                    }
                    throw new NotImplementedException();
                })
                .Do(spaceLink => ReloadObjectSubject.OnNext((spaceLink.ObjectSpace, spaceLink)));

        public static IObservable<T> ToObjects<T>(this IObservable<(IObjectSpace objectSpace, T[] objects)> source)
            =>source.SelectMany(t => t.objects.Select(arg => arg));
        
        public static IObservable<(IObjectSpace objectSpace, object obj)> WhenObjectReloaded(this IObjectSpace objectSpace,object obj=null) 
            => ReloadObjectSubject.Where(t => t.objectSpace==objectSpace&& (obj == null||obj==t.obj)).AsObservable();
        
        public static IObservable<(IObjectSpace objectSpace, T obj)> WhenObjectReloaded<T>(this IObjectSpace objectSpace,T obj=null) where T:class
            => ReloadObjectSubject.Where(t => t.objectSpace==objectSpace&&t.obj is T tObj&&(obj==null||obj==tObj)).Select(t => (t.objectSpace,(T)t.obj)).AsObservable();

        public static IObservable<IObjectSpace> WhenRefreshing(this IObjectSpace objectSpace)
            => Observable.FromEventPattern<EventHandler<CancelEventArgs>, CancelEventArgs>(h => objectSpace.Refreshing += h, h => objectSpace.Refreshing -= h,ImmediateScheduler.Instance)
                .Select(pattern => (IObjectSpace) pattern.Sender);
        public static IObservable<IObjectSpace> WhenReloaded(this IObjectSpace objectSpace) 
            => Observable.FromEventPattern<EventHandler, EventArgs>(h => objectSpace.Reloaded += h, h => objectSpace.Reloaded -= h,ImmediateScheduler.Instance)
                .Select(pattern => (IObjectSpace) pattern.Sender);

        public static IObservable<IObjectSpace> WhenReloaded(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => item.WhenReloaded());
        
        public static IObservable<T> Reload<T>(this IObservable<T> source,XafApplication application) where T:IObjectSpaceLink 
            => source.Select(item => item.Reload(application));
        public static IObservable<T> Reload<T>(this IObservable<IList<T>> source,XafApplication application) where T:IObjectSpaceLink 
            => source.WhenNotEmpty().SelectMany(items => {
                var firstItem = items.First().Reload(application);
                return items.Select(link => firstItem.ObjectSpace.GetObject(link)).StartWith(firstItem);
            });
        

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
}