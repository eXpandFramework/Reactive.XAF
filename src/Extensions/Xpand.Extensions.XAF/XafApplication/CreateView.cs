using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using JetBrains.Annotations;

namespace Xpand.Extensions.XAF.XafApplication{
    [PublicAPI]
    public static partial class XafApplicationExtensions{
        public static DetailView NewDetailView(this DevExpress.ExpressApp.XafApplication application,object currentObject,IModelDetailView modelDetailView=null,bool isRoot=true){
            var objectSpace = application.CreateObjectSpace();
            modelDetailView ??= application.FindModelDetailView(currentObject.GetType());
            var detailView = application.CreateDetailView(objectSpace, modelDetailView,isRoot);
            detailView.CurrentObject = objectSpace.GetObject(currentObject);
            return detailView;
        }

        public static ObjectView NewObjectView(this DevExpress.ExpressApp.XafApplication application,
            System.Type viewType,System.Type objectType) {
            if (viewType == typeof(ListView)){
                var listViewId = application.FindListViewId(objectType);
                var collectionSource = application.CreateCollectionSource(application.CreateObjectSpace(),objectType,listViewId);
                return application.CreateListView((IModelListView) application.Model.Views[listViewId], collectionSource, true);
            }
            var modelDetailView = application.Model.BOModel.GetClass(objectType).DefaultDetailView;
            return application.CreateDetailView(application.CreateObjectSpace(), modelDetailView,true);
        }

        public static CompositeView NewView(this DevExpress.ExpressApp.XafApplication application,string viewId){
            return application.NewView(application.Model.Views[viewId]);
        }

        public static CompositeView NewView(this DevExpress.ExpressApp.XafApplication application,IModelView modelView){
            return (CompositeView) application.CallMethod("CreateView", modelView);
        }
        

        public static TObjectView NewObjectView<TObjectView>(this DevExpress.ExpressApp.XafApplication application,System.Type objectType) where TObjectView:ObjectView{
            return (TObjectView) application.NewObjectView(typeof(TObjectView), objectType);
        }

    }
}