using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.Extensions.XAF.ActionExtensions{
    public static partial class ActionExtensions{
        public static SimpleAction AsSimpleAction(this ActionBase action) => action as SimpleAction;
        public static SimpleAction ToSimpleAction(this ActionBase action) => ((SimpleAction)action);
    }
}