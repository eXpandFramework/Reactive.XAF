using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;

namespace Xpand.Extensions.Blazor {
    public static class Extensions {
        public static BlazorApplication ToBlazor(this XafApplication application) => (BlazorApplication) application;
    }
}
