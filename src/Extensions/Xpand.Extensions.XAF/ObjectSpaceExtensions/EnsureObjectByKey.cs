using System;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static T EnsureObjectByKey<T>(this IObjectSpace objectSpace, object key,bool inTransaction=false)
            => objectSpace.GetObjectByKey<T>(key)??objectSpace.EnsureInTransaction<T>(key,inTransaction) ?? objectSpace.NewObject<T>(key);
        
        public static T EnsureObject<T>(this IObjectSpace objectSpace, Expression<Func<T, bool>> criteriaExpression,Action<T> initialize=null,bool inTransaction=false) where T : class {
            var o = objectSpace.FirstOrDefault(criteriaExpression,inTransaction);
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

        public static object EnsureObjectByKey(this IObjectSpace objectSpace, Type objectType, object key)
            => objectSpace.GetObjectByKey(objectType, key);
        
    }
}