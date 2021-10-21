using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static object Evaluate(this IObjectSpace objectSpace, Type objectType, string field,
            Aggregate aggregate, CriteriaOperator filter = null)
            => objectSpace.Evaluate(objectType, CriteriaOperator.Parse($"{aggregate}({field})"), filter);
    }
}