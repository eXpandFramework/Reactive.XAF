using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static bool IsPersistentType(this object instance) => instance.GetTypeInfo().IsPersistent;
        
    }
}