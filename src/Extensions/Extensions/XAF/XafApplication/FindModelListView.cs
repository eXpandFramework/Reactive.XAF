using System;
using DevExpress.ExpressApp.Model;

namespace Xpand.Source.Extensions.XAF.XafApplication{
    internal static partial class XafApplicationExtensions{
        public static IModelListView FindModelListView(this DevExpress.ExpressApp.XafApplication application, Type objectType){
            return (IModelListView) application.Model.Views[application.FindListViewId(objectType)];
        }
    }
}