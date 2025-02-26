using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Validation;

namespace Xpand.Extensions.XAF.ObjectExtensions{
    public static partial class ObjectExtensions{
        public static ITypeInfo GetTypeInfo(this object obj) => obj != null ? XafTypesInfo.Instance.FindTypeInfo(obj.GetType()) : null;
    }
}