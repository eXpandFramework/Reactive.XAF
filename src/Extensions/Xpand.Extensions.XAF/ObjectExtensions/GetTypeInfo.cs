using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.ObjectExtensions{
    public static class ObjectExtensions{
        public static ITypeInfo GetTypeInfo(this object obj) => obj != null ? XafTypesInfo.Instance.FindTypeInfo(obj.GetType()) : null;
    }
}