using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
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
            var connectionProvider = ((ThreadSafeDataLayer)objectSpace.UnitOfWork().DataLayer).ConnectionProvider;
            if (connectionProvider is DataCacheNode) {
                return (IDbConnection)((DataCacheRoot)connectionProvider.GetPropertyValue("Nested"))
                    .GetPropertyValue("Nested").GetPropertyValue("Connection");
            }

            return ((ConnectionProviderSql)connectionProvider).Connection;
        }
    }
}