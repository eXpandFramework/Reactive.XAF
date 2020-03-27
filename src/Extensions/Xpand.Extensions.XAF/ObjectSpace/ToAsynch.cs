using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.ObjectSpace{
    public static class ObjectSpaceExtensions{
        public static IObjectSpaceAsync ToAsynch<T>(this T objectSpace) where T : IObjectSpace{
            return (IObjectSpaceAsync) objectSpace;
        }
    }
}