using System;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Validation;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static bool IsObjectFitForCriteria(this IObjectSpace objectSpace,CriteriaOperator criteria,params object[] objects) 
            => objects.All(o => {
                var isObjectFitForCriteria = objectSpace.IsObjectFitForCriteria(o, criteria);
                return isObjectFitForCriteria.HasValue && isObjectFitForCriteria.Value;
            });

        public static void CommitChangesAndValidate(this IObjectSpace objectSpace) {
            var ruleSetValidationResult =
                Validator.RuleSet.ValidateAllTargets(objectSpace, objectSpace.ModifiedObjects, ContextIdentifier.Save);
            if (ruleSetValidationResult.ValidationOutcome == ValidationOutcome.Error)
                throw new Exception(ruleSetValidationResult.GetFormattedErrorMessage());
            objectSpace.CommitChanges();
        }
    }
}