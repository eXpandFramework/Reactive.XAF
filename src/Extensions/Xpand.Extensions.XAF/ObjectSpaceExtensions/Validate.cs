using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Validation;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static void Validate(this IObjectSpace objectSpace, params object[] objects) {
            var ruleSet = Validator.GetService(objectSpace.ServiceProvider);
            objectSpace.ModifiedObjects.Cast<object>().Concat(objects)
                .Do(o => ruleSet.ValidateTarget(objectSpace, o,ContextIdentifier.Save))
                .Enumerate();
            
            ruleSet
                .ValidateAll(objectSpace, objectSpace.ModifiedObjects.Cast<object>().Concat(objects),
                    ContextIdentifier.Save);
        }
        public static void ValidateTargets(this IObjectSpace objectSpace, params object[] objects) {
            var ruleSet = Validator.GetService(objectSpace.ServiceProvider);
            objectSpace.ModifiedObjects.Cast<object>().Concat(objects)
                .Do(o => ruleSet.ValidateTarget(objectSpace, o,ContextIdentifier.Save))
                .Enumerate();
        }
    }
}