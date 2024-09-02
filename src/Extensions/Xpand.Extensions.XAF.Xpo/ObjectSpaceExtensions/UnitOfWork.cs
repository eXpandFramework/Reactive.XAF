using System.Data;
using System.Diagnostics;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using DevExpress.Xpo.DB.Helpers;
using Fasterflect;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions{
    public static partial class ObjectSpaceExtensions{
        
        [DebuggerStepThrough]
        public static IXpoDataStoreProvider DataStoreProvider(this IObjectSpaceProvider objectSpaceProvider) 
            => (IXpoDataStoreProvider) objectSpaceProvider.GetPropertyValue("DataStoreProvider");
            
        [DebuggerStepThrough]
        public static UnitOfWork UnitOfWork(this IObjectSpace objectSpace) 
            => (UnitOfWork)(objectSpace is XPObjectSpace xpObjectSpace ? xpObjectSpace.Session
                : (UnitOfWork)objectSpace.Cast<CompositeObjectSpace>().AdditionalObjectSpaces.OfType<XPObjectSpace>().FirstOrDefault()?.Session);
        
        [DebuggerStepThrough]
        public static UnitOfWork Session(this IObjectSpace objectSpace) 
            => objectSpace.UnitOfWork();

        [DebuggerStepThrough]
        public static IDbConnection Connection(this IObjectSpace objectSpace) => objectSpace.UnitOfWork().Connection();
        
        
        public static string ConnectionString(this IObjectSpace objectSpace) {
            var dbConnection = objectSpace.Connection();
            var options = dbConnection.TryGetPropertyValue("ConnectionOptions");
            if (options == null) return dbConnection.ConnectionString;
            var password = options.GetPropertyValue("Password");
            if (password == null) return dbConnection.ConnectionString;
            var connectionStringParser = new ConnectionStringParser(dbConnection.ConnectionString);
            connectionStringParser.AddPart("Password",password.ToString());
            return connectionStringParser.GetConnectionString();

        }
    }
}