using System;
using System.Linq.Expressions;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static void SetFilterCriteria<T>(this IModelListView view, Expression<Func<T, bool>> lambda)
            => view.Filter = CriteriaOperator.FromLambda(lambda).ToString();
        public static void SetCriteria<T>(this IModelListView view, Expression<Func<T, bool>> lambda)
            => view.Criteria = CriteriaOperator.FromLambda(lambda).ToString();
        public static void SetCriteria<T>(this IModelListViewFilterItem item, Expression<Func<T, bool>> lambda)
            => item.Criteria = CriteriaOperator.FromLambda(lambda).ToString();
    }
}