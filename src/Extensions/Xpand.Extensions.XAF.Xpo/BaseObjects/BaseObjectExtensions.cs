using DevExpress.Xpo;
using DevExpress.Xpo.Helpers;
using Fasterflect;

namespace Xpand.Extensions.XAF.Xpo.BaseObjects {
    public static class BaseObjectExtensions {
        public static void OnChanged(this PersistentBase persistentBase, string memberName) 
            => persistentBase.CallMethod(nameof(OnChanged), memberName);
        public static bool IsDisposed<TObject>(this TObject obj) where TObject:IXPInvalidateableObject,ISessionProvider 
            => obj.IsInvalidated||(bool)obj.Session.GetFieldValue("isDisposed");
    }
}