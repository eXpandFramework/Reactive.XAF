using System;
using System.Data;
using DevExpress.ExpressApp;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlConnectionStringBuilder = Microsoft.Data.SqlClient.SqlConnectionStringBuilder;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static bool DbExist(this XafApplication application,string connectionString=null) 
            => new SqlConnectionStringBuilder(connectionString??application.ConnectionString).DbExists();

        public static bool DbExists(this SqlConnectionStringBuilder builder){
            var initialCatalog = "Initial catalog";
            var databaseName = builder[initialCatalog].ToString();
            builder.Remove(initialCatalog);
            using var sqlConnection = new SqlConnection(builder.ConnectionString);
            return sqlConnection.DbExists(databaseName);
        }

        public static bool DBExist(this SqlConnectionStringBuilder builder) {
            var initialCatalog = builder.InitialCatalog;
            builder.Remove("Initial catalog");
            using var sqlConnection = new SqlConnection(builder.ConnectionString);
            return sqlConnection.DbExists(initialCatalog);
        }
        public static bool DbExists(this IDbConnection dbConnection, string databaseName = null) {
            if (dbConnection.State != ConnectionState.Open) {
                dbConnection.Open();
            }

            using var dbCommand = dbConnection.CreateCommand();
            dbCommand.CommandText = $"SELECT db_id('{databaseName ?? dbConnection.Database}')";
            return dbCommand.ExecuteScalar() != DBNull.Value;
        }
    }
}