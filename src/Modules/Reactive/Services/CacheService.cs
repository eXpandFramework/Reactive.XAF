using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.Attributes;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class CacheService {
        public static IObservable<TObject[]> Cache<TObject,TKey>(this XafApplication application,
            ConcurrentDictionary<TKey, TObject> cache=null,CriteriaOperator criteriaExpression = null, params string[] modifiedProperties) where TObject : class, IObjectSpaceLink 
            => application.Cache( criteriaExpression, modifiedProperties, cache??new ConcurrentDictionary<TKey, TObject>());
        
        public static IObservable<TObject[]> Cache<TObject>(this XafApplication application,
            ConcurrentDictionary<object, TObject> cache=null,CriteriaOperator criteriaExpression = null,
            params string[] modifiedProperties) where TObject : class, IObjectSpaceLink 
            => application.Cache( criteriaExpression, modifiedProperties, cache??new ConcurrentDictionary<object, TObject>());

        private static TKey GetKeyValue<TObject, TKey>(this TObject link, Func<TObject,TKey> keyValue) where TObject : class, IObjectSpaceLink 
            => keyValue == null ? (TKey)link.ObjectSpace.GetKeyValue(link) : keyValue(link);

        private static IObservable<TObject[]> Cache<TObject, TKey>(this XafApplication application,
            CriteriaOperator criteriaExpression, string[] modifiedProperties, ConcurrentDictionary<TKey, TObject> objectSpaceLinks,Func<TObject,TKey> keyValue=null) where TObject : class, IObjectSpaceLink 
            => application.WhenCommitted<TObject>(modifiedProperties).SelectMany(t => t.RemoveDeleted(objectSpaceLinks)
                    .Concat(application.Cache(criteriaExpression, objectSpaceLinks, keyValue, t)).BufferUntilCompleted())
                .Merge(application.WhenExisting(criteriaExpression, objectSpaceLinks, keyValue)
                    .SelectMany(links => links.ToNowObservable().SelectMany(link =>
                            application.Cache(criteriaExpression, objectSpaceLinks, keyValue, link)).BufferUntilCompleted()));

        private static IObservable<TObject> Cache<TObject, TKey>(this XafApplication application, CriteriaOperator criteriaExpression, ConcurrentDictionary<TKey, TObject> objectSpaceLinks, Func<TObject, TKey> keyValue, (IObjectSpace objectSpace, (TObject instance, ObjectModification modification)[] details) t) where TObject : class, IObjectSpaceLink 
            => t.details.Where(detail => detail.modification!=ObjectModification.Deleted).Select(detail => detail.instance).ToNowObservable()
                .SelectMany(link => application.Cache( criteriaExpression, objectSpaceLinks, keyValue, link));

        private static IObservable<TObject> RemoveDeleted<TObject, TKey>(this (IObjectSpace objectSpace, (TObject instance, ObjectModification modification)[] details) t,ConcurrentDictionary<TKey, TObject> objectSpaceLinks) where TObject : class, IObjectSpaceLink 
            => t.details.Where(detail => detail.modification==ObjectModification.Deleted)
                .Do(detail => objectSpaceLinks.TryRemove((TKey)detail.instance.ObjectSpace.GetKeyValue(detail.instance), out _))
                .Select(detail => detail.instance).IgnoreElements().ToNowObservable();

        private static IObservable<TObject> Cache<TObject, TKey>(this XafApplication application, CriteriaOperator criteriaExpression, ConcurrentDictionary<TKey, TObject> objectSpaceLinks, Func<TObject, TKey> keyValue, TObject link) where TObject : class, IObjectSpaceLink 
            => application.UseObject(link, spaceLink => {
                var match = Xpand.Extensions.XAF.ObjectSpaceExtensions.ObjectSpaceExtensions.Match(spaceLink, criteriaExpression);
                var value = spaceLink.GetKeyValue(keyValue);
                spaceLink.CallMethod("Invalidate", true);
                spaceLink.ObjectSpace.Dispose();
                if (match)
                    return objectSpaceLinks.AddOrUpdate(value, spaceLink, (_, _) => spaceLink).Observe();
                objectSpaceLinks.TryRemove((TKey)link.ObjectSpace.GetKeyValue(link), out _);
                return Observable.Empty<TObject>();

            });

        private static IObservable<(IObjectSpace objectSpace, (TObject instance, ObjectModification modification)[] details)> WhenCommitted<TObject>(this XafApplication application, string[] modifiedProperties) where TObject : class, IObjectSpaceLink 
            => application.WhenProviderCommittedDetailed<TObject>(ObjectModification.All,modifiedProperties:modifiedProperties);

        private static IObservable<TObject[]> WhenExisting<TObject, TKey>(this XafApplication application, CriteriaOperator criteriaExpression, ConcurrentDictionary<TKey, TObject> objectSpaceLinks, Func<TObject, TKey> keyValue) where TObject : class, IObjectSpaceLink 
            => application.WhenExistingObject<TObject>(criteriaExpression)
                .Do(o => {
                    var value = o.GetKeyValue(keyValue);
                    o.CallMethod("Invalidate", true);
                    objectSpaceLinks.TryAdd(value, o);
                })
                .DoOnLast(o => o.ObjectSpace.Dispose())
                .BufferUntilCompleted()
                .Do(links => links.FirstOrDefault()?.ObjectSpace.Dispose());
    }
}