using DevExpress.Xpo;
using Fasterflect;

namespace Xpand.Extensions.XAF.Xpo.BaseObjects {
    public static class BaseObjectExtensions {
        public static void OnChanged(this PersistentBase persistentBase, string memberName) 
            => persistentBase.CallMethod(nameof(OnChanged), memberName);
        public static bool IsDisposed(this IXPInvalidateableObject obj) 
            => obj.IsInvalidated;
    }
}