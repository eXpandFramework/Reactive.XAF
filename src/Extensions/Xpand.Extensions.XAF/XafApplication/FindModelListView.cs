using System;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.XafApplication{
    public static partial class XafApplicationExtensions{
        public static IModelListView FindModelListView(this DevExpress.ExpressApp.XafApplication application, Type objectType){
            return (IModelListView) application.Model.Views[application.FindListViewId(objectType)];
        }
    }
}