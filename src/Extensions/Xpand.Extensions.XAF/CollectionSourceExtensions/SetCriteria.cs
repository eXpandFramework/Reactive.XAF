using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.CriteriaOperatorExtensions;

namespace Xpand.Extensions.XAF.CollectionSourceExtensions {
    public static partial class CollectionSourceExtensions {
        public static void SetCriteria(this CollectionSourceBase collectionSourceBase, string key, Type type, LambdaExpression lambda) 
            => collectionSourceBase.SetCriteria( key, lambda.ToCriteria(type));

        public static void SetCriteria(this CollectionSourceBase collectionSourceBase, string key, CriteriaOperator criteriaOperator){
            collectionSourceBase.BeginUpdateCriteria();
            collectionSourceBase.Criteria[key] = criteriaOperator;
            collectionSourceBase.EndUpdateCriteria();
        }

        public static void SetCriteria(this CollectionSourceBase collectionSourceBase, LambdaExpression lambda,[CallerMemberName]string caller="") 
            => collectionSourceBase.SetCriteria(caller,collectionSourceBase.ObjectTypeInfo.Type, lambda);
        
        public static void SetCriteria(this CollectionSourceBase collectionSourceBase, Type type,LambdaExpression lambda,[CallerMemberName]string caller="") 
            => collectionSourceBase.SetCriteria(caller,type, lambda);

        public static void SetCriteria<T>(this CollectionSourceBase collectionSourceBase, string key, Expression<Func<T, bool>> lambda) 
            => collectionSourceBase.SetCriteria(key, lambda.Parameters.First().Type,lambda);
        public static void SetCriteria<T>(this CollectionSourceBase collectionSourceBase, Expression<Func<T, bool>> lambda,[CallerMemberName]string callMemberName="") 
            => collectionSourceBase.SetCriteria(callMemberName,lambda);
        
        
        public static void SetFilter<T>(this ProxyCollection collection, Expression<Func<T, bool>> lambda) 
            => collection.Filter = CriteriaOperator.FromLambda(lambda);
    }
}