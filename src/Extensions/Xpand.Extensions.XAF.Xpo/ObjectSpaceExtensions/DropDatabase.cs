using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static bool DropDatabase(this IObjectSpaceProvider objectSpaceProvider) {
            if (!objectSpaceProvider.DbExist()) return false;
            using var objectSpace = objectSpaceProvider.CreateUpdatingObjectSpace(true);
            using var dbCommand = objectSpace.Connection().CreateCommand();
            dbCommand.CommandText =
                $"ALTER DATABASE [{objectSpace.Database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [{objectSpace.Database}]";
            dbCommand.ExecuteNonQuery();
            return true;
        }
    }
}