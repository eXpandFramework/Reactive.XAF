using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.NonPersistentObjects;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static ObjectString NewObjectString(this IObjectSpace objectSpace, string name, string caption = null) {
            var objectString = objectSpace.CreateObject<ObjectString>();
            objectString.Name = name;
            objectString.Caption = caption ?? name;
            return objectString;
        }
    }
}