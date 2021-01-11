using System;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public partial class ModelExtensions{
        public static T ReadViewInLayer<T>(this IModelApplication modelApplication, T modelView, string newViewId) where T:IModelView{
            var modelViews =modelApplication.Application.Views?? modelApplication.AddNode<IModelViews>();
            if (modelViews[modelView.Id]!=null)
                throw new NotSupportedException($"{modelView.Id} already exists");
            IModelView newNode;
            switch (modelView){
                case IModelDetailView _:
                    newNode = modelViews.AddNode<IModelDetailView>();
                    break;
                case IModelListView _:
                    newNode = modelViews.AddNode<IModelListView>();
                    break;
                case IModelDashboardView _:
                    newNode = modelViews.AddNode<IModelDashboardView>();
                    break;
                default:
                    throw new NotImplementedException();
            }
            
            newNode.ReadFromModel( modelView);
            newNode.Id = newViewId;
            return (T) newNode;
        }

    }
}