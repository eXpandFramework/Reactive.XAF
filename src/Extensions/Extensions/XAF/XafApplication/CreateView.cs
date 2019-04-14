using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;

namespace Xpand.Source.Extensions.XAF.XafApplication{
    internal static partial class XafApplicationExtensions{
        public static TObjectView CreateObjectView<TObjectView>(this DevExpress.ExpressApp.XafApplication application,Type objectType) where TObjectView:ObjectView{
            if (typeof(TObjectView) == typeof(ListView)){
                var listViewId = application.FindListViewId(objectType);
                var collectionSource = application.CreateCollectionSource(application.CreateObjectSpace(),objectType,listViewId);
                return application.CreateListView((IModelListView) application.Model.Views[listViewId], collectionSource, true) as TObjectView;
            }
            var modelDetailView = application.Model.BOModel.GetClass(objectType).DefaultDetailView;
            return application.CreateDetailView(application.CreateObjectSpace(), modelDetailView,true) as TObjectView;
            
        }

    }
}