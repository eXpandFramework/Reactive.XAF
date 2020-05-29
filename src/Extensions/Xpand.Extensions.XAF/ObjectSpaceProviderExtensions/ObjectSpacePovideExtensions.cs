using DevExpress.ExpressApp;
using Fasterflect;
using JetBrains.Annotations;

namespace Xpand.Extensions.XAF.ObjectSpaceProviderExtensions{
    public static class ObjectSpacePovideExtensions{
        [PublicAPI]
        public static string GetConnectionString(this IObjectSpaceProvider objectSpaceProvider){
            var connection = objectSpaceProvider.GetPropertyValue("DataLayer")?.GetPropertyValue("Connection");
            return (string) (connection != null ? connection.GetPropertyValue("ConnectionString")
                : ( objectSpaceProvider.GetPropertyValue("DataStoreProvider")).GetPropertyValue("ConnectionString"));
        }

    }
}