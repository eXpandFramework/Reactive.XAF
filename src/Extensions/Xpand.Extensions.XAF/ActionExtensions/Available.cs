using System.Collections.Generic;
using System.Linq;
using 
    
    
    
    DevExpress.ExpressApp.Actions;

namespace Xpand.Extensions.XAF.ActionExtensions {
    public static partial class ActionExtensions {
        public static bool Available(this ActionBase actionBase) 
            => actionBase.Active && actionBase.Enabled;
        
        public static bool Available(this ChoiceActionItem item)
            => item.Active && item.Enabled;
        public static IEnumerable<ChoiceActionItem> Available(this IEnumerable<ChoiceActionItem> source) 
            => source.Where(item => item.Available());
        
        public static IEnumerable<ChoiceActionItem> Active(this IEnumerable<ChoiceActionItem> source) 
            => source.Where(item => item.Active);
    }
}