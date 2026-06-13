using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Validation;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static void Validate(this IObjectSpace objectSpace, params object[] objects) {
            var ruleSet = Validator.GetService(objectSpace.ServiceProvider);
            // objectSpace.ModifiedObjects.Cast<object>().Concat(objects)
            //     .Do(o => ruleSet.ValidateTarget(objectSpace, o,ContextIdentifier.Save))
            //     .Enumerate();
            
            ruleSet
                .ValidateAll(objectSpace, objectSpace.ModifiedObjects.Cast<object>().Concat(objects),
                    ContextIdentifier.Save);
        }

        public static void ValidateTargets(this IObjectSpace objectSpace, params object[] objects) {
            var result = objectSpace.ValidationOutcomes(ValidationOutcome.Error, objects).FirstOrDefault();
            if (result==null)return;
            throw new ValidationException(result);
        }

        public static RuleSetValidationResult[] ValidationOutcomes(this IObjectSpace objectSpace,ValidationOutcome outcome, params object[] objects) {
            var ruleSet = Validator.GetService(objectSpace.ServiceProvider);
            return objectSpace.ModifiedObjects.Cast<object>().Concat(objects)
                .Select(o => ruleSet.ValidateTarget(objectSpace, o,ContextIdentifier.Save))
                .Where(result => result.ValidationOutcome==outcome)
                .ToArray(); 
        }
    }
}