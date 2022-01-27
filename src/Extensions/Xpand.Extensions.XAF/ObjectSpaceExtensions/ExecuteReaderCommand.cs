using System.Data;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static IDataReader ExecuteReaderCommand(this IObjectSpace objectSpace, string commandText) {
            using var command = objectSpace.CreateCommand();
            command.CommandText = commandText;
            return command.ExecuteReader();
        }
    }
}