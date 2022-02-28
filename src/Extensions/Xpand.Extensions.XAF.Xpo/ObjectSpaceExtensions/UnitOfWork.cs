using System.Data;
using System.Diagnostics;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.Helpers;
using Fasterflect;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions{
    public static partial class ObjectSpaceExtensions{
        [DebuggerStepThrough]
        public static IXpoDataStoreProvider DataStoreProvider(this IObjectSpaceProvider objectSpaceProvider) 
            => (IXpoDataStoreProvider) objectSpaceProvider.GetPropertyValue("DataStoreProvider");
            
        [DebuggerStepThrough]
        public static UnitOfWork UnitOfWork(this IObjectSpace objectSpace) => (UnitOfWork) ((XPObjectSpace) objectSpace).Session;
        [DebuggerStepThrough]
        public static IDbConnection Connection(this IObjectSpace objectSpace) {
            var dataLayer = objectSpace.UnitOfWork().DataLayer;
            var connectionProvider = ((BaseDataLayer)dataLayer).ConnectionProvider;
            if (connectionProvider is DataCacheNode) {
                return (IDbConnection)((DataCacheRoot)connectionProvider.GetPropertyValue("Nested"))
                    .GetPropertyValue("Nested").GetPropertyValue("Connection");
            }

            if (connectionProvider is DataStorePool pool) {
                var connectionProviderSql = (ConnectionProviderSql)pool.AcquireReadProvider();
                var dbConnection = connectionProviderSql.Connection;
                pool.ReleaseReadProvider(connectionProviderSql);
                return dbConnection;
            }
            return ((ConnectionProviderSql)connectionProvider).Connection;
        }
    }
}