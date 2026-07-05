using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static bool DbExist(this IObjectSpaceProvider objectSpaceProvider) {
            using var objectSpace = objectSpaceProvider.CreateUpdatingObjectSpace(true);
            var dbConnection = objectSpace.Connection();
            return dbConnection.DbExists( objectSpace.Database);
        }
    }
}