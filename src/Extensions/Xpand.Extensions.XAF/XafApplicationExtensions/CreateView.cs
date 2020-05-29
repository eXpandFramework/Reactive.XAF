using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using JetBrains.Annotations;

namespace Xpand.Extensions.XAF.XafApplicationExtensions{
    [PublicAPI]
    public static partial class XafApplicationExtensions{
        public static DetailView NewDetailView(this XafApplication application,object currentObject,IModelDetailView modelDetailView=null,bool isRoot=true){
            var objectSpace = application.CreateObjectSpace();
            modelDetailView ??= application.FindModelDetailView(currentObject.GetType());
            var detailView = application.CreateDetailView(objectSpace, modelDetailView,isRoot);
            detailView.CurrentObject = objectSpace.GetObject(currentObject);
            return detailView;
        }

        public static ObjectView NewObjectView(this XafApplication application,
            System.Type viewType,System.Type objectType) {
            if (viewType == typeof(ListView)){
                var listViewId = application.FindListViewId(objectType);
                var collectionSource = application.CreateCollectionSource(application.CreateObjectSpace(),objectType,listViewId);
                return application.CreateListView((IModelListView) application.Model.Views[listViewId], collectionSource, true);
            }
            var modelDetailView = application.Model.BOModel.GetClass(objectType).DefaultDetailView;
            return application.CreateDetailView(application.CreateObjectSpace(), modelDetailView,true);
        }

        public static CompositeView NewView(this XafApplication application,string viewId) => application.NewView(application.Model.Views[viewId]);

        public static CompositeView NewView(this XafApplication application,IModelView modelView) => (CompositeView) application.CallMethod("CreateView", modelView);


        public static TObjectView NewObjectView<TObjectView>(this XafApplication application,System.Type objectType) where TObjectView:ObjectView =>
            (TObjectView) application.NewObjectView(typeof(TObjectView), objectType);
    }
}