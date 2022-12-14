using System;
using System.Linq.Expressions;
using DevExpress.Data.Filtering;
using DevExpress.Xpo;

namespace Xpand.Extensions.XAF.Xpo.Xpo {
    public static class XpoExtensions {
        public static void FireChanged(this IXPReceiveOnChangedFromArbitrarySource source, string propertyName) 
            => source.FireChanged(propertyName);

        public static void SetCriteria<T>(this XPCollection collection, Expression<Func<T, bool>> lambda) 
            => collection.Criteria = CriteriaOperator.FromLambda(lambda);
        
        public static void SetFilter<T>(this XPCollection collection, Expression<Func<T, bool>> lambda) 
            => collection.Filter = CriteriaOperator.FromLambda(lambda);
        
    }
}