using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static void DontUseCaching(this ModelNode node) 
            => node.SetValue(ModelValueNames.NeedsCachingKey, false);
    }
}