using System.Data;
using DevExpress.ExpressApp;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions{
    public static partial class ObjectSpaceExtensions{
        public static void DeleteAllData(this IObjectSpaceProvider objectSpaceProvider) {
            using var objectSpace = objectSpaceProvider.CreateUpdatingObjectSpace(true);
            objectSpace.Connection().DeleteAllData();
            objectSpaceProvider.UpdateSchema();
        }

        public static void DeleteAllData(this XafApplication application) {
            if (!application.DbExist()) return;
            using var sqlConnection = new SqlConnection(application.GetService<IConfiguration>()
                .GetConnectionString("ConnectionString")??application.ConnectionString);
            sqlConnection.DeleteAllData();
        }

        public static void DeleteAllData(this IDbConnection dbConnection) {
            if (dbConnection.State != ConnectionState.Open) {
                dbConnection.Open();
            }

            using var dbCommand = dbConnection.CreateCommand();
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
            dbCommand.ExecuteNonQuery();


        }
    }
}