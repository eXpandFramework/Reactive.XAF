using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static T NewObject<T>(this IObjectSpace objectSpace) where T : IObjectSpaceLink {
            var instance = (T)typeof(T).CreateInstance(objectSpace.UnitOfWork());
            instance.ObjectSpace = objectSpace;
            return instance;
        }
    }
}