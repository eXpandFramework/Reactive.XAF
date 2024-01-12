using System;
using System.Data;
using System.Data.SqlClient;
using DevExpress.ExpressApp;
using Microsoft.Extensions.Configuration;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static bool DbExist(this XafApplication application) {
            var builder = new SqlConnectionStringBuilder(application.GetRequiredService<IConfiguration>()
                .GetConnectionString("ConnectionString"));
            var initialCatalog = "Initial catalog";
            var databaseName = builder[initialCatalog].ToString();
            builder.Remove(initialCatalog);
            using var sqlConnection = new SqlConnection(builder.ConnectionString);
            return sqlConnection.DbExists(databaseName);
        }

        public static bool DbExist(this IObjectSpaceProvider objectSpaceProvider) {
            using var objectSpace = objectSpaceProvider.CreateUpdatingObjectSpace(true);
            var dbConnection = objectSpace.Connection();
            return dbConnection.DbExists( objectSpace.Database);
        }

        public static bool DbExists(this IDbConnection dbConnection, string databaseName=null){
            if (dbConnection.State != ConnectionState.Open) {
                dbConnection.Open();
            }
            using var dbCommand = dbConnection.CreateCommand();
            dbCommand.CommandText = $"SELECT db_id('{databaseName??dbConnection.Database}')";
            return dbCommand.ExecuteScalar() != DBNull.Value;
        }
    }
}