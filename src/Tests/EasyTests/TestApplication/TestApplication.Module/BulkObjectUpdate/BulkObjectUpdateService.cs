using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;
using TestApplication.Module.Common;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.BulkObjectUpdate;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Module.BulkObjectUpdate {
    public static class BulkObjectUpdateService {
        public static IObservable<Unit> ConnectBulkObjectUpdate(this ApplicationModulesManager manager) {
            return manager.WhenGeneratingModelNodes<IModelBulkObjectUpdateRules>()
                .Do(rules => {
                    var rule = rules.AddNode<IModelBulkObjectUpdateRule>();
                    rule.DetailView = (IModelDetailView)rules.Application.Views[TestTask.TaskBulkUpdateDetailView];
                    rule.ListView = rules.Application.BOModel.GetClass(typeof(TestTask)).DefaultListView;
                    rule.Caption = "Update";  
                }).ToUnit();
        }

        

    }


}