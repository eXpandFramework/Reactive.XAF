using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static T EnsureObjectByKey<T>(this IObjectSpace objectSpace, object key,bool inTransaction=false)
            => objectSpace.GetObjectByKey<T>(key)??objectSpace.EnsureInTransaction<T>(key,inTransaction) ?? objectSpace.NewObject<T>(key);
        
        public static T EnsureObject<T>(this IObjectSpace objectSpace, Expression<Func<T, bool>> criteriaExpression=null,Action<T> initialize=null,Action<T> update=null,bool inTransaction=false) where T : class {
            var o = objectSpace.FirstOrDefault(criteriaExpression??(arg =>true) ,inTransaction);
            if (o != null) {
                update?.Invoke(o);
                return o;
            }
            var ensureObject = objectSpace.CreateObject<T>();
            initialize?.Invoke(ensureObject);
            update?.Invoke(ensureObject);
            return ensureObject;
        }

        public static T EnsureObject<T>(this IObjectSpace space, Dictionary<string, T> dictionary, string id){
            var ensureObject = dictionary.GetValueOrDefault(id);
            if (ensureObject == null) {
                ensureObject = space.CreateObject<T>();
                if (ensureObject == null) {
                    throw new NoNullAllowedException(typeof(T).FullName);
                }
                dictionary.Add(id, ensureObject);
            }
            else {
                if (!space.IsNewObject(ensureObject)){
                    ensureObject = space.GetObjectFromKey(ensureObject);
                    if (ensureObject == null) {
                        throw new NoNullAllowedException(typeof(T).Name);
                    }
                }
            }
            return ensureObject;
        }
        [SuppressMessage("ReSharper", "HeapView.CanAvoidClosure")]
        public static T EnsureObject<T>(this IObjectSpace space, ConcurrentDictionary<string, T> dictionary, string id) {
            var add = dictionary.GetOrAdd(id, _ => {
                var o = space.CreateObject<T>();
                typeof(T).ToTypeInfo().KeyMember.SetValue(o,id);
                return o;
            });
            return space.IsNewObject(add)?add: space.GetObject(add);
        }

        public static T EnsureObject<T>(this IObjectSpace objectSpace, CriteriaOperator criteria,Action<T> initialize=null,bool inTransaction=false) where T : class {
            var o = objectSpace.FindObject<T>(criteria,inTransaction);
            if (o != null) {
                return o;
            }
            var ensureObject = objectSpace.CreateObject<T>();
            initialize?.Invoke(ensureObject);
            return ensureObject;
        }
        
        private static T EnsureInTransaction<T>(this IObjectSpace objectSpace,object key,bool inTransaction) 
            => inTransaction ? objectSpace.GetObjects<T>(CriteriaOperator
                    .Parse($"{objectSpace.TypesInfo.FindTypeInfo(typeof(T)).KeyMember.Name}=?", key), true)
                .FirstOrDefault() : default;

        public static object EnsureObjectByKey(this IObjectSpace objectSpace, Type objectType, object key) {
            if (objectSpace.GetObjectByKey(objectType, key) != null)
                return objectSpace.GetObjectByKey(objectType, key);
            var ensureObjectByKey = objectSpace.CreateObject(objectType);
            objectType.ToTypeInfo().KeyMember.SetValue(ensureObjectByKey,key);
            return ensureObjectByKey;
        }
    }
}