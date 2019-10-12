using DevExpress.ExpressApp.Model.Core;
using Fasterflect;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static void InsertLayer(this ModelApplicationBase application, int index, ModelApplicationBase layer){
            application.CallMethod("InsertLayerAtInternal", layer, index);
        }
    }
}