using System;
using System.Data;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
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