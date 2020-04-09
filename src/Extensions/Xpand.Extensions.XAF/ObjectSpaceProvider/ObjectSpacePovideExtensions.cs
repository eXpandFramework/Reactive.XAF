using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Extensions.XAF.ObjectSpaceProvider{
    public static class ObjectSpacePovideExtensions{
        public static string GetConnectionString(this IObjectSpaceProvider objectSpaceProvider){
            var connection = objectSpaceProvider.GetPropertyValue("DataLayer")?.GetPropertyValue("Connection");
            return (string) (connection != null ? connection.GetPropertyValue("ConnectionString")
                : ( objectSpaceProvider.GetPropertyValue("DataStoreProvider")).GetPropertyValue("ConnectionString"));
        }

    }
}