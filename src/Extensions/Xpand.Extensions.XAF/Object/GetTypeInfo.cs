using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.Object{
    public static class ObjectExtensions{
        public static ITypeInfo GetTypeInfo(this object obj){
            return obj != null ? XafTypesInfo.Instance.FindTypeInfo(obj.GetType()) : null;
        }
    }
}