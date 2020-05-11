using System;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.XafApplication{
    public static partial class XafApplicationExtensions{
        public static string FindViewId(this DevExpress.ExpressApp.XafApplication application,
            ViewType viewType, System.Type objectType){
            if (viewType == ViewType.DetailView){
                return application.FindDetailViewId(objectType);
            }
            if (viewType == ViewType.ListView){
                return application.FindListViewId(objectType);
            }

            throw new NotImplementedException();
        }
    }
}