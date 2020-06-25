using System;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
	    public static void AddLayer(this ModelApplicationBase application, ModelNode layer) => ModelApplicationHelper.AddLayer(application, (ModelApplicationBase) layer);

        public static void AddLayerBeforeLast(this ModelApplicationBase application, ModelApplicationBase layer){
            ModelApplicationBase lastLayer = application.LastLayer;
            if (lastLayer.Id != "After Setup" && lastLayer.Id != "UserDiff")
                throw new ArgumentException(@"LastLayer.Id", lastLayer.Id);
            ModelApplicationHelper.RemoveLayer(application);
            ModelApplicationHelper.AddLayer(application, layer);
            ModelApplicationHelper.AddLayer(application, lastLayer);
        }
    }
}