using System.Data;
using System.Data.SqlClient;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions{
    public static partial class ObjectSpaceExtensions{
        public static void DeleteAllData(this IObjectSpaceProvider objectSpaceProvider) {
            using var objectSpace = objectSpaceProvider.CreateUpdatingObjectSpace(true);
            objectSpace.Connection().DeleteAllData();
            objectSpaceProvider.UpdateSchema();
        }

        public static void DeleteAllData(this XafApplication application) {
            if (!application.DbExist()) return;
            using var sqlConnection = new SqlConnection(application.ConnectionString);
            sqlConnection.DeleteAllData();
        }

        public static void DeleteAllData(this IDbConnection dbConnection) {
            if (dbConnection.State != ConnectionState.Open) {
                dbConnection.Open();
            }
            using var dbCommand = dbConnection.CreateCommand();
            dbCommand.CommandText = @"
        EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT all'
        EXEC sp_MSForEachTable '
            IF OBJECTPROPERTY(object_id(''?''), ''TableHasIdentity'') = 1
            BEGIN
                DBCC CHECKIDENT (''?'', RESEED, 0)
            END
            DELETE FROM ?'
        EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all'
";
            dbCommand.ExecuteNonQuery();
        }
    }
}