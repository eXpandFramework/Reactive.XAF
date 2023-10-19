using System.Data;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static IDbCommand CreateCommand(this IObjectSpace objectSpace)
            => !(objectSpace is BaseObjectSpace baseObjectSpace) ? null : baseObjectSpace.Connection.CreateCommand();
    }
}