using DevExpress.Persistent.Validation;

namespace Xpand.Extensions.XAF.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static RuleSetValidationResult Validate<T>(this object instance) where T : IRule, new() {
            var rule = new T();
            var ruleValidationResult = rule.Validate(instance);
            var ruleSetValidationResult = new RuleSetValidationResult();
            ruleSetValidationResult.AddResult(new RuleSetValidationResultItem(instance, ContextIdentifier.Save, rule, ruleValidationResult));
            if (ruleValidationResult.ValidationOutcome != ValidationOutcome.Valid) {
                throw new ValidationException(ruleSetValidationResult);
            }
            return ruleSetValidationResult;
        }
    }
}