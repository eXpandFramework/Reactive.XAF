using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static bool IsObjectFitForCriteria(this IObjectSpace objectSpace,string criteria,params object[] objects)
            => objectSpace.IsObjectFitForCriteria(CriteriaOperator.Parse(criteria),objects);
        
        public static bool IsObjectFitForCriteria(this IObjectSpace objectSpace,CriteriaOperator criteria,params object[] objects) 
            => ReferenceEquals(criteria, null) || objects.All(o => {
                var isObjectFitForCriteria = objectSpace.IsObjectFitForCriteria(o, criteria);
                return isObjectFitForCriteria.HasValue && isObjectFitForCriteria.Value;
            });

        public static bool Match(this IObjectSpaceLink objectSpaceLink, CriteriaOperator criteria)
            => objectSpaceLink.ObjectSpace.IsObjectFitForCriteria(criteria, objectSpaceLink);
        public static bool Match(this IObjectSpaceLink objectSpaceLink, string criteria)
            => objectSpaceLink.Match(objectSpaceLink.ObjectSpace.ParseCriteria(criteria));
    }
}