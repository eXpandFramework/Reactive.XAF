using System.Collections.Generic;
using DevExpress.Data.Filtering;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.CriteriaOperatorExtensions {
    public static class CriteriaOperatorExtensions {
        public static GroupOperator Group(this IEnumerable<CriteriaOperator> source,GroupOperatorType operatorType=GroupOperatorType.And) 
            => new(operatorType, source.WhereNotDefault());
    }
}