using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Fasterflect;


namespace Xpand.Extensions.XAF.XafApplicationExtensions{
    
    public static partial class XafApplicationExtensions{
        public static DetailView NewDetailView(this XafApplication application,object instance, IModelDetailView modelDetailView = null, bool isRoot = true) 
            => application.NewDetailView(space => space.GetObject(instance),modelDetailView,isRoot);

        public static DetailView NewDetailView<T>(this XafApplication application,Func<IObjectSpace,T> currentObjectFactory,IModelDetailView modelDetailView=null,bool isRoot=true){
            var objectSpace = application.CreateObjectSpace(typeof(T));
            var currentObject = currentObjectFactory(objectSpace);
            modelDetailView ??= application.FindModelDetailView(currentObject.GetType());
            var detailView = application.CreateDetailView(objectSpace, modelDetailView,isRoot);
            detailView.CurrentObject = currentObject;
            return detailView;
        }

        public static ObjectView NewObjectView(this XafApplication application,
            Type viewType,Type objectType) {
            var objectSpace = application.CreateObjectSpace(objectType);
            if (viewType == typeof(ListView)){
                var listViewId = application.FindListViewId(objectType);
                var collectionSource = application.CreateCollectionSource(objectSpace,objectType,listViewId);
                return application.CreateListView((IModelListView) application.Model.Views[listViewId], collectionSource, true);
            }
            var modelDetailView = application.Model.BOModel.GetClass(objectType).DefaultDetailView;
            return application.CreateDetailView(objectSpace, modelDetailView,true);
        }

        public static TView NewView<TView>(this XafApplication application,  Type objectType) where TView:CompositeView 
            => (TView)application.NewView(typeof(DetailView).IsAssignableFrom(typeof(TView))?ViewType.DetailView : ViewType.ListView,objectType);
        
        public static ListView NewListView(this XafApplication application,  Type objectType)  
            => application.NewView<ListView>(objectType);
        
        public static DetailView NewDetailView(this XafApplication application,  Type objectType)  
            => application.NewView<DetailView>(objectType);

        public static CompositeView NewView(this XafApplication application,ViewType viewType,Type objectType){
	        var modelClass = application.Model.BOModel.GetClass(objectType);
	        return application.NewView((viewType == ViewType.ListView ? modelClass.DefaultListView.Id : modelClass.DefaultDetailView.Id));
        }

        public static CompositeView NewView(this XafApplication application,string viewId) => application.NewView(application.Model.Views[viewId]);

        public static CompositeView NewView(this XafApplication application,IModelView modelView,IObjectSpace objectSpace=null) 
            => (CompositeView) (objectSpace==null?application.CallMethod("CreateView", modelView):application.CreateView(modelView, objectSpace));

        static View CreateView(this XafApplication application,IModelView viewModel,IObjectSpace objectSpace) {
	        View view = null;
	        switch (viewModel){
		        case IModelListView listViewModel:{
			        var collectionSource = application.CreateCollectionSource(objectSpace, listViewModel.ModelClass.TypeInfo.Type, listViewModel.Id);
			        view = application.CreateListView(listViewModel, collectionSource, true);
			        break;
		        }
		        case IModelDetailView detailViewModel:
			        view = application.CreateDetailView(objectSpace, detailViewModel, true);
			        break;
		        case IModelDashboardView _:
			        view = application.CreateDashboardView(objectSpace, viewModel.Id, true);
			        break;
	        }
	        return view;
        }



        public static TObjectView NewObjectView<TObjectView>(this XafApplication application,Type objectType) where TObjectView:ObjectView 
            => (TObjectView) application.NewObjectView(typeof(TObjectView), objectType);
    }
}