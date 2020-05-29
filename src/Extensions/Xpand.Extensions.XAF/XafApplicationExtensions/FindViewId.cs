using System;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.XafApplicationExtensions{
    public static partial class XafApplicationExtensions{
        public static string FindViewId(this XafApplication application,
            ViewType viewType, Type objectType){
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