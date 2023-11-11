using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static T GetService<T>(this XafApplication application) where T : notnull
            => application.ServiceProvider.GetService<T>();
        
        public static T GetRequiredService<T>(this XafApplication application) where T : notnull 
            => application.ServiceProvider.GetRequiredService<T>();
    }
}