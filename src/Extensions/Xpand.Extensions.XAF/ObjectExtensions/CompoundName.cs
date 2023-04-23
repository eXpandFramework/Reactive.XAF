using System.Collections;
using System.Linq;
using DevExpress.ExpressApp.Utils;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.XAF.ObjectExtensions{
    public static partial class ObjectExtensions{
        public static string CompoundName(this object obj) 
            => LinqExtensions.LinqExtensions.Join((IEnumerable)obj?.ToString().EnsureEndWith("").Split('.')
                .Select(s => CaptionHelper.ConvertCompoundName(s.Split('_').Select(s1 => s1.FirstCharacterToUpper())
                    .JoinString())).ToArray(), ".");
    }
}