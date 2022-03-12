using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Reactive.Logger.Client.Win {
    public class ReactiveLoggerClientModule:ModuleBase {
        public override void Setup(ApplicationModulesManager moduleManager) {
            base.Setup(moduleManager);
            moduleManager.RegisterViewSimpleAction("ClearListView",
                    action => action.Caption = "Clear", PredefinedCategory.PopupActions)
                .WhenExecuted(e => {
                    var objectSpace = e.Action.View().ObjectSpace;
                    var traceEvents = objectSpace.GetObjectsQuery<TraceEvent>().ToArray();
                    objectSpace.Delete(traceEvents);
                    return objectSpace.Commit();
                })
                .Subscribe(this);
        }
    }
}