using System;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static IModelListView FindLookupListView(this IModelApplication modelApplication, System.Type objectType){
            var modelClass = modelApplication.FindModelClass(objectType);
            return modelClass?.DefaultLookupListView;
        }
    }
}