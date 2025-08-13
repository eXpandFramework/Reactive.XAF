using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.ExpressionExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.Collections;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.XAF.Modules.Reactive.Services {
    public static partial class ObjectSpaceExtensions {
        public static IObservable<(IObjectSpace objectSpace, CancelEventArgs e)> WhenRollingBack(this IObjectSpace objectSpace) 
            => objectSpace.ProcessEvent<CancelEventArgs>(nameof(IObjectSpace.RollingBack)).TakeUntil(objectSpace.WhenDisposed()).InversePair(objectSpace);
        
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

        public static void AcceptObject(this NonPersistentObjectSpace objectSpace, object item) => objectSpace.CallMethod("AcceptObject", item);
        
        public static IObservable<T> Request<T>(this IObjectSpace objectSpace) 
            => objectSpace.Request(typeof(T)).Cast<T>();

        public static IObservable<T[]> RequestAll<T>(this IObjectSpace objectSpace) 
            => objectSpace.Request<T>().BufferUntilCompleted();
        
        public static IObservable<object[]> RequestAll(this IObjectSpace objectSpace,Type objectType) 
            => objectSpace.Request(objectType).BufferUntilCompleted();

        public static IObservable<object> Request(this IObjectSpace objectSpace,Type objectType) 
            => ((IBindingList) objectSpace.CreateCollection(objectType)).WhenObjects();

        public static IObservable<object> WhenObjects(this IBindingList bindingList,bool waitForTrigger=false) {
            var signalCompletion = bindingList.WhenListChanged().Where(e => e.ListChangedType == (ListChangedType) (-10000)).To(bindingList).Take(1);
            return waitForTrigger ? signalCompletion.SelectMany(list => list.Cast<object>())
                : signalCompletion.Merge(bindingList.Cast<object>().TakeLast(1).ToNowObservable()
                        .IgnoreElements().To(bindingList))
                    .SelectMany(list => list.Cast<object>());
        }

        public static IObservable<T> WhenModifiedObjects<T>(this IObjectSpace objectSpace, Expression<Func<T,object>>[] properties)
            =>objectSpace.WhenModifiedObjects<T>(properties.Select(expression => expression.MemberExpressionName()).ToArray());
        
        public static IObservable<T> WhenModifiedObjects<T>(this IObjectSpace objectSpace, params string[] properties) 
            => objectSpace.WhenModifiedObjects(typeof(T),properties).Cast<T>();

        public static IObservable<object> WhenModifiedObjects(this IObjectSpace objectSpace, Type objectType, params string[] properties) {
            var typeInfo = objectType.ToTypeInfo();
            var notExisting = properties.Select(s => s.TrimStart('*').TrimStart('!')).WhereNotNullOrEmpty().WhereDefault(name => typeInfo.FindMember(name)).ToArray();
            if (notExisting.Any()) {
                return new InvalidOperationException($"{nameof(WhenModifiedObjects)}: {objectType.FullName} member ({notExisting.JoinComma()}) not found").Throw<object>();
            }
            return objectSpace.WhenObjectChanged()
                .Where(t => objectType.IsInstanceOfType(t.e.Object) && properties.PropertiesMatch(t))
                .Select(t => t.e.Object);
        }
        public static IObservable<object> WhenModifiedObjects(this IObjectSpace objectSpace, Type[] objectTypes, params string[] properties) 
            => objectSpace.WhenObjectChanged()
                .Where(t =>  properties.PropertiesMatch(t)&&objectTypes.Any(type => type.IsInstanceOfType(t.e.Object)))
                .Select(t => t.e.Object);

        private static bool PropertiesMatch(this string[] properties, (IObjectSpace objectSpace, ObjectChangedEventArgs e) t) {
            if (!properties.Any()) return true;
            var name = t.e.MemberInfo?.Name ?? t.e.PropertyName;
            if (string.IsNullOrEmpty(name)) return properties.Contains("*");
            var isWildcard = properties.Contains("*");
            var isExcluded = properties.Contains("!" + name);
            var isExplicitlyIncluded = properties.Contains(name);
            return isWildcard && !isExcluded || isExplicitlyIncluded;
        }

        public static IObservable<T> WhenModifiedObjects<T>(this IObjectSpace objectSpace,Expression<Func<T,object>> memberSelector) 
            => objectSpace.WhenModifiedObjects(typeof(T),memberSelector.MemberExpressionName()).Cast<T>();

        public static IObjectSpace ParentObjectSpace(this IObjectSpace objectSpace) {
            while (objectSpace.IsNested()) {
                objectSpace = (IObjectSpace)objectSpace.GetPropertyValue("ParentObjectSpace");
            }
            return objectSpace;
        }

        public static bool IsNested(this IObjectSpace objectSpace) 
            => objectSpace.GetType().InheritsFrom("DevExpress.ExpressApp.Xpo.XPNestedObjectSpace");

        private static IObservable<(IObjectSpace objectSpace, IEnumerable<T>)> WhenCommitted<T>(this IObservable<IObjectSpace> whenObjectSpaceCreated,T[] instances,ObjectModification objectModification=ObjectModification.All) 
            => whenObjectSpaceCreated.WhenCommitted<T>(objectModification).Select(t => {
                if (instances.Any()) {
                    var keys = instances.Select(arg => t.objectSpace.GetKeyValue(arg)).ToArray();
                    return (t.objectSpace, t.objects.Where(arg => keys.Contains(t.objectSpace.GetKeyValue(arg))));
                }

                return t;
            });

        public static IObservable<T> ToObjects<T>(this IObservable<(IObjectSpace, IEnumerable<T> objects)> source)
            =>source.SelectMany(t => t.objects);
        
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

        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectsGettingEventArgs e)> WhenObjectsGetting(this NonPersistentObjectSpace objectSpace) 
            => objectSpace.ProcessEvent<ObjectsGettingEventArgs>(nameof(NonPersistentObjectSpace.ObjectsGetting))
                .InversePair(objectSpace).TakeUntil(objectSpace.WhenDisposed());

        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectGettingEventArgs e)> ObjectGetting(this IObservable<NonPersistentObjectSpace> source) 
            => source.SelectMany(item => item.WhenObjectGetting());

        public static IObservable<(NonPersistentObjectSpace objectSpace,ObjectGettingEventArgs e)> WhenObjectGetting(this NonPersistentObjectSpace objectSpace) 
            => objectSpace.ProcessEvent<ObjectGettingEventArgs>(nameof(NonPersistentObjectSpace.ObjectGetting))
                .InversePair(objectSpace).TakeUntil(objectSpace.WhenDisposed());
        
        public static IObservable<CancelEventArgs> Commiting(this IObservable<IObjectSpace> source) 
            => source.SelectMany(space => space.WhenCommiting());
        
        public static IObservable<(IObjectSpace objectSpace,HandledEventArgs e)> WhenCustomCommitChanges(this IObjectSpace objectSpace) 
            => objectSpace.ProcessEvent<HandledEventArgs>(nameof(IObjectSpace.CustomCommitChanges)).InversePair(objectSpace)
                .TakeUntil(objectSpace.WhenDisposed());

        public static IObservable<IObjectSpace> Committed(this IObservable<IObjectSpace> source) 
            => source.SelectMany(objectSpace => objectSpace.WhenCommitted());
        
        public static IObservable<IObjectSpace> WhenCommitted(this IObjectSpace objectSpace) 
            => objectSpace.ProcessEvent(nameof(IObjectSpace.Committed)).To(objectSpace)
                .TakeUntil(objectSpace.WhenDisposed())
            ;

        public static IObservable<CancelEventArgs> WhenCommiting(this IObjectSpace objectSpace) 
            => objectSpace.ProcessEvent<CancelEventArgs>(nameof(IObjectSpace.Committing))
                .TakeUntil(objectSpace.WhenDisposed());

        public static IObservable<(IObjectSpace objectSpace,ObjectsManipulatingEventArgs e)> ObjectDeleted(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => item.WhenObjectDeleted());

        public static IObservable<(IObjectSpace objectSpace,ObjectsManipulatingEventArgs e)> WhenObjectDeleted(this IObjectSpace objectSpace) 
            => objectSpace.ProcessEvent<ObjectsManipulatingEventArgs>(nameof(IObjectSpace.ObjectDeleted)).InversePair(objectSpace)
                .TakeUntil(objectSpace.WhenDisposed());

        public static IObservable<(IObjectSpace objectSpace,ObjectChangedEventArgs e)> ObjectChanged(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => item.WhenObjectChanged());

        public static IObservable<(IObjectSpace objectSpace,ObjectChangedEventArgs e)> WhenObjectChanged(this IObjectSpace objectSpace,params Type[] objectTypes) 
            => objectSpace.ProcessEvent<ObjectChangedEventArgs>(nameof(IObjectSpace.ObjectChanged)).InversePair(objectSpace)
                .TakeUntil(objectSpace.WhenDisposed())
                .Where(t =>!objectTypes.Any() ||objectTypes.Any(type => type.IsInstanceOfType(t.source.Object)));
        
        public static IObservable<(IObjectSpace objectSpace,ObjectChangedEventArgs e)> WhenObjectChanged(this IObjectSpace objectSpace,Type objectType,params string[] properties) 
            => objectSpace.ProcessEvent<ObjectChangedEventArgs>(nameof(IObjectSpace.ObjectChanged)).InversePair(objectSpace)
                .TakeUntil(objectSpace.WhenDisposed())
                .Where(t =>objectType.IsInstanceOfType(t.source.Object)&&properties.Any(s => t.source.PropertyName==s));

        public static IObservable<Unit> Disposed(this IObservable<IObjectSpace> source) 
            => source.SelectMany( objectSpace => objectSpace.WhenDisposed());

        public static IObservable<Unit> WhenDisposed(this IObjectSpace objectSpace)
            => objectSpace.ProcessEvent(nameof(IObjectSpace.Disposed)).ToUnit();

        public static IObservable<IObjectSpace> WhenModifyChanged(this IObjectSpace objectSpace) 
            => objectSpace.ProcessEvent(nameof(IObjectSpace.ModifiedChanged)).To(objectSpace)
                .TakeUntil(objectSpace.WhenDisposed());

        public static IObservable<IObjectSpace> WhenModifyChanged(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => item.WhenModifyChanged());

#if !XAF192
        public static IObservable<(IObjectSpace objectSpace, ObjectSpaceModificationEventArgs e)> WhenModifiedChanging(this IObjectSpace objectSpace) 
            => objectSpace.ProcessEvent<ObjectSpaceModificationEventArgs>(nameof(BaseObjectSpace.ModifiedChanging)).InversePair(objectSpace)
                .TakeUntil(objectSpace.WhenDisposed());

        public static IObservable<(IObjectSpace objectSpace, ObjectSpaceModificationEventArgs e)> WhenModifiedChanging(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => item.WhenModifiedChanging());
#endif

        static readonly ISubject<(IObjectSpace objectSpace,object obj)> ReloadObjectSubject=Subject.Synchronize(new Subject<(IObjectSpace objectSpace,object obj)>());

        private static readonly Type XPInvalidateableObjectType =
            AppDomain.CurrentDomain.GetAssemblyType("DevExpress.Xpo.IXPInvalidateableObject");
        
        public static IObservable<(IObjectSpace objectSpace, object obj)> WhenObjectReloaded(this IObjectSpace objectSpace,object obj=null) 
            => ReloadObjectSubject.Where(t => t.objectSpace==objectSpace&& (obj == null||obj==t.obj)).AsObservable();
        
        public static IObservable<(IObjectSpace objectSpace, T obj)> WhenObjectReloaded<T>(this IObjectSpace objectSpace,T obj=null) where T:class
            => ReloadObjectSubject.Where(t => t.objectSpace==objectSpace&&t.obj is T tObj&&(obj==null||obj==tObj)).Select(t => (t.objectSpace,(T)t.obj)).AsObservable();

        public static IObservable<IObjectSpace> WhenRefreshing(this IObjectSpace objectSpace)
            => objectSpace.ProcessEvent(nameof(IObjectSpace.Refreshing)).TakeUntil(objectSpace.WhenDisposed()).To(objectSpace);
        public static IObservable<IObjectSpace> WhenReloaded(this IObjectSpace objectSpace) 
            => objectSpace.ProcessEvent(nameof(IObjectSpace.Reloaded)).TakeUntil(objectSpace.WhenDisposed()).To(objectSpace);

        public static IObservable<IObjectSpace> WhenReloaded(this IObservable<IObjectSpace> source) 
            => source.SelectMany(item => item.WhenReloaded());
        
        public static IObservable<T> WhenObjects<T>(this IObjectSpace  objectSpace)
            => objectSpace.WhenCommittedDetailed<T>(ObjectModification.New).ToObjects()
                .Merge(objectSpace.TypesInfo.PersistentTypes(typeof(T))
                    .SelectMany(type => objectSpace.GetObjects(type).Cast<T>().ToArray()).ToNowObservable());

        public static void DeleteObject<T>(this T value) where T:class,IObjectSpaceLink => value.ObjectSpace.Delete(value);
        public static void DeleteObject<T>(this IObjectSpace objectSpace, Expression<Func<T, bool>> criteria=null) {
            var query = objectSpace.GetObjectsQuery<T>();
            if (criteria != null) {
                query = query.Where(criteria);
            }
            objectSpace.Delete(query.ToArray());
        }
        public static bool IsUpdated<T>(this IObjectSpace objectSpace, T t) where T:class 
            => !objectSpace.IsNewObject(t)&&!objectSpace.IsDeletedObject(t);
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
        public static void CommitChanges(this IObjectSpaceLink link)
            => link.ObjectSpace.CommitChanges();
        public static bool IsModified(this IObjectSpaceLink link)
            => link.ObjectSpace.ModifiedObjects.Contains(link);
        public static Task CommitChangesAsync(this IObjectSpaceLink link)
            => link.ObjectSpace.CommitChangesAsync();
        public static T CreateObject<T>(this IObjectSpaceLink link)
            => link.ObjectSpace.CreateObject<T>();
        public static Task CommitChangesAsync(this IObjectSpace objectSpace) 
            => objectSpace is NonPersistentObjectSpace nonPersistentObjectSpace
                ? Task.WhenAll(nonPersistentObjectSpace.AdditionalObjectSpaces.OfType<IObjectSpaceAsync>().ToObservable()
                    .SelectMany(async => Observable.FromAsync(() => async.CommitChangesAsync())).ToTask())
                : ((IObjectSpaceAsync)objectSpace).CommitChangesAsync();
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
                return (T)objectSpace.ReloadObject(value);  
            }
            return objectSpace.GetObject(value);
        }
        public static IObservable<object> WhenNewObjectCreated(
            this IObjectSpace objectSpace, Type objectSpaceLinkType = null)
#if !XAF192
            => objectSpace.WhenModifiedChanging().Where(t => !t.e.Cancel && t.objectSpace.IsNewObject(t.e.Object) && t.e.MemberInfo == null)
                .DistinctUntilChanged(t => t.e.Object)
                .Where(t => objectSpaceLinkType == null || objectSpaceLinkType.IsInstanceOfType(t.e.Object))
                .Select(t => t.e.Object);
#else
            => Observable.Throw<object>(new NotImplementedException());
#endif
        public static IObservable<T> WhenNewObjectCreated<T>(this IObjectSpace objectSpace) where T:IObjectSpaceLink
            => objectSpace.WhenNewObjectCreated(typeof(T)).OfType<T>();

    }
}