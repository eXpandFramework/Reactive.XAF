using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.Helpers;
using DevExpress.Xpo.Metadata;
using DevExpress.Xpo.Providers;
using Fasterflect;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.XAF.Xpo.Xpo {
    public static class XpoExtensions {
        public static void FireChanged(this IXPReceiveOnChangedFromArbitrarySource source, string propertyName) 
            => source.FireChanged(propertyName);

        public static void SetCriteria<T>(this XPBaseCollection collection, Expression<Func<T, bool>> lambda) 
            => collection.Criteria = CriteriaOperator.FromLambda(lambda);
        
        public static void SetFilter<T>(this XPBaseCollection collection, Expression<Func<T, bool>> lambda) 
            => collection.Filter = CriteriaOperator.FromLambda(lambda);

        public static IDbConnection Connection(this UnitOfWork unitOfWork){
            var dataLayer = unitOfWork.DataLayer;
            var connectionProvider = ((BaseDataLayer)dataLayer).ConnectionProvider;
            if (connectionProvider is DataCacheNode){
                return (IDbConnection)((DataCacheRoot)connectionProvider.GetPropertyValue("Nested"))
                    .GetPropertyValue("Nested").GetPropertyValue("Connection");
            }

            if (connectionProvider is DataStorePool pool){
                var connectionProviderSql = (ConnectionProviderSql)pool.AcquireReadProvider();
                var dbConnection = connectionProviderSql.Connection;
                pool.ReleaseReadProvider(connectionProviderSql);
                return dbConnection;
            }

            if (connectionProvider is STASafeDataStore){
                return connectionProvider.GetFieldValue("DataStore").To<ConnectionProviderSql>().Connection;
            }

            return ((ConnectionProviderSql)connectionProvider).Connection;
        }
        
        public static void XpoMigrateDatabase(this XafApplication application, string connectionString=null) {
            var provider = XpoDefault.GetConnectionProvider(connectionString??application.ConnectionString, AutoCreateOption.DatabaseAndSchema);
            var sql = ((IUpdateSchemaSqlFormatter)provider).FormatUpdateSchemaScript(((IDataStoreSchemaMigrationProvider)provider)
                .CompareSchema(new ReflectionDictionary().GetDataStoreSchema(application.TypesInfo.PersistentTypes
                    .Where(info => info.IsPersistent).Select(info => info.Type).ToArray()), new SchemaMigrationOptions()));
            if (!sql.IsNullOrEmpty()) {
                var command = ((ConnectionProviderSql)provider).Connection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

    }
}