using DevExpress.ExpressApp;


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
            objectSpace.Validate();
            objectSpace.CommitChanges();
        }
    }
}