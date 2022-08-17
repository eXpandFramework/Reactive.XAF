using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static bool IsObjectFitForCriteria(this IObjectSpace objectSpace,CriteriaOperator criteria,params object[] objects) {
            if (ReferenceEquals(criteria,null)) return true;
            return objects.All(o => {
                var isObjectFitForCriteria = objectSpace.IsObjectFitForCriteria(o, criteria);
                return isObjectFitForCriteria.HasValue && isObjectFitForCriteria.Value;
            });
        }

        public static bool Fit(this IObjectSpaceLink objectSpaceLink, CriteriaOperator criteria)
            => objectSpaceLink.ObjectSpace.IsObjectFitForCriteria(criteria, objectSpaceLink);
        public static bool Fit(this IObjectSpaceLink objectSpaceLink, string criteria)
            => objectSpaceLink.Fit(CriteriaOperator.Parse(criteria));
    }
}