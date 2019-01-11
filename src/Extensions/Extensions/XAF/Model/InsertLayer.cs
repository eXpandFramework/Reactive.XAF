using DevExpress.ExpressApp.Model.Core;
using Fasterflect;

namespace Xpand.Source.Extensions.XAF.Model{
    internal static partial class Extensions{
        public static void InsertLayer(this ModelApplicationBase application, int index, ModelApplicationBase layer){
            application.CallMethod("InsertLayerAtInternal", layer, index);
        }
    }
}