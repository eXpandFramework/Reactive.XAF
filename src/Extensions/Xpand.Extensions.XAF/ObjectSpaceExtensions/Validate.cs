using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Validation;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static void Validate(this IObjectSpace objectSpace, params object[] objects) 
            => Validator.GetService(objectSpace.ServiceProvider)
                .ValidateAll(objectSpace, objectSpace.ModifiedObjects.Cast<object>().Concat(objects), ContextIdentifier.Save);
    }
}