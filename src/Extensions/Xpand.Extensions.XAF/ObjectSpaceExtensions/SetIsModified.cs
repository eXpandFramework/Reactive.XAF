
using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static void SetIsModified(this IObjectSpace objectSpace, bool value)
            => objectSpace.CallMethod(nameof(SetIsModified), value);
    }
}