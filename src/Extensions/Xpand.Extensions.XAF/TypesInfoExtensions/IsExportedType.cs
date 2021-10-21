using System;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static bool IsExportedType(this Type type) => ExportedTypeHelpers.IsExportedType(type);
    }
}