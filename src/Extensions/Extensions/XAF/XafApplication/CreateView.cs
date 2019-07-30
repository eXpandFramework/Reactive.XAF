using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;

namespace Xpand.Source.Extensions.XAF.XafApplication{
    internal static partial class XafApplicationExtensions{
        public static ObjectView CreateObjectView(this DevExpress.ExpressApp.XafApplication application,
            Type viewType,Type objectType) {
            if (viewType == typeof(ListView)){
                var listViewId = application.FindListViewId(objectType);
                var collectionSource = application.CreateCollectionSource(application.CreateObjectSpace(),objectType,listViewId);
                return application.CreateListView((IModelListView) application.Model.Views[listViewId], collectionSource, true);
            }
            var modelDetailView = application.Model.BOModel.GetClass(objectType).DefaultDetailView;
            return application.CreateDetailView(application.CreateObjectSpace(), modelDetailView,true);
        }

        public static TObjectView CreateObjectView<TObjectView>(this DevExpress.ExpressApp.XafApplication application,Type objectType) where TObjectView:ObjectView{
            var objectView = (TObjectView) application.CreateObjectView(typeof(TObjectView), objectType);
            if (typeof(TObjectView) == typeof(DetailView)){
                objectView.CurrentObject = objectView.ObjectSpace.CreateObject(objectType);
                objectView.ObjectSpace.CommitChanges();
            }
            return objectView;
        }

    }
}