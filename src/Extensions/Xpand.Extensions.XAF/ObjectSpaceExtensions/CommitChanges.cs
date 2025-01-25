using DevExpress.ExpressApp;
using DevExpress.Persistent.Validation;


namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static void CommitChanges(this IObjectSpace objectSpace,bool validate) {
            if (!validate) {
                objectSpace.CommitChanges();
            }
            else {
                objectSpace.CommitChangesAndValidate();
            }
        }

        public static void CommitChangesAndValidate(this IObjectSpace objectSpace) {
            var ruleSetValidationResult = Validator.RuleSet.ValidateAllTargets(objectSpace, objectSpace.ModifiedObjects, ContextIdentifier.Save);
            if (ruleSetValidationResult.ValidationOutcome == ValidationOutcome.Error)
                throw new ValidationException(ruleSetValidationResult);
            objectSpace.CommitChanges();
        }
    }
}