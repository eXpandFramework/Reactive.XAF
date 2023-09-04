using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Actions;

namespace Xpand.Extensions.XAF.ActionExtensions {
    public static partial class ActionExtensions {
        public static IEnumerable<ChoiceActionItem> Items<T>(this SingleChoiceAction action)
            => action.Items.Where(item => item.Data is T);
    }
}