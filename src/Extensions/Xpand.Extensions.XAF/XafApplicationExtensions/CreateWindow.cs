using System;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.XafApplicationExtensions{
    public static partial class XafApplicationExtensions{
        public static Window CreateViewWindow(this XafApplication application,bool isMain=true,params Controller[] controllers) 
            => application.CreateWindow(TemplateContext.View, controllers, isMain);
        
        public static Window CreateViewWindow(this XafApplication application,Func<View> viewFactory,params Controller[] controllers) {
            var window = application.CreateWindow(TemplateContext.View, controllers, false);
            window.SetView(viewFactory());
            return window;
        }

        public static Window CreatePopupWindow(this XafApplication application,bool isMain=true,params Controller[] controllers) 
            => application.CreateWindow(TemplateContext.PopupWindow, controllers, isMain);
    }
}