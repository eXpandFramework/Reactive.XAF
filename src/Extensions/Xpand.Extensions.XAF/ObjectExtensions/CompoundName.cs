using DevExpress.ExpressApp.Utils;

namespace Xpand.Extensions.XAF.ObjectExtensions{
    public static partial class ObjectExtensions{
        public static string CompoundName(this object obj) => CaptionHelper.ConvertCompoundName($"{obj}");
    }
}