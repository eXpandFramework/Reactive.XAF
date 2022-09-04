using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.CollectionSourceExtensions {
    public static partial class CollectionSourceExtensions {
        public static void SetCriteria<T>(this CollectionSourceBase collectionSourceBase, string key, Expression<Func<T, bool>> lambda) 
            => collectionSourceBase.Criteria[key]=CriteriaOperator.FromLambda(lambda);
        public static void SetCriteria<T>(this CollectionSourceBase collectionSourceBase, Expression<Func<T, bool>> lambda,[CallerMemberName]string callMemberName="") 
            => collectionSourceBase.SetCriteria(callMemberName,lambda);
    }
}