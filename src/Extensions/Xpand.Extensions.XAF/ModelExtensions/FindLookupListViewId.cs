using System;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static IModelListView FindLookupListView(this IModelApplication modelApplication, Type objectType) => modelApplication
            .GetModelClass(objectType)?.DefaultLookupListView;
    }
}