using System.Data;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.Helpers;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions{
    public static partial class ObjectSpaceExtensions{
        public static void DeleteAllData(this IObjectSpaceProvider objectSpaceProvider) {
            using var objectSpace = objectSpaceProvider.CreateUpdatingObjectSpace(true);
            var dbConnection =
                ((ConnectionProviderSql) ((BaseDataLayer) ((XPObjectSpace) objectSpace).Session.DataLayer)
                    .ConnectionProvider).Connection;
            DeleteAllData(dbConnection);

            objectSpaceProvider.UpdateSchema();
        }

        public static void DeleteAllData(this IDbConnection dbConnection) {
            using (var dbCommand = dbConnection.CreateCommand()) {
                dbCommand.CommandText = @"
        EXEC sp_MSForEachTable ""ALTER TABLE ? NOCHECK CONSTRAINT all""
        EXEC sp_MSForEachTable ""DELETE FROM ?""
        exec sp_MSForEachTable ""ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all""
";
                dbCommand.ExecuteNonQuery();
            }
        }
    }
}