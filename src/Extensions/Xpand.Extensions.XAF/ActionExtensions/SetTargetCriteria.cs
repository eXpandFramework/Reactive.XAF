using System;
using System.Linq.Expressions;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Actions;

namespace Xpand.Extensions.XAF.ActionExtensions {
    public static partial class ActionExtensions {
        public static void SetTargetCriteria<T>(this ActionBase action, Expression<Func<T, bool>> lambda)
            => action.TargetObjectsCriteria = CriteriaOperator.FromLambda(lambda).ToString();
    }
}