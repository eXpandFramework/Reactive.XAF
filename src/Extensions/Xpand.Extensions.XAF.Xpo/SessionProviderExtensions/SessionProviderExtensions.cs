using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using DevExpress.Xpo.Helpers;

namespace Xpand.Extensions.XAF.Xpo.SessionProviderExtensions {
    public static class SessionProviderExtensions {
        public static void InitGuidKey<T>(this T value) where T:ISessionProvider,IXPClassInfoProvider{
            if (value.Session is NestedUnitOfWork || !value.Session.IsNewObject(value) || 
                !value.ClassInfo.KeyProperty.GetValue(value).Equals(Guid.Empty))
                return;
            value.ClassInfo.KeyProperty.SetValue(value,XpoDefault.NewGuid());
        } 
        public static IObjectSpace ObjectSpace(this ISessionProvider provider) 
            => XPObjectSpace.FindObjectSpaceByObject(provider);
    }
}
