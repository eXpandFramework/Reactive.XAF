using System;
using System.Linq.Expressions;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using Xpand.Extensions.ExpressionExtensions;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static object Evaluate(this IObjectSpace objectSpace, Type objectType, string field,
            Aggregate aggregate, CriteriaOperator filter = null)
            => objectSpace.Evaluate(objectType, CriteriaOperator.Parse($"{aggregate}({field})"), filter);
        
        public static object Evaluate<T>(this IObjectSpace objectSpace,  Expression<Func<T,object>> field,
            Aggregate aggregate, Func<Expression<Func<T, bool>>> filter = null) {
            filter ??= () => arg => true;
            return objectSpace.Evaluate(typeof(T), CriteriaOperator.Parse($"{aggregate}({field.MemberExpressionName()})"), CriteriaOperator.FromLambda(filter()));
        }
    }
}