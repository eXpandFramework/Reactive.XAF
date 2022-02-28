using System;
using System.Linq.Expressions;
using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using Xpand.Extensions.ExpressionExtensions;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static object Evaluate(this Session session, Type objectType, string field,
            Aggregate aggregate, CriteriaOperator filter = null)
            => session.Evaluate(objectType, CriteriaOperator.Parse($"{aggregate}({field})"), filter);
        
        public static object Evaluate(this Session session, XPClassInfo classInfo, string field,
            Aggregate aggregate, CriteriaOperator filter = null)
            => session.Evaluate(classInfo, CriteriaOperator.Parse($"{aggregate}({field})"), filter);
        
        public static object Evaluate<T>(this Session session,  Expression<Func<T,object>> field,
            Aggregate aggregate, Func<Expression<Func<T, bool>>> filter = null) {
            filter ??= () => arg => true;
            return session.Evaluate(typeof(T), CriteriaOperator.Parse($"{aggregate}({field.MemberExpressionName()})"), CriteriaOperator.FromLambda(filter()));
        }
    }
}