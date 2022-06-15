using DevExpress.ExpressApp;
using Fasterflect;

using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.XAF.ObjectSpaceProviderExtensions{
    public static class ObjectSpaceProviderExtensions{
        
        public static string GetConnectionString(this IObjectSpaceProvider objectSpaceProvider){
            var connection = objectSpaceProvider.GetPropertyValue("DataLayer")?.GetPropertyValue("Connection");
            return (string) (connection != null ? connection.GetPropertyValue("ConnectionString")
                : ( objectSpaceProvider.GetPropertyValue("DataStoreProvider")).GetPropertyValue("ConnectionString"));
        }

        public static bool IsMiddleTier(this IObjectSpaceProvider objectSpaceProvider)
            => objectSpaceProvider.IsInstanceOf("DevExpress.ExpressApp.Security.ClientServer.MiddleTierServerObjectSpaceProvider");

    }
}