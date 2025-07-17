using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Validation;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static void Validate(this IObjectSpace objectSpace, params object[] objects) {
            var ruleSetValidationResult = Validator.RuleSet.ValidateAllTargets(objectSpace,
                objectSpace.ModifiedObjects.Cast<object>().Concat(objects), ContextIdentifier.Save);
            if (ruleSetValidationResult.ValidationOutcome == ValidationOutcome.Error)
                throw new ValidationException(ruleSetValidationResult);
        }
    }
}