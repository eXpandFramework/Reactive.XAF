using System.Data;
using DevExpress.ExpressApp;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions{
    public static partial class ObjectSpaceExtensions{
        public static void DeleteAllData(this IObjectSpaceProvider objectSpaceProvider) {
            objectSpaceProvider.DeleteAllData(false);
        }

        public static void DeleteAllData(this IObjectSpaceProvider objectSpaceProvider, bool deleteTables) {
            using var objectSpace = objectSpaceProvider.CreateUpdatingObjectSpace(true);
            objectSpace.Connection().DeleteAllData(deleteTables);
            objectSpaceProvider.UpdateSchema();
        }

        public static void DeleteAllData(this XafApplication application) {
            application.DeleteAllData(false);
        }

        public static void DeleteAllData(this XafApplication application, bool deleteTables) {
            var connectionString = application.GetService<IConfiguration>()
                .GetConnectionString("ConnectionString")??application.ConnectionString;
            if (!application.DbExist(connectionString)) return;
            using var sqlConnection = new SqlConnection(connectionString);
            sqlConnection.DeleteAllData(deleteTables);
        }

        public static void DeleteAllData(this IDbConnection dbConnection) {
            dbConnection.DeleteAllData(false);
        }

        public static void DeleteAllData(this IDbConnection dbConnection, bool deleteTables) {
            if (dbConnection.State != ConnectionState.Open) {
                dbConnection.Open();
            }

            using var dbCommand = dbConnection.CreateCommand();

            if (deleteTables) {
                dbCommand.CommandText = @"
    EXEC sp_MSForEachTable 'SET QUOTED_IDENTIFIER ON; ALTER TABLE ? NOCHECK CONSTRAINT all'
    EXEC sp_MSForEachTable 'SET QUOTED_IDENTIFIER ON; DROP TABLE ?'
";
            }
            else {
                dbCommand.CommandText = @"
    EXEC sp_MSForEachTable 'SET QUOTED_IDENTIFIER ON; ALTER TABLE ? NOCHECK CONSTRAINT all'
    EXEC sp_MSForEachTable 'SET QUOTED_IDENTIFIER ON;
        IF OBJECTPROPERTY(object_id(''?''), ''TableHasIdentity'') = 1
        BEGIN
            DBCC CHECKIDENT (''?'', RESEED, 0)
        END
        DELETE FROM ?'
    EXEC sp_MSForEachTable 'SET QUOTED_IDENTIFIER ON; ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all'
";
            }
            dbCommand.ExecuteNonQuery();
        }
    }
}