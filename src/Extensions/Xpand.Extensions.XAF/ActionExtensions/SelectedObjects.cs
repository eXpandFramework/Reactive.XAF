using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Actions;

namespace Xpand.Extensions.XAF.ActionExtensions {
    public static partial class ActionExtensions {
        public static IEnumerable<T> SelectedObjects<T>(this ActionBase actionBase) =>
            actionBase.SelectionContext.SelectedObjects.Cast<T>();
    }
}