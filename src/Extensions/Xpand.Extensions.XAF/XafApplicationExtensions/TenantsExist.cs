using System.Data.SqlClient;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static bool TenantsExist(this XafApplication application, string connectionString = null,
            int recordCount = 2) {
            connectionString ??= application.ConnectionString;
            if (!application.DbExist(connectionString)) return false;
            using var sqlConnection = new SqlConnection(connectionString);
            var cmdText = @"
            IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Tenant')
            BEGIN
                SELECT CASE WHEN COUNT(*) = @RecordCount THEN 1 ELSE 0 END FROM dbo.Tenant;
            END
            ELSE
            BEGIN
                SELECT 0;
            END";

            using var command = new SqlCommand(cmdText, sqlConnection);
            command.Parameters.AddWithValue("@RecordCount", recordCount);
            sqlConnection.Open();
            var result = command.ExecuteScalar()!;
            sqlConnection.Close();
            return result == (object)1;
        }
    }
}