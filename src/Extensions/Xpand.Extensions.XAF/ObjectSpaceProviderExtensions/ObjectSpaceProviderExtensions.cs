using DevExpress.ExpressApp;
using DevExpress.Xpo.DB.Helpers;
using Fasterflect;

using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.XAF.ObjectSpaceProviderExtensions{
    public static class ObjectSpaceProviderExtensions{
        public static string GetConnectionString(this IObjectSpaceProvider objectSpaceProvider){
            var connection = objectSpaceProvider.GetPropertyValue("DataLayer")?.GetPropertyValue("Connection");
            return (string) (connection != null ? connection.GetPropertyValue("ConnectionString")
                : objectSpaceProvider.GetPropertyValue("DataStoreProvider").GetPropertyValue("ConnectionString"));
        }
        public static string GetDataBase(this IObjectSpaceProvider objectSpaceProvider) {
            var parser = new ConnectionStringParser(objectSpaceProvider.GetConnectionString());
            return parser.GetPartByName("Initial Catalog");

        }

        public static bool IsMiddleTier(this IObjectSpaceProvider objectSpaceProvider)
            => objectSpaceProvider.IsInstanceOf("DevExpress.ExpressApp.Security.ClientServer.MiddleTierServerObjectSpaceProvider");

    }
}