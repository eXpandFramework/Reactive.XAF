using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Reactive.Logger.Client.Win {
    public class ReactiveLoggerClientModule:ModuleBase {
        public override void Setup(ApplicationModulesManager moduleManager) {
            base.Setup(moduleManager);
            moduleManager.RegisterViewSimpleAction("ClearListView",
                    action => action.Caption = "Clear", PredefinedCategory.PopupActions)
                .WhenExecuted(e => {
                    var objectSpace = e.Action.View().ObjectSpace;
                    try {
                        var dbCommand = objectSpace.Connection().CreateCommand();
                        dbCommand.CommandText = $"Truncate Table {nameof(TraceEvent)}";
                        dbCommand.ExecuteScalar();
                        objectSpace.Refresh();
                    }
                    catch (Exception) {
                        var traceEvents = objectSpace.GetObjectsQuery<TraceEvent>().ToArray();
                        objectSpace.Delete(traceEvents);
                        objectSpace.CommitChanges();    
                    }
                    
                })
                .Subscribe(this);
        }
    }
}